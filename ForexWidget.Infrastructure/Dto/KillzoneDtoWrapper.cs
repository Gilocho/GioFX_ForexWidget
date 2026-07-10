namespace ForexWidget.Infrastructure.Dto;

using System.Collections.Generic;

public class KillzoneDtoWrapper
{
    public List<KillzoneDto> Killzones { get; set; } = new();
}

public class KillzoneDto
{
    public string Name        { get; set; } = "";
    public string StartUtc    { get; set; } = "00:00";
    public string EndUtc      { get; set; } = "00:00";
    public string Color       { get; set; } = "#FFFFFF";
    public string Methodology { get; set; } = "Custom";
    public bool   Enabled     { get; set; } = true;
}
