namespace ForexWidget.Infrastructure.Configuration;

using System;
using System.IO;

/// <summary>
/// Raíz de datos de usuario en %LOCALAPPDATA%\ForexWidget.
/// Bajo MSIX el directorio de instalación es de solo lectura, así que
/// config, cache y logs nunca deben escribirse junto al exe.
/// </summary>
public static class AppPaths
{
    public static string DataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForexWidget");
}
