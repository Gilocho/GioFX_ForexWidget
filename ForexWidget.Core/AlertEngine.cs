namespace ForexWidget.Core;

using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Stateless: dado un momento exacto responde "¿qué alertas aplican ahora?".
/// La ventana de disparo tiene precisión de 1 minuto (el scheduler corre cada 30s);
/// la deduplicación entre ticks es responsabilidad del caller.
/// </summary>
public class AlertEngine : IAlertEngine
{
    public IReadOnlyList<AlertTrigger> Evaluate(
        MarketState marketState,
        IReadOnlyList<KillzoneState> killzoneStates,
        DstStatus dstStatus,
        IReadOnlyList<AlertDefinition> alertDefinitions,
        DateTimeOffset utcNow)
    {
        var triggers = new List<AlertTrigger>();

        foreach (var def in alertDefinitions.Where(a => a.Enabled))
        {
            switch (def.Event)
            {
                case "LondonOpen":
                    CheckSessionAlert(marketState, "London", def, utcNow, isOpen: true, triggers);
                    break;
                case "NYOpen":
                    CheckSessionAlert(marketState, "New York", def, utcNow, isOpen: true, triggers);
                    break;
                case "LondonClose":
                    CheckSessionAlert(marketState, "London", def, utcNow, isOpen: false, triggers);
                    break;
                case "KillzoneStart":
                    CheckKillzoneAlerts(killzoneStates, def, utcNow, triggers);
                    break;
                case "WeekendClose":
                    CheckWeekendAlert(marketState, def, utcNow, triggers);
                    break;
                // "USHoliday" y "HighImpactNews": TODO — requieren extender la firma
                // del método con IHolidayProvider / IEconomicCalendarProvider en un
                // sprint futuro. No se implementan con datos ficticios.
            }
        }

        return triggers;
    }

    private static void CheckSessionAlert(
        MarketState state, string sessionDisplayName, AlertDefinition def,
        DateTimeOffset utcNow, bool isOpen, List<AlertTrigger> triggers)
    {
        var session = state.Sessions.FirstOrDefault(s => s.DisplayName == sessionDisplayName);
        if (session is null) return;

        var relevant = isOpen ? session.TimeUntilOpen : session.TimeUntilClose;
        if (relevant is null) return;

        if (IsWithinTriggerWindow(relevant.Value, def.MinutesBefore))
        {
            string action = isOpen ? "opens" : "closes";
            triggers.Add(new AlertTrigger(
                def.Event,
                $"{sessionDisplayName} {(isOpen ? "Open" : "Close")}",
                $"{sessionDisplayName} {action} in {def.MinutesBefore} minutes",
                utcNow));
        }
    }

    private static void CheckKillzoneAlerts(
        IReadOnlyList<KillzoneState> killzones, AlertDefinition def,
        DateTimeOffset utcNow, List<AlertTrigger> triggers)
    {
        foreach (var kz in killzones.Where(k => !k.IsActive && k.TimeUntilStart.HasValue))
        {
            if (IsWithinTriggerWindow(kz.TimeUntilStart!.Value, def.MinutesBefore))
            {
                triggers.Add(new AlertTrigger(
                    $"KillzoneStart:{kz.Name}",
                    $"{kz.Name} starting soon",
                    $"{kz.Name} killzone starts in {def.MinutesBefore} minutes",
                    utcNow));
            }
        }
    }

    private static void CheckWeekendAlert(
        MarketState state, AlertDefinition def, DateTimeOffset utcNow, List<AlertTrigger> triggers)
    {
        if (state.IsWeekendClosed) return;
        if (state.NextMilestoneName != "Weekend Close" || state.TimeUntilNextMilestone is null) return;

        if (IsWithinTriggerWindow(state.TimeUntilNextMilestone.Value, def.MinutesBefore))
        {
            triggers.Add(new AlertTrigger(
                def.Event,
                "Weekend Close Approaching",
                $"Market closes for the weekend in {def.MinutesBefore} minutes",
                utcNow));
        }
    }

    /// <summary>
    /// True if 'remaining' falls within a 1-minute precision window ending at MinutesBefore.
    /// Example: MinutesBefore=10 fires when remaining is between 9:00 and 10:00 minutes.
    /// </summary>
    private static bool IsWithinTriggerWindow(TimeSpan remaining, int minutesBefore)
    {
        double totalMinutes = remaining.TotalMinutes;
        return totalMinutes <= minutesBefore && totalMinutes > minutesBefore - 1.0;
    }
}
