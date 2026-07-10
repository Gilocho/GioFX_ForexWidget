namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// A concrete alert that should fire right now.
/// </summary>
public record AlertTrigger(
    string EventName,      // "LondonOpen", "KillzoneStart:London Open", etc.
    string Title,          // "London Open in 10 minutes"
    string Message,        // Cuerpo de la notificación
    DateTimeOffset FiredAtUtc
);
