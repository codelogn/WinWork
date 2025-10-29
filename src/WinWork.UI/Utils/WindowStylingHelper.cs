using System.Windows;
using System.Windows.Media;
using WinWork.Core.Services;

namespace WinWork.UI.Utils
{
    /// <summary>
    /// Helper class to apply consistent styling and transparency to all windows in the application
    /// </summary>
    public static class WindowStylingHelper
    {
        /// <summary>
        /// Apply consistent background color, opacity, and styling to a window to match the main window appearance
        /// </summary>
        /// <param name="window">The window to style</param>
        /// <param name="settingsService">Settings service to get saved background color and opacity</param>
        public static async void ApplyConsistentStyling(Window window, ISettingsService? settingsService)
        {
            if (window == null) return;

            try
            {
                // Enable transparency for the window
                window.AllowsTransparency = true;
                window.WindowStyle = WindowStyle.None;
                window.Background = Brushes.Transparent;

                // Apply saved background color and opacity if settings service is available
                if (settingsService != null)
                {
                    try
                    {
                        var bgColor = await settingsService.GetBackgroundColorAsync();
                        if (!string.IsNullOrWhiteSpace(bgColor))
                        {
                            var conv = new BrushConverter();
                            var brush = conv.ConvertFromString(bgColor) as Brush;
                            if (brush != null && window.Content is FrameworkElement content)
                            {
                                // Apply the background to the main content container
                                ApplyBackgroundToContent(content, brush);
                                
                                // Add a delayed retry to ensure it's applied (fix for timing issues)
                                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                                timer.Interval = TimeSpan.FromMilliseconds(200);
                                timer.Tick += (s, e) =>
                                {
                                    timer.Stop();
                                    ApplyBackgroundToContent(content, brush);
                                };
                                timer.Start();
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        var opacityVal = await settingsService.GetBackgroundOpacityAsync();
                        if (opacityVal >= 10 && opacityVal <= 100)
                        {
                            window.Opacity = opacityVal / 100.0;
                        }
                    }
                    catch { }
                }
                else
                {
                    // Apply default styling if no settings service available
                    window.Opacity = 0.95;
                    if (window.Content is FrameworkElement content)
                    {
                        ApplyBackgroundToContent(content, new SolidColorBrush(Color.FromArgb(230, 43, 45, 48))); // Default dark background
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Apply background color to the window content - looks for MainBorder first, then applies to content directly
        /// </summary>
        private static void ApplyBackgroundToContent(FrameworkElement content, Brush background)
        {
            try
            {
                // First check if this is the MainBorder directly
                if (content is System.Windows.Controls.Border border && border.Name == "MainBorder")
                {
                    border.Background = background;
                    return;
                }
                
                // Search for MainBorder in the visual tree
                var mainBorder = FindChildByName<System.Windows.Controls.Border>(content, "MainBorder");
                if (mainBorder != null)
                {
                    mainBorder.Background = background;
                    return;
                }
                
                // Fallback: apply to content directly
                if (content is System.Windows.Controls.Border contentBorder)
                {
                    contentBorder.Background = background;
                }
                else if (content is System.Windows.Controls.Panel panel)
                {
                    panel.Background = background;
                }
                else
                {
                    // For other content types, try to set the background property if it exists
                    var backgroundProperty = content.GetType().GetProperty("Background");
                    if (backgroundProperty != null && backgroundProperty.CanWrite)
                    {
                        backgroundProperty.SetValue(content, background);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Helper method to find a child control by name in the visual tree
        /// </summary>
        private static T? FindChildByName<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.FrameworkElement
        {
            try
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                    
                    if (child is T element && element.Name == name)
                    {
                        return element;
                    }

                    var result = FindChildByName<T>(child, name);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Apply modern window chrome effects similar to the main window
        /// </summary>
        public static void ApplyModernChrome(Window window)
        {
            if (window == null) return;

            try
            {
                var chrome = new System.Windows.Shell.WindowChrome
                {
                    ResizeBorderThickness = new Thickness(6),
                    CaptionHeight = 0,
                    GlassFrameThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    UseAeroCaptionButtons = false,
                    NonClientFrameEdges = System.Windows.Shell.NonClientFrameEdges.None
                };

                System.Windows.Shell.WindowChrome.SetWindowChrome(window, chrome);
            }
            catch { }
        }
    }
}