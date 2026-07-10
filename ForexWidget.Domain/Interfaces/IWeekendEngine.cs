namespace ForexWidget.Domain.Interfaces;

using System;

public interface IWeekendEngine
{
    bool IsWeekendClosed(DateTimeOffset utcNow);
    DateTimeOffset GetNextOpen(DateTimeOffset utcNow);
    DateTimeOffset GetNextClose(DateTimeOffset utcNow);
    TimeSpan? TimeUntilOpen(DateTimeOffset utcNow);
    TimeSpan? TimeUntilClose(DateTimeOffset utcNow);
}
