using System.Windows;
using System.Windows.Controls;
using WinWork.UI.ViewModels;
using WinWork.UI.Utils;

namespace WinWork.UI.Views
{
    public partial class HotclicksWindow : Window
    {
        public HotclicksWindow(HotclicksViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            
            // Apply consistent styling and transparency to match main window
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
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
                
            var maximizeButton = this.FindName("MaximizeButton") as Button;
            if (maximizeButton != null)
            {
                maximizeButton.Tag = WindowState == WindowState.Maximized ? "🗗" : "🗖";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
