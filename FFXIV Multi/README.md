# FFXIV Client Manager

A comprehensive utility for managing multiple Final Fantasy XIV game clients with isolated configurations, plugins, and settings.

![FFXIV Client Manager Screenshot](screenshot.png)

## Features

- **Multiple Client Management**: Run multiple FFXIV clients simultaneously with different accounts
- **Profile System**: Create, edit, and manage client profiles with custom configurations
- **Plugin Management**: Manage Dalamud plugins across different profiles
- **Backup & Restore**: Create backups of your configurations and restore them when needed
- **Process Monitoring**: Track running FFXIV clients with resource usage statistics
- **Automatic Updates**: Check for updates to the application

## System Requirements

- Windows 10/11 (64-bit)
- .NET Framework 4.7.2 or newer
- At least 8GB RAM (16GB+ recommended for running multiple clients)
- [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) installed

## Installation

1. Download the latest release from the [Releases page](https://github.com/yourusername/FFXIVClientManager/releases)
2. Extract the zip file to any location on your computer
3. Run `FFXIVClientManager.exe`
4. On first run, configure the path to your XIVLauncher executable

## Usage Guide

### Creating Profiles

1. Click "Add Profile" to create a new profile
2. Enter a name for the profile
3. Configure the profile settings:
   - **Config Path**: Where client-specific configurations will be stored
   - **Plugin Path**: Where client-specific Dalamud plugins will be stored
   - **Game Path** (optional): Custom FFXIV installation path if you have multiple installations
4. Configure launch options (Steam, DirectX 11, etc.)
5. Add character details (optional)
6. Click "Save" to create the profile

### Launching Clients

1. Select a profile from the list
2. Click "Launch Selected" to launch the client
3. To launch multiple clients, click "Launch All" (each client will launch with its own isolated configuration)

### Managing Plugins

1. Select a profile and click "Manage Plugins" or right-click and select "Manage Plugins"
2. In the Plugin Manager:
   - View installed plugins
   - Enable/disable plugins for the selected profile
   - Remove plugins
   - Copy plugins to other profiles
   - Sync plugins between profiles

### Creating Backups

1. Select a profile and click "Create Backup" or right-click and select "Create Backup"
2. Backups are stored in the configured backup directory
3. Use the Backup Manager to view, restore, or delete backups

### Restoring from Backup

1. Open the Backup Manager
2. Select a profile and backup
3. Click "Restore" to restore the configuration from the backup

## Building from Source

### Prerequisites

- Visual Studio 2019/2022 (Community Edition is sufficient)
- .NET Framework 4.7.2 SDK or newer
- NuGet package manager

### Build Steps

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/FFXIVClientManager.git
   ```

2. Open `FFXIVClientManager.sln` in Visual Studio

3. Restore NuGet packages:
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"

4. Build the solution:
   - Press F6 or go to Build → Build Solution

5. Run the application:
   - Press F5 or go to Debug → Start Debugging

### Project Structure

- **FFXIVClientManager**: Main application project
  - **Models**: Data models
  - **Forms**: User interface forms
  - **Services**: Core services for managing clients, plugins, etc.
  - **Utils**: Utility classes for common operations

## Configuration

The application stores configuration files in the following locations:

- **Settings**: `%AppData%\FFXIVClientManager\settings.json`
- **Profiles**: `%AppData%\FFXIVClientManager\Profiles\`
- **Logs**: `%AppData%\FFXIVClientManager\Logs\`
- **Default Backup Directory**: `%UserProfile%\Documents\FFXIVClientManager\Backups\`

These paths can be customized in the application settings.

## Troubleshooting

### Common Issues

1. **XIVLauncher not found**
   - Ensure XIVLauncher is installed
   - Set the correct path to XIVLauncher.exe in Settings

2. **Cannot launch multiple clients**
   - Ensure each profile has unique Config and Plugin paths
   - Check system resource usage (RAM, CPU)

3. **Plugins not loading**
   - Verify the Plugin path is correct
   - Check if Dalamud is enabled in the profile settings

4. **Game crashes on launch**
   - Try disabling Dalamud plugins 
   - Ensure sufficient system resources are available

### Logs

Log files are stored in the configured log directory. When reporting issues, please include the relevant log files.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) - The custom launcher that makes this possible
- [Dalamud](https://github.com/goatcorp/Dalamud) - The plugin framework for FFXIV

## Disclaimer

This software is not affiliated with SQUARE ENIX CO., LTD. FINAL FANTASY is a registered trademark of SQUARE ENIX CO., LTD. All trademarks are the property of their respective owners.