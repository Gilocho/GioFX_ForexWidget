namespace ForexWidget.Domain.Models;

using ForexWidget.Domain.Enums;
using System;

/// <summary>
/// Runtime state of a session at a specific point in time.
/// </summary>
public record SessionState(
    SessionName Name,
    string DisplayName,
    SessionStatus Status,
    TimeOnly OpenUtc,
    TimeOnly CloseUtc,
    TimeSpan? TimeUntilOpen,   // null if already open
    TimeSpan? TimeUntilClose   // null if already closed
);
