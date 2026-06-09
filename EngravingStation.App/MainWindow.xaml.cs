using System.Windows;
using EngravingStation.App.ViewModels;

namespace EngravingStation.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.CreateDefault();
    }
}
