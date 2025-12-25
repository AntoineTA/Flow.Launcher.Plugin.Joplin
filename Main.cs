using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Joplin
{
    public class Main : IAsyncPlugin, ISettingProvider, IDisposable
    {
        private PluginInitContext? _context;
        private Settings? _settings;
        private JoplinApiClient? _apiClient;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
            _apiClient = new JoplinApiClient(_settings);
            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            // Reload settings on each query to get latest values
            _settings = _context?.API.LoadSettingJsonStorage<Settings>() ?? new Settings();
            _apiClient?.Dispose();
            _apiClient = new JoplinApiClient(_settings);

            var searchText = query.Search.Trim();

            // Empty query - show help immediately without connection checks
            if (string.IsNullOrEmpty(searchText))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Create a note in Joplin",
                        SubTitle = "Format: <title> <content> [!notebook]",
                        IcoPath = "icon.png"
                    }
                };
            }

            // Check if API token is configured
            if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "⚠️ Joplin API Token not configured",
                        SubTitle = "Please configure your Joplin API token in plugin settings",
                        IcoPath = "icon.png",
                        Action = _ =>
                        {
                            _context?.API.OpenSettingDialog();
                            return false;
                        }
                    }
                };
            }

            // Check if default notebook is configured
            if (string.IsNullOrWhiteSpace(_settings.DefaultNotebookName))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "⚠️ Default notebook not configured",
                        SubTitle = "Please set a default notebook name in plugin settings",
                        IcoPath = "icon.png",
                        Action = _ =>
                        {
                            _context?.API.OpenSettingDialog();
                            return false;
                        }
                    }
                };
            }

            // Parse query
            var (title, content, notebookName) = ParseQuery(searchText);

            if (string.IsNullOrEmpty(title))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Create a note in Joplin",
                        SubTitle = "Format: <title> <content> [!notebook]",
                        IcoPath = "icon.png"
                    }
                };
            }

            // Show result immediately without checking note existence
            // The check will happen when user presses Enter
            var results = new List<Result>();
            var previewContent = string.IsNullOrEmpty(content) ? "" :
                content.Length > 50 ? $"{content.Substring(0, 50)}..." : content;

            results.Add(new Result
            {
                Title = $"Create/append note: {title}",
                SubTitle = string.IsNullOrEmpty(content)
                    ? "Press Enter to create note or append to existing note with empty content"
                    : $"Content: {previewContent}",
                IcoPath = "icon.png",
                Action = _ =>
                {
                    Task.Run(async () => await CreateOrAppendNoteAsync(title, content, notebookName));
                    return false;
                }
            });

            return results;
        }

        private (string title, string content, string? notebookName) ParseQuery(string query)
        {
            // Extract notebook specification if present (!notebook or !"notebook name")
            string? notebookName = null;
            var notebookPattern = @"\s+!(?:""([^""]+)""|(\S+))$";
            var match = Regex.Match(query, notebookPattern);

            if (match.Success)
            {
                notebookName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                query = query.Substring(0, match.Index).Trim();
            }

            string title;
            string content;

            // Check if query starts with a quote
            if (query.StartsWith("\""))
            {
                var closingQuoteIdx = query.IndexOf('"', 1);
                if (closingQuoteIdx != -1)
                {
                    title = query.Substring(1, closingQuoteIdx - 1);
                    content = query.Substring(closingQuoteIdx + 1).Trim();
                }
                else
                {
                    // No closing quote - return empty to show error
                    return ("", "", null);
                }
            }
            else
            {
                // No quotes - use first word as title, rest as content
                var parts = query.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                title = parts.Length > 0 ? parts[0] : "";
                content = parts.Length > 1 ? parts[1] : "";
            }

            return (title, content, notebookName);
        }

        private async Task CreateOrAppendNoteAsync(string title, string content, string? notebookName)
        {
            try
            {
                if (_apiClient == null || _context == null) return;

                // Reload settings
                _settings = _context.API.LoadSettingJsonStorage<Settings>();
                _apiClient.Dispose();
                _apiClient = new JoplinApiClient(_settings);

                // Test connection
                if (!await _apiClient.TestConnectionAsync())
                {
                    _context.API.ShowMsg("Cannot Connect to Joplin",
                        $"Make sure Joplin is running and Web Clipper is enabled on port {_settings.GetPort()}");
                    return;
                }

                // Get notebook ID if notebook name is provided
                string? notebookId = null;
                if (!string.IsNullOrEmpty(notebookName))
                {
                    notebookId = await _apiClient.GetNotebookIdByNameAsync(notebookName);
                    if (string.IsNullOrEmpty(notebookId))
                    {
                        _context.API.ShowMsg("Notebook Not Found",
                            $"Notebook '{notebookName}' does not exist in Joplin");
                        return;
                    }
                }

                // Check if note already exists
                var existingNote = await _apiClient.GetNoteByTitleAsync(title);

                if (existingNote != null)
                {
                    // Note exists - append content
                    var noteId = existingNote.Id;
                    var existingBody = existingNote.Body ?? "";

                    // Append new content with a newline separator
                    var newBody = existingBody;
                    if (!string.IsNullOrEmpty(existingBody) && !existingBody.EndsWith("\n"))
                    {
                        newBody += "\n";
                    }
                    if (!string.IsNullOrEmpty(content))
                    {
                        newBody += content;
                    }

                    await _apiClient.UpdateNoteAsync(noteId!, body: newBody);

                    _context.API.ShowMsg("Note Updated",
                        $"Appended content to existing note: {title}");
                    
                    // Reset input to jp keyword
                    _context.API.ChangeQuery("jp ");
                }
                else
                {
                    // Note doesn't exist - create new one
                    var result = await _apiClient.CreateNoteAsync(title, content ?? "", notebookId);

                    if (result?.Id != null)
                    {
                        var notebookMsg = !string.IsNullOrEmpty(notebookName) ? $" in '{notebookName}'" : "";
                        _context.API.ShowMsg("Note Created",
                            $"Created new note: {title}{notebookMsg}");
                        
                        // Reset input to jp keyword
                        _context.API.ChangeQuery("jp ");
                    }
                    else
                    {
                        _context.API.ShowMsg("Note Creation Failed",
                            "No note ID returned from Joplin");
                    }
                }
            }
            catch (Exception ex)
            {
                _context?.API.ShowMsg("Error",
                    $"{ex.Message}\n\nCheck that Joplin is running with Web Clipper enabled.");
            }
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new SettingsControl(_context!, _settings!);
        }

        public void Dispose()
        {
            _apiClient?.Dispose();
        }
    }
}
