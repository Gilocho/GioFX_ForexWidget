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
}
