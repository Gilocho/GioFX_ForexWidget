namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

public partial class KillzoneToggleViewModel : ObservableObject
{
    public KillzoneDefinition Definition { get; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _colorHex;
    [ObservableProperty] private Brush _colorBrush;

    public string Name => Definition.Name;

    public KillzoneToggleViewModel(KillzoneDefinition definition)
    {
        Definition = definition;
        _isEnabled = definition.Enabled;
        _colorHex = definition.Color;
        _colorBrush = MakeBrush(definition.Color);
    }

    partial void OnColorHexChanged(string value) => ColorBrush = MakeBrush(value);

    private static Brush MakeBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.Orange; }
    }
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationLoader _configLoader;
    private readonly StartupRegistryService _registry;
    private readonly Action<AppSettings>? _onSaved;
    private readonly AppSettings _original;

    public Action? RequestClose { get; set; }

    [ObservableProperty] private string _theme = "Dark";
    [ObservableProperty] private double _opacity = 0.95;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _showLocalTime;

    public string[] ThemeOptions { get; } = ["Dark", "Light"];
    public ObservableCollection<KillzoneToggleViewModel> Killzones { get; } = new();

    public SettingsViewModel(
        IConfigurationLoader configLoader,
        StartupRegistryService registry,
        Action<AppSettings>? onSaved = null)
    {
        _configLoader = configLoader;
        _registry = registry;
        _onSaved = onSaved;

        _original = configLoader.LoadSettings();
        Theme = _original.Theme;
        Opacity = Math.Clamp(_original.Opacity, 0.5, 1.0);
        StartWithWindows = registry.IsStartWithWindowsEnabled();
        ShowLocalTime = string.Equals(_original.TimeDisplay, "Local", StringComparison.OrdinalIgnoreCase);

        foreach (var kz in configLoader.LoadKillzones())
            Killzones.Add(new KillzoneToggleViewModel(kz));
    }

    [RelayCommand]
    private void Save()
    {
        var updated = _original with
        {
            Theme = Theme,
            Opacity = Opacity,
            TimeDisplay = ShowLocalTime ? "Local" : "UTC"
        };
        _configLoader.SaveSettings(updated);

        _configLoader.SaveKillzones(
            Killzones.Select(k => k.Definition with { Enabled = k.IsEnabled, Color = k.ColorHex }).ToList());

        _registry.SetStartWithWindows(StartWithWindows);

        // Tema en caliente, ANTES del callback onSaved (que dispara Refresh
        // en MainViewModel y regenera las barras con los brushes nuevos)
        if (System.Windows.Application.Current is App app)
            app.ApplyTheme(Theme);

        _onSaved?.Invoke(updated);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
