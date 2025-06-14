# PHP Switcher

PHP Switcher is a Windows application that allows you to easily manage and switch between different PHP versions on your system. It is focused on the CLI version of PHP.

## Features

- Download and install multiple PHP versions from official repositories
- Quickly switch between installed versions with a single click
- Manage Thread Safe and Non-Thread Safe versions
- Intuitive and easy-to-use graphical interface
- Automatic environment variable configuration
- Full support for PHP 5.x, 7.x, and 8.x, including all versions and compiler formats
- Support for both x64 and x86 architectures

## System Requirements

- Windows 10/11
- .NET 9.0 or higher
- Administrator permissions (to create symbolic links)

## Installation

1. Download the latest version from the releases section
2. Run the installer or extract the ZIP file
3. Run `PhpSwitcher.exe` as administrator

## Usage

### Installing PHP Versions

1. Go to the "Available Versions" tab
2. Select the PHP version you want to install
3. Click on "Download Selected Version(s)"
4. Wait for the download and installation to complete

### Switching Between PHP Versions

1. Go to the "Installed Versions" tab
2. Select the PHP version you want to activate
3. Click on "Activate Selected Version"
4. The selected version will be immediately available in the command line

### Removing PHP Versions

1. Go to the "Installed Versions" tab
2. Select the PHP version you want to remove
3. Click on "Remove Selected Version"
4. Confirm the removal

## How It Works

PHP Switcher creates a symbolic link at `C:\php` that points to the selected PHP version. It also ensures that `C:\php` is in the system's PATH environment variable.

## Development

### Development Requirements

- Visual Studio 2022 or higher
- .NET 9.0 SDK
- Git

### Cloning the Repository

```bash
git clone https://github.com/your-username/php-Switcher.git
cd php-Switcher
```

### Building

```bash
# Build in Debug mode (development)
dotnet build

# Build in Release mode (production)
dotnet build -c Release
```

### Running

```bash
# Normal execution
dotnet run

# Execution with visible console (debug mode)
dotnet run -- -c

# Execution in development mode
dotnet run -- -d
```

### Generating a Self-Contained Executable

To create a self-contained executable that includes all .NET dependencies:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The executable will be generated in the `bin\Release\net9.0-windows\win-x64\publish\` folder.

### Creating an MSI Installer

To create an MSI installer, WiX Toolset is used:

1. Install WiX Toolset:
```bash
dotnet tool install --global wix
```

2. Create the WiX configuration file (PhpSwitcher.wxs) in the Installer folder.

3. Build the installer:
```bash
cd Installer
wix build PhpSwitcher.wxs -out PhpSwitcher.msi
```

The MSI installer will be generated in the `Installer\` folder.

### Project Structure

To understand the project structure, check the following files:
- [structure.md](structure.md) - Detailed project structure
- [features.md](features.md) - Implemented features
- [task_pending.md](task_pending.md) - Completed tasks
- [CHANGELOG.md](CHANGELOG.md) - Change history

## License

This project is licensed under the MIT License - see the LICENSE file for details.
