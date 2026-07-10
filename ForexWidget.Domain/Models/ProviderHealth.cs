namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// Health/status snapshot of a single data provider.
/// </summary>
public record ProviderHealth(
    string ProviderName,      // "ForexFactory", "Internet", "Timezone DB"
    bool IsHealthy,
    DateTimeOffset? LastSuccessfulUpdate,
    string? LastErrorMessage
);
