# FFXIV Client Manager

A comprehensive utility for managing multiple Final Fantasy XIV game clients with isolated configurations, plugins, and settings.

![FFXIV Client Manager Screenshot](screenshot.png)

## Table of Contents

1. [Introduction](#introduction)
2. [Features](#features)
3. [System Requirements](#system-requirements)
4. [Installation](#installation)
5. [Usage Guide](#usage-guide)
    - [Creating Profiles](#creating-profiles)
    - [Launching Clients](#launching-clients)
    - [Managing Plugins](#managing-plugins)
    - [Creating Backups](#creating-backups)
    - [Restoring from Backup](#restoring-from-backup)
6. [Configuration](#configuration)
7. [Building from Source](#building-from-source)
8. [Troubleshooting](#troubleshooting)
9. [Contributing](#contributing)
10. [License](#license)
11. [Acknowledgments](#acknowledgments)
12. [Disclaimer](#disclaimer)

---

## 1. Introduction

The **FFXIV Client Manager** is a powerful utility designed to help you manage and launch multiple FFXIV (Final Fantasy XIV) game clients simultaneously. It allows you to maintain separate configurations, manage plugins, create backups, and monitor running instances—all from a user-friendly interface.

---

## 2. Features

- **Multiple Client Management:** Run multiple FFXIV clients simultaneously with different accounts.
- **Profile System:** Create, edit, and manage client profiles with custom configurations.
- **Plugin Management:** Manage Dalamud plugins across different profiles.
- **Backup & Restore:** Create backups of your configurations and restore them when needed.
- **Process Monitoring:** Track running FFXIV clients with resource usage statistics.
- **Automatic Updates:** Check for updates to the application.

---

## 3. System Requirements

- **Operating System:** Windows 10/11 (64-bit)
- **.NET Runtime:** .NET Framework 4.7.2 or newer (or .NET 8.0 for the latest version)
- **Memory:** At least 8GB RAM (16GB+ recommended for running multiple clients)
- **Dependencies:**
  - [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) must be installed

---

## 4. Installation

1. **Download the Latest Release:**  
   Visit the [Releases page](https://github.com/yourusername/FFXIVClientManager/releases) and download the latest zip file.

2. **Extract the Files:**  
   Extract the contents of the zip file to a location on your computer.

3. **Run the Application:**  
   Navigate to the extracted folder and run `FFXIVClientManager.exe`.

4. **Initial Configuration:**  
   On first run, open the **Settings** dialog to configure the path to your XIVLauncher executable and other settings.

---

## 5. Usage Guide

### Creating Profiles

1. **Click "Add Profile":**  
   Either click the "Add Profile" button on the main interface or select "New Profile" from the File menu.

2. **Enter Profile Details:**  
   - **Profile Name:** Provide a unique name.
   - **Configuration Paths:** Specify where client configurations, plugins, and other data should be stored.
   - **Launch Options:** Set options such as Steam mode, DirectX 11, and others.
   - **Character Details:** Optionally enter your character name, world, etc.

3. **Save the Profile:**  
   Click "Save" to add the profile. The new profile will now appear in the list.

### Launching Clients

#### Launch Selected Profile

1. **Select a Profile:**  
   Click on a profile in the list.
2. **Click "Launch Selected":**  
   The application checks if the XIVLauncher path is configured, ensures no conflicting instances, and launches the client.
3. **Status Update:**  
   The profile status will update to "Running" in the grid.

#### Launch All Profiles

1. **Click "Launch All":**  
   This option launches all profiles sequentially.
2. **Set Launch Delay:**  
   Use the "Delay (sec)" numeric control to set a delay between launches (e.g., between 5 and 120 seconds).
3. **Confirm and Launch:**  
   Confirm the action when prompted. Each profile will be launched in sequence.

### Managing Plugins

1. **Open Plugin Manager:**  
   Select a profile and click "Manage Plugins" or right-click the profile and choose the appropriate option.
2. **Plugin Options:**  
   - View installed plugins
   - Enable or disable plugins
   - Remove unwanted plugins
   - Copy or sync plugins between profiles

### Creating Backups

1. **Create a Backup:**  
   Select a profile and click "Create Backup" (or use the context menu). Backups are automatically stored in the configured backup directory.
2. **Auto-Backup:**  
   If enabled, the application can automatically create backups before launching a client or after a client exits.

### Restoring from Backup

1. **Open the Backup Manager:**  
   Click the "Backup Manager" button or select it from the File menu.
2. **Select a Backup:**  
   Choose a backup from the list for the selected profile.
3. **Restore:**  
   Click "Restore" and confirm the action. The profile’s current configuration will be overwritten with the backup data.

---

## 6. Configuration

The application stores its configuration files in these default locations (which can be customized in the Settings):

- **Settings:** `%AppData%\FFXIVClientManager\settings.json`
- **Profiles:** `%AppData%\FFXIVClientManager\Profiles\`
- **Logs:** `%AppData%\FFXIVClientManager\Logs\`
- **Default Backup Directory:** `%UserProfile%\Documents\FFXIVClientManager\Backups\`

---

## 7. Building from Source

### Prerequisites

- Visual Studio 2019/2022 (Community Edition works fine)
- .NET Framework 4.7.2 SDK or newer (or .NET 8.0 as per project settings)
- NuGet Package Manager

### Build Steps

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/yourusername/FFXIVClientManager.git
