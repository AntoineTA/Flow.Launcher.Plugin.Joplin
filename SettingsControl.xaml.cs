using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.Joplin
{
    public partial class SettingsControl : UserControl
    {
        private readonly PluginInitContext _context;
        private readonly Settings _settings;

        public SettingsControl(PluginInitContext context, Settings settings)
        {
            InitializeComponent();
            _context = context;
            _settings = settings;

            // Load current settings
            ApiTokenTextBox.Text = _settings.ApiToken;
            ApiPortTextBox.Text = _settings.ApiPort;
            DefaultNotebookTextBox.Text = _settings.DefaultNotebookName;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update settings object in place
            _settings.ApiToken = ApiTokenTextBox.Text?.Trim() ?? string.Empty;
            _settings.ApiPort = ApiPortTextBox.Text?.Trim() ?? "41184";
            _settings.DefaultNotebookName = DefaultNotebookTextBox.Text?.Trim() ?? string.Empty;

            // Save the updated settings to disk
            _context.API.SavePluginSettings();

            // Show success message
            StatusTextBlock.Text = "✓ Settings saved successfully!";
            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

            // Clear message after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, args) =>
            {
                StatusTextBlock.Text = string.Empty;
                timer.Stop();
            };
            timer.Start();
        }
    }
}
