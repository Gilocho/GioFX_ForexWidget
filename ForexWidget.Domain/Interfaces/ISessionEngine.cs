namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;

public interface ISessionEngine
{
    MarketState GetMarketState(DateTimeOffset utcNow);
    IReadOnlyList<SessionDefinition> GetSessionDefinitions();
}
