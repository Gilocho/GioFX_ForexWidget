namespace ForexWidget.Domain.Interfaces;

using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;

public interface IKillzoneEngine
{
    IReadOnlyList<KillzoneState> GetKillzoneStates(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow);

    IReadOnlyList<KillzoneState> GetActiveKillzones(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow);

    KillzoneState? GetNextKillzone(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow);
}
