using System.Windows;
using System.Windows.Interop;
using AutoMate.ViewModels;

namespace AutoMate.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                var h = new WindowInteropHelper(this).Handle;
                vm.AttachHotkeys(h);
            }
        };
    }
}
