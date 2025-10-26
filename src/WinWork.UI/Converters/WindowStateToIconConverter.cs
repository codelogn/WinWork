using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace WinWork.UI.Converters
{
    public class WindowStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WindowState state)
            {
                return state == WindowState.Maximized ? "🗗" : "🗖";
            }
            return "🗖";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}