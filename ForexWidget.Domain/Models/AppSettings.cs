namespace ForexWidget.Domain.Models;

using ForexWidget.Domain.Enums;

public record AppSettings(
    string Theme,
    bool AlwaysOnTop,
    double Opacity,
    string TimeDisplay,
    string HolidayProvider,
    string FinnhubApiKey,
    string FMPApiKey,
    string Language,
    bool ShowSessionTimes,
    bool MinimalistMode,
    ViewMode ViewMode
)
{
    public static AppSettings Default => new(
        Theme: "Dark",
        AlwaysOnTop: true,
        Opacity: 0.95,
        TimeDisplay: "UTC",
        HolidayProvider: "ForexFactory",
        FinnhubApiKey: "",
        FMPApiKey: "",
        Language: "en",
        ShowSessionTimes: false,
        MinimalistMode: false,
        ViewMode: ViewMode.Bars
    );
}
