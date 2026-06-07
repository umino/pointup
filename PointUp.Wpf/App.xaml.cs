using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PointUp.Application.UseCases;
using PointUp.Core.Interfaces;
using PointUp.Infrastructure.Services;
using PointUp.Wpf.Services;
using PointUp.Wpf.ViewModels;
using PointUp.Wpf.Views;

namespace PointUp.Wpf;

public partial class App
{
    private readonly IHost _host;
    private GlobalHotkeyService? _hotkeyService;
    private TrayIconService? _trayService;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ISettingsService, JsonSettingsService>();
                services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
                services.AddTransient<CalculateVelocityThicknessUseCase>();
                services.AddTransient<StrokeLifetimeUseCase>();
                services.AddSingleton<OverlayViewModel>();
                services.AddSingleton<FloatingBarWindow>();
                services.AddSingleton<OverlayWindow>();
                services.AddSingleton<TrayIconService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var settingsService = _host.Services.GetRequiredService<ISettingsService>();
        settingsService.Load();

        _hotkeyService = (GlobalHotkeyService)_host.Services.GetRequiredService<IGlobalHotkeyService>();
        var s = settingsService.Settings;
        _hotkeyService.Register(s.ToggleShortcut, s.ClearShortcut);

        var vm = _host.Services.GetRequiredService<OverlayViewModel>();
        _hotkeyService.ToggleRequested += (_, _) => vm.ToggleCommand.Execute(null);
        _hotkeyService.ClearRequested += (_, _) => vm.ClearCommand.Execute(null);

        var overlay = _host.Services.GetRequiredService<OverlayWindow>();
        overlay.Show();

        _trayService = _host.Services.GetRequiredService<TrayIconService>();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Unregister();
        _trayService?.Dispose();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
