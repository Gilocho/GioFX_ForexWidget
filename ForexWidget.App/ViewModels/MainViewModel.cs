namespace ForexWidget.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForexWidget.Core;
using ForexWidget.Domain.Enums;
using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using ForexWidget.App.Views;
using ForexWidget.Infrastructure;
using ForexWidget.Infrastructure.Cache;
using ForexWidget.Infrastructure.Configuration;
using ForexWidget.Infrastructure.Notifications;
using ForexWidget.Infrastructure.Providers;
using ForexWidget.Infrastructure.Scheduling;
using ForexWidget.Infrastructure.Theming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

public partial class MainViewModel : ObservableObject
{
    // ── Engines ──────────────────────────────────────────────────────
    private readonly SessionEngine _sessionEngine;
    private readonly KillzoneEngine _killzoneEngine;
    private readonly DstEngine _dstEngine;
    private readonly WeekendEngine _weekendEngine;
    private readonly ConfigurationLoader _configLoader;
    private readonly DispatcherScheduler _scheduler;      // motor de mercado, cada 30s
    private readonly DispatcherScheduler _clockScheduler; // solo reloj visual, cada 1s

    // Cacheado desde settings.json: el tick de 1s no debe leer disco.
    // Se refresca en Refresh() (30s) y al guardar Settings.
    private string _timeDisplayMode = "UTC";

    // ── Providers de datos externos (Sprint 4) ───────────────────────
    private readonly HolidayCache _holidayCache;
    private readonly EconomicCalendarCache _calendarCache;
    private readonly IHolidayProvider _holidayProvider;
    private readonly IEconomicCalendarProvider _calendarProvider;
    private readonly ISystemStatusService _statusService;
    private readonly DailyScheduler _dailyScheduler;
    private bool _statusCheckInProgress;

    // ── Alertas + bandeja (Sprint 5) ─────────────────────────────────
    private readonly IAlertEngine _alertEngine;
    private readonly INotificationService _notificationService;
    private readonly Dictionary<string, DateTimeOffset> _firedAlerts = new();
    // Dedup separado para alertas de día completo (USHoliday): el dedup de 2h de arriba
    // repetiría la notificación cada 2h durante todo el día. Esta clave por fecha calendario
    // dispara una sola vez por día, no una sola vez por ventana de tiempo transcurrido.
    private readonly Dictionary<string, DateOnly> _firedDailyAlerts = new();
    private DateTimeOffset _lastAlertPurge = DateTimeOffset.MinValue;

    // ── Observable properties ─────────────────────────────────────────
    [ObservableProperty] private string _currentTimeUtc = "00:00:00";
    [ObservableProperty] private string _currentDateUtc = "";
    [ObservableProperty] private string _marketPhase = "";
    [ObservableProperty] private string _institutionalActivity = "";
    [ObservableProperty] private string _liquidityLevel = "";
    [ObservableProperty] private Brush _liquidityColor = Brushes.Gray;
    [ObservableProperty] private string _nextMilestone = "";
    [ObservableProperty] private string _nextMilestoneTime = "";
    [ObservableProperty] private bool _isWeekendClosed;
    [ObservableProperty] private bool _isDstWarningActive;
    [ObservableProperty] private string _dstWarningMessage = "";
    [ObservableProperty] private double _nowLinePosition; // 0.0 - 1.0 fracción de 24h

    // ── High Impact News + System Status (Sprint 4) ───────────────────
    [ObservableProperty] private string _nextHighImpactNews = "None scheduled";
    [ObservableProperty] private string _nextHighImpactTime = "";
    [ObservableProperty] private bool _isInternetHealthy = true;
    [ObservableProperty] private string _internetStatus = "checking…";
    [ObservableProperty] private string _holidayDataStatus = "Not yet updated";
    [ObservableProperty] private string _calendarDataStatus = "Not yet updated";
    [ObservableProperty] private double _windowOpacity = 0.95;
    [ObservableProperty] private string _timeDisplayLabel = "UTC";
    [ObservableProperty] private string _nextKillzoneText = "";

    public ObservableCollection<SessionRowViewModel> Sessions { get; } = new();
    public ObservableCollection<KillzoneBarViewModel> Killzones { get; } = new();
    // Leyenda: TODAS las killzones habilitadas (no solo las activas ahora),
    // para saber qué color buscar en el overlay aunque no esté corriendo.
    public ObservableCollection<KillzoneLegendItem> KillzoneLegend { get; } = new();

