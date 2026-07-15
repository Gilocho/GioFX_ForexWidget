namespace ForexWidget.App.Utils;

using Windows.ApplicationModel;

/// <summary>
/// Versión real del paquete MSIX instalado.
/// </summary>
public static class AppVersionHelper
{
    public static string GetDisplayVersion()
    {
        try
        {
            var v = Package.Current.Id.Version;
            return $"v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
        catch
        {
            // Package.Current lanza si el proceso corre sin identidad de paquete
            // (dotnet run en desarrollo). No es un error: solo no hay versión que leer.
            return "dev build";
        }
    }
}
