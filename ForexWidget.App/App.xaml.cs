namespace ForexWidget.App;

using ForexWidget.Infrastructure.Configuration;
using ForexWidget.Infrastructure.Logging;
using ForexWidget.Infrastructure.Theming;
using System;
using System.Threading;
using System.Windows;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsMutex;
    private static FileLogger? _logger;

    /// <summary>
    /// Aplica el tema en frío (OnStartup) o en caliente (desde Settings).
    /// Los converters de App.xaml son entradas directas del diccionario raíz,
    /// no merged — el Clear() de MergedDictionaries no los toca.
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(ThemeResolver.Resolve(themeName), UriKind.Relative)
        });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Tema según settings.json, ANTES de cualquier creación de ventanas:
        // ninguna ruta de arranque puede tocar recursos de tema sin esto cargado.
        var settings = new ConfigurationLoader().LoadSettings();
        ApplyTheme(settings.Theme);

        _singleInstanceMutex = new Mutex(true, "ForexWidget_SingleInstance_Mutex", out _ownsMutex);

        if (!_ownsMutex)
        {
            // Ya hay una instancia corriendo — cerrar esta silenciosamente
            Shutdown();
            return;
        }

        _logger = new FileLogger();
        _logger.Info("Application starting");

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            _logger.Error($"Unhandled exception: {args.ExceptionObject}");
        };

        DispatcherUnhandledException += (_, args) =>
        {
            _logger.Error(args.Exception);
            args.Handled = true; // evitar el crash duro, la app sigue viva
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _logger?.Info("Application exiting");
            _singleInstanceMutex?.ReleaseMutex();
        }
        base.OnExit(e);
    }
}
