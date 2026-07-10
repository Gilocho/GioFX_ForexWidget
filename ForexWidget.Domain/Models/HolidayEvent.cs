namespace ForexWidget.Domain.Models;

using System;

/// <summary>
/// A bank holiday affecting a currency's liquidity.
/// </summary>
public record HolidayEvent(
    string Currency,       // "USD", "GBP", "JPY", "AUD"
    string Name,           // "Independence Day", "Bank Holiday", etc.
    DateOnly Date
);
