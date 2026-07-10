namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// A single economic calendar event.
/// </summary>
public record EconomicEvent(
    string Currency,       // "USD", "EUR", "GBP", "JPY", etc.
    string EventName,      // "CPI m/m", "Non-Farm Payrolls", etc.
    string Impact,         // "High", "Medium", "Low"
    DateTimeOffset TimeUtc,
    string? Forecast,
    string? Previous
);
