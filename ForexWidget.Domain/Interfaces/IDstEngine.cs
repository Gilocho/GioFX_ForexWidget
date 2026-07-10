namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System;

public interface IDstEngine
{
    DstStatus GetDstStatus(DateTimeOffset utcNow);
    DstInfo GetRegionDstInfo(string timeZoneId, string regionName, DateTimeOffset utcNow);
    bool IsTransitionWarningActive(DateTimeOffset utcNow);
}
