﻿using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using DispatchersMonitoringTool.Contracts.Services;
using DispatchersMonitoringTool.Contracts.Views;
using DispatchersMonitoringTool.Core.Contracts.Services;
using DispatchersMonitoringTool.Core.Services;
using DispatchersMonitoringTool.Models;
using DispatchersMonitoringTool.Services;
using DispatchersMonitoringTool.ViewModels;
using DispatchersMonitoringTool.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MonitoringSuiteLibrary.Contracts.Services;
using MonitoringSuiteLibrary.Models;
using MonitoringSuiteLibrary.Services;

namespace DispatchersMonitoringTool;

// For more information about application lifecycle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview

// WPF UI elements use language en-US by default.
// If you need to support other cultures make sure you add converters and review dates and numbers in your UI to ensure everything adapts correctly.
// Tracking issue for improving this is https://github.com/dotnet/wpf/issues/1946
public partial class App : Application
{
    private IHost _host;

    public T GetService<T>()
        where T : class
        => _host.Services.GetService(typeof(T)) as T;

    public App()
    {}

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        // For more information about .NET generic host see  https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0
        _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(appLocation);
                })
                .ConfigureServices(ConfigureServices)
                .Build();

        await _host.StartAsync();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // TODO: Register your services, viewmodels and pages here

        // App Host
        services.AddHostedService<ApplicationHostService>();

        // Activation Handlers

        // Core Services
        services.AddSingleton<IFileService, FileService>();

        // Services
        services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
        services.AddSingleton<ISystemService, SystemService>();
        services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Views and ViewModels
        services.AddTransient<IShellWindow, ShellWindow>();
        services.AddTransient<ShellViewModel>();

        services.AddTransient<FirstResponderViewModel>();
        services.AddTransient<FirstResponderPage>();

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();

        // Configuration

        var secretsConfig = new ConfigurationBuilder().AddUserSecrets<App>().Build();

        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(secretsConfig["SyncfusionKey"]);

        DataServiceConfiguration dataServiceConfiguration = new DataServiceConfiguration();
        dataServiceConfiguration.ConnectionString = secretsConfig["ConnectionString"];

        services.AddSingleton<IDataService>(new DataService(Options.Create<DataServiceConfiguration>(dataServiceConfiguration)));

        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // TODO: Please log and handle the exception as appropriate to your scenario
        // For more info see https://docs.microsoft.com/dotnet/api/system.windows.application.dispatcherunhandledexception?view=netcore-3.0
    }
}
