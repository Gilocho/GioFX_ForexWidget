namespace ForexWidget.Domain.Models;

public record AlertDefinition(
    string Event,
    int MinutesBefore,
    bool Enabled
);
