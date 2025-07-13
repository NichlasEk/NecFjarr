using Avalonia.Controls;
using NecFjärr.ViewModels;

namespace NecFjärr.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
