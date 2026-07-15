namespace ForexWidget.Domain.Models;

public record AppSettings(
    string Theme,
    bool AlwaysOnTop,
    double Opacity,
    string TimeDisplay,
    string HolidayProvider,
    string FinnhubApiKey,
    string FMPApiKey,
    string Language,
    bool ShowSessionTimes
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
        ShowSessionTimes: false
    );
}
