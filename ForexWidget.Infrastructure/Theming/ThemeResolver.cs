namespace ForexWidget.Infrastructure.Theming;

public static class ThemeResolver
{
    /// <summary>
    /// Maps a theme name to its ResourceDictionary pack URI.
    /// Defaults to Dark for unknown/empty values.
    /// </summary>
    public static string Resolve(string? themeName) => themeName?.Trim().ToLowerInvariant() switch
    {
        "light" => "/Themes/Light.xaml",
        "dark" => "/Themes/Dark.xaml",
        _ => "/Themes/Dark.xaml"
    };
}
