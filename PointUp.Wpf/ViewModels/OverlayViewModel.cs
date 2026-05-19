using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointUp.Application.UseCases;
using PointUp.Core.Interfaces;
using PointUp.Core.Models;

namespace PointUp.Wpf.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly CalculateVelocityThicknessUseCase _velocityUseCase;
    private readonly StrokeLifetimeUseCase _lifetimeUseCase;

    [ObservableProperty]
    private bool _isPointingEnabled;

    public AppSettings Settings => _settingsService.Settings;

    public event EventHandler? ClearRequested;
    public event EventHandler? SettingsReloaded;

    public OverlayViewModel(
        ISettingsService settingsService,
        IGlobalHotkeyService hotkeyService,
        CalculateVelocityThicknessUseCase velocityUseCase,
        StrokeLifetimeUseCase lifetimeUseCase)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _velocityUseCase = velocityUseCase;
        _lifetimeUseCase = lifetimeUseCase;
        _isPointingEnabled = settingsService.Settings.StartEnabled;
    }

    [RelayCommand]
    private void Toggle() => IsPointingEnabled = !IsPointingEnabled;

    [RelayCommand]
    private void Clear() => ClearRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenSettingsFile()
    {
        var path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PointUp", "settings.json");
        if (System.IO.File.Exists(path))
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private void ReloadSettings()
    {
        _settingsService.Load();
        _hotkeyService.Unregister();
        _hotkeyService.Register(_settingsService.Settings.ToggleShortcut, _settingsService.Settings.ClearShortcut);
        SettingsReloaded?.Invoke(this, EventArgs.Empty);
    }

    public double CalculateThickness(double speedPxPerMs, double previousThickness)
    {
        var s = _settingsService.Settings;
        if (!s.VelocityThicknessEnabled) return s.Thickness;
        return _velocityUseCase.Calculate(
            speedPxPerMs, s.MinThickness, s.MaxThickness,
            s.VelocityAtMinThickness, s.VelocityAtMaxThickness,
            previousThickness, s.SmoothingFactor);
    }

    public (double Opacity, bool Expired) CalculateLifetime(long ageMs)
    {
        var s = _settingsService.Settings;
        return _lifetimeUseCase.Calculate(ageMs, s.LifetimeMs, s.FadeMs);
    }
}