    public MainViewModel()
    {
        _weekendEngine = new WeekendEngine();
        _dstEngine = new DstEngine();
        _sessionEngine = new SessionEngine(_weekendEngine, _dstEngine);
        _killzoneEngine = new KillzoneEngine();
        _configLoader = new ConfigurationLoader();

        _holidayCache = new HolidayCache();
        _calendarCache = new EconomicCalendarCache();
        // HttpClient compartido: ambos providers leen el mismo feed, y con el
        // mismo cliente ForexFactoryFeed reusa la descarga (<60s) en vez de
        // pedir el XML dos veces por ciclo.
        var feedHttp = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _holidayProvider = new ForexFactoryProvider(_holidayCache, feedHttp);
        _calendarProvider = new EconomicCalendarProvider(_calendarCache, feedHttp);
        _statusService = new SystemStatusService(_holidayProvider, _calendarProvider);

        _alertEngine = new AlertEngine();
        _notificationService = new NotificationService(
            onShowRequested: () =>
            {
                var window = Application.Current.MainWindow;
                window?.Show();
                window?.Activate();
            },
            onExitRequested: () => Application.Current.Shutdown(),
            iconPath: System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        _notificationService.Initialize();
        Application.Current.Exit += (_, _) => _notificationService.Shutdown();

        var startupSettings = _configLoader.LoadSettings();
        WindowOpacity = Math.Clamp(startupSettings.Opacity, 0.5, 1.0);
        _timeDisplayMode = startupSettings.TimeDisplay;

        _scheduler = new DispatcherScheduler(TimeSpan.FromSeconds(30));
        _scheduler.Tick += Refresh;
        _scheduler.Start();

        // Reloj visual desacoplado: 1s, sin recalcular el motor de mercado
        _clockScheduler = new DispatcherScheduler(TimeSpan.FromSeconds(1));
        _clockScheduler.Tick += UpdateClockOnly;
        _clockScheduler.Start();

        // Cache-first: la UI se puebla desde disco antes de tocar la red
        Refresh();

        // La descarga diaria corre en background; nunca bloquea el arranque
        _dailyScheduler = new DailyScheduler(async () =>
        {
            await _holidayProvider.RefreshAsync();
            await _calendarProvider.RefreshAsync();
            Refresh();
        });
        _dailyScheduler.Start();

        // Primera ejecución sin cache: no esperar hasta las 06:00 UTC para tener datos.
        // Solo si el DailyScheduler no disparó ya en Start() (pasadas las 06:00 dispara
        // inmediatamente) — sin este guard se duplican los requests al feed y su
        // rate-limit (HTTP 429) rechaza las descargas de la segunda tanda.
        // El tick de 30s del DispatcherScheduler recoge el resultado — no se toca la UI
        // desde este hilo de fondo.
        if (_calendarCache.GetLastUpdated() is null && _dailyScheduler.LastTriggeredUtc is null)
        {
            _ = Task.Run(async () =>
            {
                await _holidayProvider.RefreshAsync();
                await _calendarProvider.RefreshAsync();
            });
        }
    }

    // SOLO reloj y línea NOW — nada de motor de mercado. Corre cada 1s.
    private void UpdateClockOnly()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var displayOffset = TimeDisplayHelper.GetDisplayOffset(_timeDisplayMode, utcNow);
        TimeDisplayLabel = TimeDisplayHelper.GetDisplayLabel(_timeDisplayMode, utcNow);

        var displayNow = utcNow.ToOffset(displayOffset);
        CurrentTimeUtc = displayNow.ToString("HH:mm:ss");
        CurrentDateUtc = displayNow.ToString("ddd dd MMM yyyy");
        NowLinePosition = TimeDisplayHelper.ToDisplayFraction(
            TimeOnly.FromTimeSpan(utcNow.TimeOfDay), displayOffset);
    }

