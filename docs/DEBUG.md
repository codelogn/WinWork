# Debug Console Window for WinWork.UI

This app supports an optional debug console window for troubleshooting. You can enable or disable the debug console using any of the following methods:

## 1. Command-Line Argument
Run the app with the `--debugconsole` argument:

```
dotnet run --project "src/WinWork.UI/WinWork.UI.csproj" --no-build -- --debugconsole
```
Or, for the built executable:
```
WinWork.UI.exe --debugconsole
```

## 2. Environment Variable
Set the environment variable before running:

**Windows Command Prompt:**
```
set WINWORK_DEBUG_CONSOLE=true
WinWork.UI.exe
```

**PowerShell:**
```
$env:WINWORK_DEBUG_CONSOLE='true'
WinWork.UI.exe
```

## 3. Config File
Create a file named `debugconsole.txt` in the same directory as `WinWork.UI.exe` with the contents:
```
true
```
Then run the app normally.

## How It Works
- When enabled, the debug console window will appear alongside the main UI window.
- When disabled, the console window will be hidden automatically.
- You can use any method above; the app checks all three in priority order: command-line, environment variable, then config file.

## Troubleshooting
- The debug console window only appears when running the built `.exe` directly. It may not show when running via `dotnet run` in VS Code or other terminals.
- If you do not see the console, try running `WinWork.UI.exe` from Windows Explorer or a regular command prompt.

---
For further help, contact the project maintainer or check the source code in `App.xaml.cs` for implementation details.
