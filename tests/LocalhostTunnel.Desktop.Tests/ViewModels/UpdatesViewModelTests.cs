using FluentAssertions;
using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Core.Updates;
using LocalhostTunnel.Desktop.ViewModels;
using System.Net.Http;

namespace LocalhostTunnel.Desktop.Tests.ViewModels;

public sealed class UpdatesViewModelTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_Does_Not_Throw_When_Update_Service_Fails()
    {
        var vm = CreateViewModel(new ThrowingUpdateService(new HttpRequestException("Not Found")));

        Func<Task> act = () => vm.CheckForUpdatesAsync();

        await act.Should().NotThrowAsync();
        vm.IsUpdateAvailable.Should().BeFalse();
        vm.LatestVersion.Should().Be("-");
        vm.ReleaseNotes.Should().Contain("Unable to check updates");
        vm.StatusMessage.Should().Contain("Update check failed");
    }

    private static UpdatesViewModel CreateViewModel(IUpdateService updateService)
    {
        var coordinator = new UpdateCoordinator(updateService, new NoopUpdaterLauncher());
        return new UpdatesViewModel(coordinator);
    }

    private sealed class ThrowingUpdateService : IUpdateService
    {
        private readonly Exception _exception;

        public ThrowingUpdateService(Exception exception)
        {
            _exception = exception;
        }

        public Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromException<ReleaseInfo?>(_exception);
        }
    }

    private sealed class NoopUpdaterLauncher : IUpdaterLauncher
    {
        public Task LaunchAsync(ReleaseInfo release, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
