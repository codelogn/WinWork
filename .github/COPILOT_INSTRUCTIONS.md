# Copilot / Automation Instructions for WinWork repository

This file provides concise guidance for GitHub Copilot or automated assistants that operate on this repository.

Core rules
- Keep changes minimal and safe: prefer small, incremental edits. Avoid wide-reaching refactors without approval.
- Always run `dotnet build` after changes to C# / XAML files and ensure the build succeeds before committing.
- Avoid modifying generated files or files outside the scope of the task unless explicitly requested.

XAML & WPF specifics
- For runtime text color changes, prefer central helpers (e.g., `GetForegroundBrush()`) and set control Foreground/Background using those helpers.
- When modifying templates, bind text color on a `TextBlock` using the attached property `TextBlock.Foreground` where ContentPresenter is used — binding `Foreground` on ContentPresenter can cause XAML compile errors.
- To update live controls after theme changes, walk the visual tree and update control properties; also prefer recreating dynamic content (reload) when practical.

Theme behavior
- Provide both creation-time defaults (when building controls) and runtime-update logic so switching themes updates existing controls.
- Persist theme selection via the app's SettingsService and apply the saved theme on window load; if SettingsService is asynchronous, apply default theme first and reapply once settings are loaded.

Commits and tests
- When implementing bug fixes, include a short test plan in the PR description explaining how to verify the fix manually.
- If changes are larger than a single file, run the project's tests (if present) and include `dotnet build` results in the PR description.

Contact
- If unsure about a design decision, open an issue or ask in a PR discussion before making large changes.
