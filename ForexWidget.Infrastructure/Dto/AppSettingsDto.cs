namespace ForexWidget.Infrastructure.Dto;

public class AppSettingsDto
{
    public string Theme           { get; set; } = "Dark";
    public bool   AlwaysOnTop     { get; set; } = true;
    public double Opacity         { get; set; } = 0.95;
    public string TimeDisplay     { get; set; } = "UTC";
    public string HolidayProvider { get; set; } = "ForexFactory";
    public string FinnhubApiKey   { get; set; } = "";
    public string FMPApiKey       { get; set; } = "";
    public string Language        { get; set; } = "en";
    public bool   ShowSessionTimes { get; set; } = false;
    public bool   MinimalistMode   { get; set; } = false;

    // null = archivo pre-Sprint 12 sin el campo → ViewMode.Bars.
    // El valor "Minimalist" (enum intermedio de 3 valores que existió durante
    // el desarrollo de Sprint 12) también se acepta y migra — ver el loader.
    public string? ViewMode        { get; set; } = null;
}
