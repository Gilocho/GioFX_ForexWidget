namespace ForexWidget.Core;

using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class KillzoneEngine : IKillzoneEngine
{
    public IReadOnlyList<KillzoneState> GetKillzoneStates(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow)
    {
        var result = new List<KillzoneState>();
        var nowTime = TimeOnly.FromTimeSpan(utcNow.TimeOfDay);

        foreach (var def in definitions)
        {
            if (!def.Enabled)
            {
                result.Add(new KillzoneState(
                    def.Name, false, def.StartUtc, def.EndUtc,
                    def.Color, def.Methodology, null, null));
                continue;
            }

            bool isActive = IsKillzoneActive(def.StartUtc, def.EndUtc, nowTime);

            TimeSpan? timeUntilStart = null;
            TimeSpan? timeUntilEnd = null;

            if (isActive)
                timeUntilEnd = CalculateTimeUntil(nowTime, def.EndUtc);
            else
                timeUntilStart = CalculateTimeUntil(nowTime, def.StartUtc);

            result.Add(new KillzoneState(
                def.Name, isActive, def.StartUtc, def.EndUtc,
                def.Color, def.Methodology, timeUntilStart, timeUntilEnd));
        }

        return result;
    }

    public IReadOnlyList<KillzoneState> GetActiveKillzones(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow)
    {
        return GetKillzoneStates(definitions, utcNow)
            .Where(k => k.IsActive)
            .ToList();
    }

    public KillzoneState? GetNextKillzone(
        IReadOnlyList<KillzoneDefinition> definitions,
        DateTimeOffset utcNow)
    {
        var states = GetKillzoneStates(definitions, utcNow);
        
        var nextKillzones = states
            .Where(k => !k.IsActive && k.TimeUntilStart.HasValue)
            .OrderBy(k => k.TimeUntilStart!.Value)
            .ToList();

        return nextKillzones.FirstOrDefault();
    }

    private bool IsKillzoneActive(TimeOnly start, TimeOnly end, TimeOnly nowTime)
    {
        if (start <= end)
        {
            return nowTime >= start && nowTime <= end;
        }
        else
        {
            return nowTime >= start || nowTime <= end;
        }
    }

    private TimeSpan CalculateTimeUntil(TimeOnly from, TimeOnly target)
    {
        if (target > from)
        {
            return target - from;
        }
        else
        {
            return new TimeSpan(24, 0, 0) - (from - target);
        }
    }
}
