using System.Windows;
using Vexel.App.ViewModels;

namespace Vexel.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
