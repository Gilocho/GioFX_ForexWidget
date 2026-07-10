namespace ForexWidget.Domain.Interfaces;

public interface INotificationService
{
    void ShowNotification(string title, string message);
    void Initialize();   // Crea el ícono de bandeja, etc.
    void Shutdown();     // Limpieza al cerrar la app
}
