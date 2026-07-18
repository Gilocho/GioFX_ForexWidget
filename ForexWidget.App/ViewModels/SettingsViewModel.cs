namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForexWidget.Domain.Enums;
using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure;
using ForexWidget.Infrastructure.Theming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

public partial class KillzoneToggleViewModel : ObservableObject
{
    public KillzoneDefinition Definition { get; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _colorHex;
    [ObservableProperty] private Brush _colorBrush;

    public string Name => Definition.Name;
    public bool IsCustom => Definition.IsCustom;

    // Etiqueta informativa de mercado, solo para orientar en la lista —
    // el overlay del timeline se calcula por solapamiento real de horarios
    public string DisplayLabel => Definition.AssociatedMarket is { Length: > 0 } market
        ? $"{Definition.Name} · {market}"
        : Definition.Name;

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
    [ObservableProperty] private bool _showSessionTimes;
    [ObservableProperty] private bool _minimalistMode;
    // "Selected" evita el choque nombre-de-propiedad == nombre-de-tipo (ViewMode)
    [ObservableProperty] private ViewMode _selectedViewMode = ViewMode.Bars;

    public IReadOnlyList<ViewMode> ViewModeOptions { get; } =
        [ViewMode.Bars, ViewMode.Clock];

    // ── Alta de killzones personalizadas ─────────────────────────────
    [ObservableProperty] private bool _isAddingKillzone;
    [ObservableProperty] private string _newKillzoneName = "";
    [ObservableProperty] private string _newKillzoneStartText = "";
    [ObservableProperty] private string _newKillzoneEndText = "";
    [ObservableProperty] private string _newKillzoneMarket = "Other";
    [ObservableProperty] private string _killzoneFormError = "";
    [ObservableProperty] private string _timeInputModeLabel = "";

    public IReadOnlyList<string> MarketOptions { get; } =
        ["Sydney", "Tokyo", "London", "New York", "Other"];

    public bool CanAddCustomKillzone => Killzones.Count(k => k.IsCustom) < 5;

    private const string DefaultCustomColor = "#AA44FF";

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
        ShowSessionTimes = _original.ShowSessionTimes;
        MinimalistMode = _original.MinimalistMode;
        SelectedViewMode = _original.ViewMode;
        UpdateTimeInputModeLabel();

        foreach (var kz in configLoader.LoadKillzones())
            Killzones.Add(new KillzoneToggleViewModel(kz));
    }

    // Sigue el checkbox en vivo (no lo persistido): si el usuario cambia
    // Local/UTC en este mismo diálogo, el formulario debe pedir horas en ese modo
    partial void OnShowLocalTimeChanged(bool value) => UpdateTimeInputModeLabel();

    private void UpdateTimeInputModeLabel()
    {
        string mode = ShowLocalTime ? "Local" : "UTC";
        TimeInputModeLabel = $"Enter times in: {TimeDisplayHelper.GetDisplayLabel(mode, DateTimeOffset.UtcNow)}";
    }

    private TimeSpan CurrentInputOffset()
        => TimeDisplayHelper.GetDisplayOffset(ShowLocalTime ? "Local" : "UTC", DateTimeOffset.UtcNow);

    private void PersistKillzones()
        => _configLoader.SaveKillzones(
            Killzones.Select(k => k.Definition with { Enabled = k.IsEnabled, Color = k.ColorHex }).ToList());

    [RelayCommand]
    private void AddCustomKillzone()
    {
        KillzoneFormError = "";
        IsAddingKillzone = true;
    }

    [RelayCommand]
    private void SaveNewKillzone()
    {
        string name = NewKillzoneName.Trim();
        if (name.Length == 0)
        {
            KillzoneFormError = "Name is required";
            return;
        }

        if (!TimeOnly.TryParseExact(NewKillzoneStartText.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            || !TimeOnly.TryParseExact(NewKillzoneEndText.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            KillzoneFormError = "Times must be in HH:mm format (e.g. 09:30)";
            return;
        }

        if (start == end)
        {
            KillzoneFormError = "Start and end must differ";
            return;
        }

        // El usuario ingresó horas en su modo de display; el storage es siempre UTC
        var offset = CurrentInputOffset();
        var definition = new KillzoneDefinition(
            name,
            TimeDisplayHelper.ToUtcTimeOnly(start, offset),
            TimeDisplayHelper.ToUtcTimeOnly(end, offset),
            DefaultCustomColor,
            "Custom",
            Enabled: true,
            IsCustom: true,
            AssociatedMarket: NewKillzoneMarket == "Other" ? null : NewKillzoneMarket);

        Killzones.Add(new KillzoneToggleViewModel(definition));
        PersistKillzones();

        IsAddingKillzone = false;
        NewKillzoneName = "";
        NewKillzoneStartText = "";
        NewKillzoneEndText = "";
        NewKillzoneMarket = "Other";
        KillzoneFormError = "";
        OnPropertyChanged(nameof(CanAddCustomKillzone));
    }

    [RelayCommand]
    private void CancelNewKillzone()
    {
        IsAddingKillzone = false;
        KillzoneFormError = "";
    }

    [RelayCommand]
    private void DeleteCustomKillzone(KillzoneToggleViewModel kz)
    {
        if (!kz.IsCustom) return; // las MMM por defecto nunca se borran

        Killzones.Remove(kz);
        PersistKillzones();
        OnPropertyChanged(nameof(CanAddCustomKillzone));
    }

    [RelayCommand]
    private void Save()
    {
        var updated = _original with
        {
            Theme = Theme,
            Opacity = Opacity,
            TimeDisplay = ShowLocalTime ? "Local" : "UTC",
            ShowSessionTimes = ShowSessionTimes,
            MinimalistMode = MinimalistMode,
            ViewMode = SelectedViewMode
        };
        _configLoader.SaveSettings(updated);

        PersistKillzones();

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