    private void Refresh()
    {
        var utcNow = DateTimeOffset.UtcNow;

        // ── Modo de display (UTC/Local) desde settings, luego reloj ────
        _timeDisplayMode = _configLoader.LoadSettings().TimeDisplay;
        UpdateClockOnly();
        var displayOffset = TimeDisplayHelper.GetDisplayOffset(_timeDisplayMode, utcNow);

        // ── MarketState desde SessionEngine ───────────────────────────
        var state = _sessionEngine.GetMarketState(utcNow);
        IsWeekendClosed = state.IsWeekendClosed;
        MarketPhase = state.PhaseDisplayName;
        InstitutionalActivity = state.InstitutionalActivity;
        LiquidityLevel = state.LiquidityLevel.ToString().ToUpperInvariant()
            .Replace("VERYHIGH", "VERY HIGH")
            .Replace("VERYLOW", "VERY LOW");
        LiquidityColor = GetLiquidityBrush(state.LiquidityLevel);

        if (state.TimeUntilNextMilestone.HasValue && state.NextMilestoneName != null)
        {
            NextMilestone = state.NextMilestoneName;
            var t = state.TimeUntilNextMilestone.Value;
            NextMilestoneTime = t.TotalHours >= 1
                ? $"{(int)t.TotalHours}h {t.Minutes:D2}m"
                : $"{t.Minutes}m {t.Seconds:D2}s";
        }
        else
        {
            NextMilestone = "";
            NextMilestoneTime = "";
        }

        // ── Sessions para el timeline (con overlays de killzones) ─────
        var kzDefs = _configLoader.LoadKillzones();
        UpdateSessionRows(state.Sessions, displayOffset, kzDefs);

        // ── Leyenda de colores de killzones habilitadas ────────────────
        KillzoneLegend.Clear();
        foreach (var kz in kzDefs.Where(k => k.Enabled))
            KillzoneLegend.Add(new KillzoneLegendItem(kz.Name, new SolidColorBrush(TryParseHex(kz.Color))));

        // ── Killzones ─────────────────────────────────────────────────
        var kzStates = _killzoneEngine.GetKillzoneStates(kzDefs, utcNow);
        UpdateKillzoneRows(kzStates, displayOffset);

        // ── Próxima killzone cuando ninguna está activa ────────────────
        if (!kzStates.Any(k => k.IsActive))
        {
            var next = _killzoneEngine.GetNextKillzone(kzDefs, utcNow);
            NextKillzoneText = next?.TimeUntilStart is not null
                ? $"Next: {next.Name} in {FormatTimeSpan(next.TimeUntilStart.Value)}"
                : "";
        }
        else
        {
            NextKillzoneText = "";
        }

        // ── DST Warning ───────────────────────────────────────────────
        var dstStatus = _dstEngine.GetDstStatus(utcNow);
        IsDstWarningActive = dstStatus.HasActiveTransitionWarning;
        DstWarningMessage = dstStatus.WarningMessage ?? "";

        // ── Alertas ───────────────────────────────────────────────────
        EvaluateAlerts(state, kzStates, dstStatus, utcNow);

        // ── High Impact News (desde cache, nunca red) ──────────────────
        UpdateHighImpactNews(utcNow);

        // ── System Status ─────────────────────────────────────────────
        UpdateDataStatuses();
        RefreshInternetStatusInBackground();
    }

    // Dedup por EventName con supresión de 30 min: la ventana de disparo de 1 min
    // puede abarcar dos minutos de reloj entre ticks de 30s, así que una clave por
    // minuto exacto dispararía dos veces. Ningún evento legítimo se repite en <30 min.
    // Las alertas de día completo (USHoliday) usan un dedup separado por fecha, no por
    // tiempo transcurrido — ver _firedDailyAlerts.
    private void EvaluateAlerts(
        MarketState state, IReadOnlyList<KillzoneState> kzStates, DstStatus dstStatus, DateTimeOffset utcNow)
    {
        var alertDefs = _configLoader.LoadAlerts();
        var holidays = _holidayProvider.GetCachedHolidays();
        var upcomingNews = _calendarProvider.GetUpcomingHighImpact(utcNow, maxCount: 10);
        var triggers = _alertEngine.Evaluate(state, kzStates, dstStatus, alertDefs, holidays, upcomingNews, utcNow);

        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);

        foreach (var trigger in triggers)
        {
            if (trigger.EventName.StartsWith("USHoliday:"))
            {
                if (_firedDailyAlerts.TryGetValue(trigger.EventName, out var firedDate) && firedDate == today)
                    continue;

                _firedDailyAlerts[trigger.EventName] = today;
                _notificationService.ShowNotification(trigger.Title, trigger.Message);
                continue;
            }

            if (_firedAlerts.TryGetValue(trigger.EventName, out var lastFired)
                && utcNow - lastFired < TimeSpan.FromMinutes(30))
                continue;

            _firedAlerts[trigger.EventName] = utcNow;
            _notificationService.ShowNotification(trigger.Title, trigger.Message);
        }

