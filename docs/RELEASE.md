# WinWork Release & Packaging Guide

This document explains how to publish and package the WinWork application for distribution on Windows.

---

## 1. Prerequisites
- Windows 10/11 (x64)
- .NET 9 SDK installed ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- Visual Studio 2022 (17.8+) or VS Code

---

## 2. Build the Application

Open a terminal in the project root and run:

```
dotnet build --configuration Release
```

---

## 3. Publish for Distribution

### Framework-Dependent (requires .NET 9 runtime on target machine)
```
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64 --self-contained false --output ./publish
```

### Self-Contained (no .NET runtime required)
```
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64 --self-contained true --output ./publish
```


### Single-File Executable (recommended for easy distribution)
```
dotnet publish src/WinWork.UI --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output ./publish
```

Or use the automated PowerShell script (recommended):
```
powershell -ExecutionPolicy Bypass -File run_and_publish.ps1
```

---


## 4. Packaging & Branding

- The output will be in the `./publish` folder (or `C:\bin\WinWork` if using the script).
- Distribute the single `.exe` file (or the entire folder for self-contained).
- **Custom icon**: The executable includes a multi-size ICO for full Windows integration (Explorer, taskbar, window).
- If the icon does not appear, try renaming the EXE or clearing the Windows icon cache.
- Include `linker.db` if you want to ship sample data (optional).
- You may compress the folder as a `.zip` for easy download.

---

## 5. Versioning & Release Notes

- Update the version in `AssemblyInfo.cs` or project file before publishing.
- Tag the release in GitHub (e.g., `v1.2.0`).
- Create a release note summarizing changes (see `RECENT_IMPROVEMENTS.md`).

---


## 6. Installation Instructions (for users)

1. Download and extract the `.zip` (if provided) or copy the `.exe` to any folder.
2. Double-click the `.exe` to launch. No installation or .NET runtime required.
3. If the icon does not appear, try renaming the EXE or clearing the Windows icon cache.

- Download and extract the release `.zip`.
- Double-click `WinWork.UI.exe` to run.
- On first run, the database will be created automatically.
- For framework-dependent builds, install .NET 9 runtime if prompted.

---

## 7. Optional: Code Signing

- For production, sign the executable using a code signing certificate.
- Use `signtool.exe` (Windows SDK) or a third-party tool.

---

## 8. Troubleshooting

- If the app does not start, ensure .NET 9 runtime is installed (for framework-dependent).
- For missing dependencies, use the self-contained or single-file option.
- See `USER_MANUAL.md` and `PROJECT_STATUS.md` for more help.

---

## 9. Support & Feedback

- Report issues on the GitHub repository.
- Include OS version, .NET version, and error details.

---

**WinWork is ready for production deployment!**
