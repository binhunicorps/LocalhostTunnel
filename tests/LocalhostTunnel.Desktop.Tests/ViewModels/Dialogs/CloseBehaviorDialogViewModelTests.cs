using FluentAssertions;
using LocalhostTunnel.Desktop.ViewModels.Dialogs;

namespace LocalhostTunnel.Desktop.Tests.ViewModels.Dialogs;

public sealed class CloseBehaviorDialogViewModelTests
{
    [Fact]
    public void ConfirmRunInTray_Sets_Result_Without_Closing_Runtime()
    {
        var vm = new CloseBehaviorDialogViewModel();

        vm.RunInTrayCommand.Execute(null);

        vm.Result.Should().Be(CloseBehaviorResult.RunInTray);
    }

    [Fact]
    public void ConfirmExit_Sets_Result_To_ExitApplication()
    {
        var vm = new CloseBehaviorDialogViewModel();

        vm.ExitApplicationCommand.Execute(null);

        vm.Result.Should().Be(CloseBehaviorResult.ExitApplication);
    }

    [Fact]
    public void Cancel_Sets_Result_To_Cancel()
    {
        var vm = new CloseBehaviorDialogViewModel();

        vm.CancelCommand.Execute(null);

        vm.Result.Should().Be(CloseBehaviorResult.Cancel);
    }
}