        // Purga horaria: una app 24/7 no puede acumular claves indefinidamente
        if (utcNow - _lastAlertPurge > TimeSpan.FromHours(1))
        {
            _lastAlertPurge = utcNow;
            var expired = _firedAlerts
                .Where(kv => utcNow - kv.Value > TimeSpan.FromHours(2))
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in expired)
                _firedAlerts.Remove(key);

            // Margen de 1 día extra: nunca purgar "hoy" antes de que termine el día.
            var expiredDaily = _firedDailyAlerts
                .Where(kv => (today.DayNumber - kv.Value.DayNumber) > 1)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in expiredDaily)
                _firedDailyAlerts.Remove(key);
        }
    }

    private void UpdateHighImpactNews(DateTimeOffset utcNow)
    {
        var next = _calendarProvider.GetUpcomingHighImpact(utcNow, 1).FirstOrDefault();
        if (next is null)
        {
            NextHighImpactNews = "None scheduled";
            NextHighImpactTime = "";
            return;
        }

        NextHighImpactNews = $"{next.Currency} · {next.EventName}";
        var t = next.TimeUtc - utcNow;
        NextHighImpactTime = t.TotalHours >= 24
            ? next.TimeUtc.ToString("ddd HH:mm", CultureInfo.InvariantCulture)
            : t.TotalHours >= 1
                ? $"{(int)t.TotalHours}h {t.Minutes:D2}m"
                : $"{Math.Max(0, t.Minutes)}m";
    }

    private void UpdateDataStatuses()
    {
        HolidayDataStatus = FormatDataStatus(_holidayCache.GetLastUpdated(), _holidayProvider.GetHealth());
        CalendarDataStatus = FormatDataStatus(_calendarCache.GetLastUpdated(), _calendarProvider.GetHealth());
    }

    private static string FormatDataStatus(DateTimeOffset? lastUpdated, ProviderHealth health)
    {
        if (lastUpdated is null)
            return health.IsHealthy ? "Not yet updated" : "offline · no data";

        var stamp = lastUpdated.Value.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture) + " UTC";
        return health.IsHealthy ? stamp : $"cache · {stamp}";
    }

    // El ping de SystemStatusService bloquea hasta 2s — siempre en hilo de fondo.
    // Solo se actualizan propiedades escalares (WPF marshalea PropertyChanged).
    private void RefreshInternetStatusInBackground()
    {
        if (_statusCheckInProgress) return;
        _statusCheckInProgress = true;

        _ = Task.Run(() =>
        {
            try
            {
                var snapshot = _statusService.GetSnapshot();
                IsInternetHealthy = snapshot.InternetAvailable;
                InternetStatus = snapshot.InternetAvailable ? "online" : "offline";
            }
            catch
            {
                IsInternetHealthy = false;
                InternetStatus = "offline";
            }
            finally
            {
                _statusCheckInProgress = false;
            }
        });
    }

    private void UpdateSessionRows(
        IReadOnlyList<SessionState> sessions, TimeSpan displayOffset,
        IReadOnlyList<KillzoneDefinition> killzoneDefs)
    {
        // Segmentos de killzones habilitadas en fracciones de display
        // (mismo offset que las barras — el overlay respeta el toggle UTC/Local)
        var kzSegments = new List<(double Start, double Width, Brush Color)>();
        foreach (var kz in killzoneDefs.Where(k => k.Enabled))
        {
            var brush = new SolidColorBrush(TryParseHex(kz.Color));
            double kzStart = TimeDisplayHelper.ToDisplayFraction(kz.StartUtc, displayOffset);
            double kzEnd = TimeDisplayHelper.ToDisplayFraction(kz.EndUtc, displayOffset);
            if (kzEnd > kzStart)
            {
                kzSegments.Add((kzStart, kzEnd - kzStart, brush));
            }
            else
            {
                // La killzone cruza medianoche (en tiempo de display): dos segmentos
                kzSegments.Add((kzStart, 1.0 - kzStart, brush));
                kzSegments.Add((0.0, kzEnd, brush));
            }
        }

        Sessions.Clear();
        foreach (var s in sessions)
        {
            var vm = new SessionRowViewModel
            {
                Name = s.DisplayName,
                IsOpen = s.Status == SessionStatus.Open,
                OpenTimeUtc = s.OpenUtc.ToString("HH:mm"),
                CloseTimeUtc = s.CloseUtc.ToString("HH:mm"),
                BarColor = s.Status == SessionStatus.Open
                    ? (Brush)Application.Current.FindResource("SessionOpenBrush")
                    : (Brush)Application.Current.FindResource("SessionClosedBrush")
            };

            // Fracciones desplazadas al modo de display; la relación de orden
            // open/close se preserva, así que el split de medianoche sigue igual.
            double openFrac = TimeDisplayHelper.ToDisplayFraction(s.OpenUtc, displayOffset);
            double closeFrac = TimeDisplayHelper.ToDisplayFraction(s.CloseUtc, displayOffset);

            if (closeFrac > openFrac)
            {
                vm.BarStart = openFrac;
                vm.BarWidth = closeFrac - openFrac;
                vm.HasMidnightCross = false;
            }
            else
            {
                vm.BarStart = openFrac;
                vm.BarWidth = 1.0 - openFrac;
                vm.BarStart2 = 0.0;
                vm.BarWidth2 = closeFrac;
                vm.HasMidnightCross = true;
            }

            // Overlays: intersección de cada segmento de la sesión con cada
            // segmento de killzone (ambos ya desplazados al modo de display)
            var sessionSegments = new List<(double Start, double Width)> { (vm.BarStart, vm.BarWidth) };
            if (vm.HasMidnightCross)
                sessionSegments.Add((vm.BarStart2, vm.BarWidth2));

            foreach (var (ssStart, ssWidth) in sessionSegments)
            {
                foreach (var (kzStart, kzWidth, kzColor) in kzSegments)
                {
                    var overlap = TimelineMath.IntersectRanges(ssStart, ssWidth, kzStart, kzWidth);
                    if (overlap is not null)
                        vm.KillzoneOverlays.Add(new KillzoneOverlaySegment(
                            overlap.Value.Start, overlap.Value.Width, kzColor));
                }
            }

            Sessions.Add(vm);
        }
    }

    private void UpdateKillzoneRows(IReadOnlyList<KillzoneState> states, TimeSpan displayOffset)
    {
        Killzones.Clear();
        foreach (var kz in states.Where(k => k.IsActive))
        {
            var color = TryParseHex(kz.Color);
            double startFrac = TimeDisplayHelper.ToDisplayFraction(kz.StartUtc, displayOffset);
            double endFrac = TimeDisplayHelper.ToDisplayFraction(kz.EndUtc, displayOffset);
            Killzones.Add(new KillzoneBarViewModel
            {
                Name = kz.Name,
                IsActive = true,
                BarStart = startFrac,
                BarWidth = endFrac - startFrac,
                Color = new SolidColorBrush(color),
                TimeUntil = kz.TimeUntilEnd.HasValue
                    ? $"ends in {(int)kz.TimeUntilEnd.Value.TotalMinutes}m"
                    : ""
            });
        }
    }

    private static string FormatTimeSpan(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:D2}m" : $"{t.Minutes}m {t.Seconds:D2}s";

    private static Color TryParseHex(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Colors.Orange; }
    }

    private static SolidColorBrush GetLiquidityBrush(ForexWidget.Domain.Enums.LiquidityLevel level) => level switch
    {
        ForexWidget.Domain.Enums.LiquidityLevel.VeryHigh => new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xAA)),
        ForexWidget.Domain.Enums.LiquidityLevel.High => new SolidColorBrush(Color.FromRgb(0x00, 0xAA, 0x44)),
        ForexWidget.Domain.Enums.LiquidityLevel.Medium => new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x00)),
        ForexWidget.Domain.Enums.LiquidityLevel.Low => new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x00)),
        _ => new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x80)),
    };

    [RelayCommand]
    private void CloseApp()
    {
        // Ya NO cierra la app — la oculta a la bandeja. La única salida real
        // es "Exit" en el menú del ícono de bandeja.
        Application.Current.MainWindow?.Hide();
    }

    [RelayCommand]
    private void OpenSupport()
    {
        var vm = new SupportViewModel();
        var window = new SupportWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        vm.RequestClose = window.Close;
        window.ShowDialog();
    }

    public record KillzoneLegendItem(string Name, Brush Color);

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_configLoader, new StartupRegistryService(),
            onSaved: settings =>
            {
                WindowOpacity = Math.Clamp(settings.Opacity, 0.5, 1.0);
                _timeDisplayMode = settings.TimeDisplay;
                Refresh(); // aplica UTC/Local en vivo: reloj, barras y línea NOW
            });
        var window = new SettingsWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        vm.RequestClose = window.Close;
        window.ShowDialog();
    }
}
