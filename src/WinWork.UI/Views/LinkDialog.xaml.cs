using System.Windows;
using System.Windows.Input;
using WinWork.UI.ViewModels;
using WinWork.UI.Utils;
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

        // Subscribe to events
        viewModel.LinkSaved += OnLinkSaved;
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
            var msg1 = $"LinkDialog: TerminalPanel.Visibility={TerminalPanel.Visibility}";
            var msg2 = $"LinkDialog: UrlPanel.Visibility={UrlPanel.Visibility}";
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnLinkSaved(object? sender, LinkSaveEventArgs e)
    {
        DialogResult = true;
        Close();
    }

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
        ViewModel.LinkSaved -= OnLinkSaved;
        ViewModel.DialogCancelled -= OnDialogCancelled;
        
        base.OnClosed(e);
    }
}
