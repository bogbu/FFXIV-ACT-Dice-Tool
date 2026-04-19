using System.Windows;
using FFXIVActDiceTool.ViewModels;

namespace FFXIVActDiceTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
