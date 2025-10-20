using System.Windows;
using System.Windows.Input;
using WinWork.UI.ViewModels;

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
