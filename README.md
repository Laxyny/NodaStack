# NodaStack

<p align="center">
  <strong>A modern, Docker-powered local web server manager for Windows.</strong><br/>
  An intuitive alternative to XAMPP & WAMP, built with .NET (WPF).
</p>

<p align="center">
  <a href="https://github.com/Laxyny/NodaStack/actions/workflows/dotnet-desktop.yml"><img src="https://github.com/Laxyny/NodaStack/actions/workflows/dotnet-desktop.yml/badge.svg" alt="Build Status"></a>
  <a href="https://github.com/Laxyny/NodaStack/releases"><img src="https://img.shields.io/github/v/release/Laxyny/NodaStack" alt="Latest Release"></a>
  <a href="./LICENSE"><img src="https://img.shields.io/github/license/Laxyny/NodaStack" alt="License"></a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Laxyny/NodaStack/main/.github/assets/screenshot.png" alt="NodaStack Screenshot" width="700"/>
</p>

---

## ✨ Features

- **One-Click Services**: Launch Apache, PHP, MySQL, and phpMyAdmin instantly.
- **Docker-Powered**: Leverages the power and isolation of Docker, without the complexity.
- **Project Management**: Easily create, manage, and browse projects in your `www` directory.
- **Modern UI**: A clean and intuitive WPF interface with light and dark modes.
- **Real-time Monitoring**: Built-in logging and service status indicators.
- **Lightweight & Portable**: Use the installer or just run the portable version.

## 🏗️ Architecture Overview

The codebase is organized into two main projects:

- **NodaStack.Core** – Contains domain models shared across the application.
- **NodaStack** – The WPF interface referencing the core library.

This separation keeps core logic independent from the user interface and paves the way for additional front‑ends or services.

## 📚 Table of Contents

- Installation
- Usage
- Getting Started for Developers
- Contributing
- Support the Project
- License

## 🚀 Installation

For most users, the easiest way to get started is to download the latest release.

1.  Go to the [**Releases Page**](https://github.com/Laxyny/NodaStack/releases/latest).
2.  Download the `NodaStack-Installer.exe` for a full installation or `NodaStack-Release.zip` for a portable experience.
3.  Run the installer or extract the zip file and launch `NodaStack.exe`.

**Requirements:**
- Windows 10/11 (x64)
- .NET 9 Runtime (the installer will prompt you if it's missing)

## 🖥️ Usage

1.  **Launch NodaStack**: Open the application.
2.  **Start Services**: Click the `Start` buttons for Apache, PHP, or MySQL as needed. The status indicators will turn green.
3.  **Create a Project**: Type a name in the "New Project" box and click `Create`. Your new project folder will appear in the `www` directory.
4.  **View Your Project**: Select a project from the list and click `Open in Browser` to open it in your browser.

## 🛠️ Getting Started for Developers

Interested in contributing or running the development version?

**Requirements:**
- Windows 10/11 (x64)
- .NET 9 SDK
- Docker Desktop
- Git

```bash
# 1. Clone the repository
git clone https://github.com/Laxyny/NodaStack.git

# 2. Navigate to the project directory
cd NodaStack

# 3. Build the project
dotnet build
```

Press `F5` in Visual Studio or VS Code to launch the application with the debugger.

## 🤝 Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or code contributions, please feel free to get involved.

Please read our [**CONTRIBUTING.md**](./CONTRIBUTING.md) file for guidelines on how to contribute.

## ❤️ Support the Project

If you find NodaStack useful and want to support its development, you can:

- ⭐ **Star the repository** on GitHub.
- ☕ **Buy me a coffee** to help fund development.

<p>
  <a href="https://coff.ee/nodasys"><img src="https://img.shields.io/badge/Buy%20Me%20a%20Coffee-Donate-%23FFDD00" alt="Buy Me a Coffee"></a>
</p>

Your support is greatly appreciated and helps keep the project alive and thriving!

## 📜 License

This project is licensed under the MIT License. See the [**LICENSE**](./LICENSE) file for the full text.

---

<p align="center">
  © 2025 Nodasys — Crafted with ❤️ by Kevin GREGOIRE.
</p>