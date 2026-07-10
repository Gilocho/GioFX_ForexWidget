namespace ForexWidget.Infrastructure.Notifications;

using ForexWidget.Domain.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Windows.Forms;

/// <summary>
/// Tray icon (NotifyIcon, menú Show/Exit) + notificaciones nativas de Windows
/// (toast). Con identidad MSIX el toast usa el registro del paquete; sin
/// empaquetar (dev), el toolkit auto-registra el exe — ambos caminos persisten
/// en el Centro de Notificaciones, a diferencia del balloon tip legacy.
/// No sabe de lógica de mercado: recibe título/mensaje y los muestra.
/// </summary>
public class NotificationService : INotificationService, IDisposable
{
    private NotifyIcon? _trayIcon;
    private readonly Action _onShowRequested;
    private readonly Action _onExitRequested;
    private readonly string? _iconPath;

    public NotificationService(Action onShowRequested, Action onExitRequested, string? iconPath = null)
    {
        _onShowRequested = onShowRequested;
        _onExitRequested = onExitRequested;
        _iconPath = iconPath;
    }

    public void Initialize()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Visible = true,
            Text = "ForexWidget — Market Context Engine"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => _onShowRequested());
        menu.Items.Add("Exit", null, (_, _) => _onExitRequested());
        _trayIcon.ContextMenuStrip = menu;

        _trayIcon.DoubleClick += (_, _) => _onShowRequested();
    }

    private System.Drawing.Icon LoadIcon()
    {
        try
        {
            if (_iconPath is not null && System.IO.File.Exists(_iconPath))
                return new System.Drawing.Icon(_iconPath);
        }
        catch
        {
            // Ícono corrupto/ilegible: caer al de sistema, nunca tumbar el arranque
        }
        return System.Drawing.SystemIcons.Application;
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch
        {
            // Si el toast nativo falla (permisos, Focus Assist, entorno raro),
            // caer al balloon tip legacy antes que perder la alerta.
            _trayIcon?.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
        }
    }

    public void Shutdown()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    public void Dispose() => Shutdown();
}
