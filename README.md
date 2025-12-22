# Flow Launcher Joplin Plugin

A Flow Launcher plugin for quickly creating and editing notes in Joplin.

## Features

- **Quick Note Creation**: Type `jp title content` to instantly create a new note in Joplin
- **Smart Appending**: Automatically appends to existing notes with the same title instead of creating duplicates
- **Multi-word Titles**: Use quotes for titles with spaces: `jp "Shopping List" milk`
- **Inline Notebook Selection**: Specify target notebook per-note using `!notebook` syntax
- **Background Operation**: Notes are created/updated silently without opening Joplin
- **Easy Configuration**: Configure API token and settings via Flow Launcher's settings UI

## Installation

### Prerequisites

1. **Joplin Desktop**: Install [Joplin](https://joplinapp.org/) on your system
2. **Flow Launcher**: Install [Flow Launcher](https://www.flowlauncher.com/)
3. **Enable Joplin Web Clipper**:
   - Open Joplin
   - Go to `Tools > Options > Web Clipper`
   - Enable the Web Clipper service
   - Note the port number (default: 41184)
   - Copy the API token shown

### Plugin Installation

1. Open Flow Launcher
2. Type `pm install Joplin`
3. Press Enter to install

## Configuration

1. Open Flow Launcher settings
2. Go to the Joplin plugin settings
3. Configure the following:

   - **API Token**: Paste the token from Joplin Web Clipper settings
   - **API Port**: Port number (default: 41184)
   - **Default Notebook Name**: Name of the notebook where notes will be created (e.g., "Inbox", "Quick Notes")

## Usage

### Create a New Note

```
jp <note_title> <content>
jp <note_title> <content> !<notebook>
jp "<note title>" <content> !"<notebook name>"
```

**Basic Examples:**

- `jp Meeting Remember to discuss the new project`
- `jp "Shopping List" Milk, eggs, bread`
- `jp TODO Review pull requests`

**With Notebook Selection:**

- `jp Meeting Discuss Q4 goals !work` - Create in "work" notebook
- `jp "Shopping List" Milk !personal` - Create in "personal" notebook
- `jp Ideas New feature concept !"Project Notes"` - Create in "Project Notes" notebook

### Notebook Selection

You can specify which notebook to use by adding `!notebook` at the end of your command:

- Single-word notebook: `jp title content !work`
- Multi-word notebook: `jp title content !"My Notebook"`

**Priority order:**

1. Inline `!notebook` (highest priority - overrides default)
2. Default notebook from plugin settings (required)
3. If inline notebook is not found, falls back to default notebook

## How It Works

1. Type `jp` in Flow Launcher
2. Enter your note title and content (optionally with `!notebook`)
3. The plugin will:
   - Show whether it will create a new note or append to an existing one
   - Create/update the note in Joplin when you press Enter
   - Show a confirmation notification

## Requirements

- .NET 8.0 or higher
- Flow Launcher v1.8+
- Joplin Desktop with Web Clipper enabled

## Building from Source

```bash
dotnet restore
dotnet build
```

## Troubleshooting

### "Cannot connect to Joplin" error

1. Make sure Joplin is running
2. Verify Web Clipper service is enabled in Joplin settings
3. Check that the port number in plugin settings matches Joplin's Web Clipper port

### "API Token not configured" error

1. Open Joplin settings (Tools > Options > Web Clipper)
2. Copy the API token
3. Open Flow Launcher settings for Joplin plugin
4. Paste the token in the API Token field

## License

MIT
