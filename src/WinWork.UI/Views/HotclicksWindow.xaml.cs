using System.Windows;
using WinWork.UI.ViewModels;

namespace WinWork.UI.Views
{
    public partial class HotclicksWindow : Window
    {
        public HotclicksWindow(HotclicksViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
