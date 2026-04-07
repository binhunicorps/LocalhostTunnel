using LocalhostTunnel.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LocalhostTunnel.Desktop.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
    }

    private void OnApplyFilterClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel vm)
        {
            vm.Refresh();
        }
    }

    private void OnLevelFilterClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LogsViewModel vm || sender is not Button button || button.Tag is not string level)
        {
            return;
        }

        vm.SelectedLevel = level;
        vm.Refresh();
    }
}
