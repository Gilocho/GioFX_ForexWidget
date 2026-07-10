namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;

public interface IAlertEngine
{
    /// <summary>
    /// Evaluates current market state against alert definitions and returns
    /// any triggers that should fire right now. The engine is stateless:
    /// deduplication of "already fired" is the caller's responsibility,
    /// keyed on AlertTrigger.EventName.
    /// </summary>
    IReadOnlyList<AlertTrigger> Evaluate(
        MarketState marketState,
        IReadOnlyList<KillzoneState> killzoneStates,
        DstStatus dstStatus,
        IReadOnlyList<AlertDefinition> alertDefinitions,
        DateTimeOffset utcNow);
}
