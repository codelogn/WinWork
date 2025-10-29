using System.Windows;
using System.Windows.Input;
using WinWork.UI.ViewModels;
using WinWork.UI.Utils;
using WinWork.Core.Interfaces;
using System.Windows.Threading;

namespace WinWork.UI.Views;

/// <summary>
/// Interaction logic for LinkDialog.xaml
/// </summary>
public partial class LinkDialog : Window
{
    public LinkDialogViewModel ViewModel => (LinkDialogViewModel)DataContext;

    public LinkDialog(LinkDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Apply consistent styling and transparency to match other windows
        ApplyConsistentStyling();

    // Subscribe to events
    // Note: do NOT subscribe to LinkSaved here. The host (MainWindow) will perform the save
    // operation and close the dialog on success. This prevents the dialog from closing
    // prematurely when validation fails during the save process.
    viewModel.DialogCancelled += OnDialogCancelled;
    viewModel.LinkDeleted += OnLinkDeleted;
    viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Log initial visibility and VM state for diagnostics
        // Defer a visibility/log refresh until after the window has fully loaded and layout pass completed.
        this.Loaded += (s, e) =>
        {
            // Use BeginInvoke to allow bindings and DataTriggers to settle, then force a layout update and log.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Force layout update
                    this.UpdateLayout();
                }
                catch { }
                LogVisibilityAndState();
            }), DispatcherPriority.Loaded);
        };
        
        // Apply consistent styling using WindowStylingHelper
        ApplyConsistentStyling();
    }

    private async void ApplyConsistentStyling()
    {
        try
        {
            // Get settings service from the main window if available
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var settingsService = mainWindow?.ViewModel?.SettingsService;

            // Apply modern chrome effects
            WindowStylingHelper.ApplyModernChrome(this);

            // Apply consistent background and opacity
            WindowStylingHelper.ApplyConsistentStyling(this, settingsService);
        }
        catch { }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }
        else
        {
            // Single click to drag
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LinkDialogViewModel.SelectedLinkType))
        {
            LogVisibilityAndState();
        }
    }

    private void LogVisibilityAndState()
    {
        try
        {
            var vm = DataContext as LinkDialogViewModel;
            
            // Use FindName to safely get the panels
            var terminalPanel = FindName("TerminalPanel") as FrameworkElement;
            var urlPanel = FindName("UrlPanel") as FrameworkElement;
            
            var msg1 = $"LinkDialog: TerminalPanel.Visibility={terminalPanel?.Visibility ?? Visibility.Collapsed}";
            var msg2 = $"LinkDialog: UrlPanel.Visibility={urlPanel?.Visibility ?? Visibility.Collapsed}";
            System.Diagnostics.Debug.WriteLine(msg1);
            System.Diagnostics.Debug.WriteLine(msg2);
            // Also write to the persistent debug log for later inspection
            FileLogger.Log(msg1);
            FileLogger.Log(msg2);
            if (vm != null)
            {
                var vmMsg = $"VM: SelectedLinkType={vm.SelectedLinkType}, TerminalType='{vm.TerminalType}', Url='{vm.Url}', Command='{vm.Command}'";
                System.Diagnostics.Debug.WriteLine(vmMsg);
                FileLogger.Log(vmMsg);
            }
        }
        catch { }
    }

    // Intentionally do not close the dialog on LinkSaved here. MainWindow will handle saving
    // and will close the dialog only when the save completes successfully.

    private void OnDialogCancelled(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnLinkDeleted(object? sender, LinkDeleteEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        // LinkSaved was not subscribed here by design
        ViewModel.DialogCancelled -= OnDialogCancelled;
        ViewModel.LinkDeleted -= OnLinkDeleted;
        
        base.OnClosed(e);
    }
}
