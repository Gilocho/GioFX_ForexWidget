namespace ForexWidget.App;

/// <summary>
/// Publisher donation info. Ships baked into the app — edit these values
/// with your real payment details BEFORE building for distribution.
/// Public wallet addresses are safe to commit (unlike API keys) — only
/// private keys must never be exposed.
/// </summary>
public static class SupportInfo
{
    public const string PayPalMeUrl = "https://paypal.me/Giodow";

    public const string UsdtTrc20Address = "TMgpufHnuhQLBZxr4LTF6wY3ZyCibQnhJc";
    public const string UsdtBep20Address = "0x2f4fc2f588d6222b34b997fe2d6b9ad14f6bebce";
    public const string UsdtErc20Address = "0x2f4fc2f588d6222b34b997fe2d6b9ad14f6bebce";

    private const string PlaceholderMarker = "PASTE_YOUR";

    /// <summary>
    /// True if any donation value is still an unedited placeholder.
    /// Used to show a warning in the UI instead of silently shipping fake addresses.
    /// </summary>
    public static bool HasUnconfiguredPlaceholders =>
        PayPalMeUrl.Contains(PlaceholderMarker) ||
        UsdtTrc20Address.Contains(PlaceholderMarker) ||
        UsdtBep20Address.Contains(PlaceholderMarker) ||
        UsdtErc20Address.Contains(PlaceholderMarker);
}
