using System.Windows;
using System.Windows.Input;
using WinWork.UI.ViewModels;

namespace WinWork.UI.Views;

/// <summary>
/// Interaction logic for TagManagementDialog.xaml
/// </summary>
public partial class TagManagementDialog : Window
{
    public TagManagementViewModel ViewModel => (TagManagementViewModel)DataContext;

    public TagManagementDialog(TagManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Subscribe to events
        viewModel.CloseRequested += OnCloseRequested;
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
        Close();
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        ViewModel.CloseRequested -= OnCloseRequested;
        
        base.OnClosed(e);
    }
}
