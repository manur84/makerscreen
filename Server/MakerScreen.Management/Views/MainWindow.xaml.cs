using System.Windows;
using MakerScreen.Management.ViewModels;

namespace MakerScreen.Management.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
