Manual Test Checklist for recent UI features

1. Save button in Edit Item form
   - Open an existing item for editing.
   - Make a change and click the new Save button at the top-right.
   - Verify the dialog closes (or shows a saved indicator) and the change persists in the main list.
   - Try clicking Save without making changes — the button should be disabled or no-op.

2. Add New with parent preselection
   - Select a folder/item in the left tree.
   - Click the top "Add New" toolbar button.
   - The Add dialog should open with the selected item preselected as the parent.
   - Create the new child item and verify it appears under the chosen parent.

3. Background color picker (Settings → Interface)
   - Open Settings and choose Interface.
   - Click "Pick background color" and select a color in the dialog.
   - Verify the main window background updates immediately to the selected color.
   - Close and re-open the app — the selected background color should persist.

4. General smoke test
   - Start the application and ensure no errors on startup.
   - Verify the app icon is displayed in the window and taskbar.
   - Try opening Settings, Add/Edit dialogs, and ensure they behave correctly.

Notes:
- If the icon doesn't refresh on Windows, rename the executable or clear icon cache to force update.
- For single-file deployments, the runtime icon-loading fallback is used to ensure the icon appears.

5. Publish artifacts
    - After running the publish script, confirm the single-file exe is located at: C:\bin\WinWork\WinWork.UI.exe
    - Verify the exe is ~180MB (includes .NET runtime) and launches without a console window.
