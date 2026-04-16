using LocalhostTunnel.Application.Interfaces;
using LocalhostTunnel.Application.Services;
using LocalhostTunnel.Application.Services.Runtime;
using LocalhostTunnel.Desktop.Services;
using LocalhostTunnel.Desktop.ViewModels;
using LocalhostTunnel.Infrastructure.Forwarding;
using LocalhostTunnel.Infrastructure.Logging;
using LocalhostTunnel.Infrastructure.Storage;
using LocalhostTunnel.Infrastructure.Tavily;
using LocalhostTunnel.Infrastructure.Tunnel;
using LocalhostTunnel.Infrastructure.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace LocalhostTunnel.Desktop;

public static class AppHost
{
    public static IHost Build()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ => AppDataPaths.Build());
                services.AddSingleton<IConfigStore, JsonConfigStore>();
                services.AddSingleton<IProfilesConfigStore, JsonProfilesConfigStore>();
                services.AddSingleton<ISessionStore, JsonSessionStore>();
                services.AddSingleton<ILogStore, ObservableLogStore>();
                services.AddSingleton<ICloudflaredInstaller, CloudflaredInstaller>();
                services.AddSingleton<IForwarderHost, ForwarderHost>();
                services.AddSingleton<ITunnelHost, CloudflaredProcessHost>();
                services.AddSingleton<ITavilyProxyHost, TavilyProxyHost>();
                services.AddSingleton<IForwarderHostFactory, ForwarderHostFactory>();
                services.AddSingleton<ITunnelHostFactory, TunnelHostFactory>();
                services.AddSingleton<ITavilyProxyHostFactory, TavilyProxyHostFactory>();
                services.AddSingleton<HttpClient>();
                services.AddSingleton<IUpdateService>(sp =>
                {
                    var currentVersion = typeof(AppHost).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                    return new GitHubReleaseService(sp.GetRequiredService<HttpClient>(), currentVersion);
                });
                services.AddSingleton<IUpdaterLauncher, DesktopUpdaterLauncher>();
                services.AddSingleton<StartupImportService>();
                services.AddSingleton<RuntimeCoordinator>();
                services.AddSingleton<RuntimeManager>();
                services.AddSingleton<UpdateCoordinator>();
                services.AddSingleton<NavigationService>();
                services.AddSingleton<TrayIconService>();
                services.AddSingleton<WindowLifecycleService>();
                services.AddSingleton<OverviewViewModel>();
                services.AddSingleton<ConfigurationViewModel>();
                services.AddSingleton<TavilyApiViewModel>();
                services.AddSingleton<LogsViewModel>();
                services.AddSingleton<DiagnosticsViewModel>();
                services.AddSingleton<UpdatesViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton(sp => new MainWindow
                {
                    DataContext = sp.GetRequiredService<MainWindowViewModel>()
                });
            })
            .Build();
    }
}
