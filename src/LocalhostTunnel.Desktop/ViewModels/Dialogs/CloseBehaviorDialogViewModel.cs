using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LocalhostTunnel.Desktop.ViewModels.Dialogs;

public enum CloseBehaviorResult
{
    RunInTray,
    ExitApplication,
    Cancel
}

public sealed partial class CloseBehaviorDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private CloseBehaviorResult? _result;

    public CloseBehaviorDialogViewModel()
    {
        RunInTrayCommand = new RelayCommand(() => Result = CloseBehaviorResult.RunInTray);
        ExitApplicationCommand = new RelayCommand(() => Result = CloseBehaviorResult.ExitApplication);
        CancelCommand = new RelayCommand(() => Result = CloseBehaviorResult.Cancel);
    }

    public IRelayCommand RunInTrayCommand { get; }

    public IRelayCommand ExitApplicationCommand { get; }

    public IRelayCommand CancelCommand { get; }
}
