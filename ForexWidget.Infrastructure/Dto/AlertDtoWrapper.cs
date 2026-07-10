namespace ForexWidget.Infrastructure.Dto;

using System.Collections.Generic;

public class AlertDtoWrapper
{
    public List<AlertDto> Alerts { get; set; } = new();
}

public class AlertDto
{
    public string Event         { get; set; } = "";
    public int    MinutesBefore { get; set; } = 0;
    public bool   Enabled       { get; set; } = true;
}
