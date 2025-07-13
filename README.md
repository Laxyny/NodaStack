
# NodaStack

**NodaStack** is a modern and user-friendly local web server manager for Windows, built with .NET (WPF).  
It serves as a powerful alternative to XAMPP/WAMP, allowing developers to easily run Apache, PHP, and MySQL locally — with Docker under the hood.

## Releases

[![.NET Core Desktop](https://github.com/Laxyny/NodaStack/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/Laxyny/NodaStack/actions/workflows/dotnet-desktop.yml)

## Features

- Simple graphical interface
- One-click launch for Apache, PHP, and MySQL
- Docker-powered backend (transparent for end users)
- Project directory (`www/`) management
- Built-in logging system

## Structure

```
NodaStack/
├── Docker/           # Docker configurations
├── www/              # Local project directory
├── Views/            # WPF UI (XAML)
├── Services/         # Core service logic (launchers, status, etc.)
├── utils/            # Utilities and helpers
```

## Requirements

- Windows 10/11 (x64)
- .NET 8 or 9 SDK
- Docker Desktop (only required for developers)

## Getting Started

```bash
git clone https://github.com/laxyny/NodaStack.git
cd NodaStack
dotnet build
```

Then press `F5` in VS Code to launch.

## License

Licensed under the MIT License. See [LICENSE](./LICENSE) for details.

---

© 2025 Nodasys — Crafted with ❤️ by Kevin GREGOIRE.
