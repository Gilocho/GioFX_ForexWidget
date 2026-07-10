namespace ForexWidget.Infrastructure;

using ForexWidget.Domain.Interfaces;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

/// <summary>
/// Aggregates provider health + internet reachability.
/// Nota: CheckInternet hace un ping bloqueante (hasta 2s) — llamar desde
/// un hilo de fondo, nunca desde el hilo de UI.
/// </summary>
public class SystemStatusService : ISystemStatusService
{
    private readonly IHolidayProvider _holidayProvider;
    private readonly IEconomicCalendarProvider _calendarProvider;

    public SystemStatusService(IHolidayProvider holidayProvider, IEconomicCalendarProvider calendarProvider)
    {
        _holidayProvider = holidayProvider;
        _calendarProvider = calendarProvider;
    }

    public SystemStatusSnapshot GetSnapshot()
    {
        bool internetOk = CheckInternet();

        var providers = new List<ProviderHealth>
        {
            new("Internet", internetOk, DateTimeOffset.UtcNow, internetOk ? null : "No connection"),
            _holidayProvider.GetHealth(),
            _calendarProvider.GetHealth(),
        };

        return new SystemStatusSnapshot(providers, internetOk);
    }

    private static bool CheckInternet()
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send("8.8.8.8", 2000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
