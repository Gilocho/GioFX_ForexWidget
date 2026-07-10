namespace ForexWidget.Tests;

using ForexWidget.Core;
using ForexWidget.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class KillzoneEngineTests
{
    private readonly KillzoneEngine _engine = new();
    private readonly IReadOnlyList<KillzoneDefinition> _definitions =
    [
        new("London Open",       new TimeOnly( 6, 0), new TimeOnly( 9, 0), "#00AA00", "MMM", true),
        new("New York Open",     new TimeOnly(12, 0), new TimeOnly(15, 0), "#FF8800", "MMM", true),
        new("London-NY Overlap", new TimeOnly(12, 0), new TimeOnly(16, 0), "#00FFAA", "MMM", true),
        new("Asian Killzone",    new TimeOnly( 0, 0), new TimeOnly( 3, 0), "#4488FF", "MMM", true),
        new("London Close",      new TimeOnly(15, 0), new TimeOnly(16, 0), "#FF4444", "MMM", true),
        new("Disabled KZ",       new TimeOnly(10, 0), new TimeOnly(11, 0), "#000000", "MMM", false)
    ];

    [Fact]
    public void Case1_LondonOpen_730_IsActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 7, 30, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.True(states.First(k => k.Name == "London Open").IsActive);
    }

    [Fact]
    public void Case2_LondonOpen_559_IsNotActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 5, 59, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.False(states.First(k => k.Name == "London Open").IsActive);
    }

    [Fact]
    public void Case3_LondonOpen_600_IsActiveInclusive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 6, 0, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.True(states.First(k => k.Name == "London Open").IsActive);
    }

    [Fact]
    public void Case4_LondonOpen_900_IsActiveInclusive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.True(states.First(k => k.Name == "London Open").IsActive);
    }

    [Fact]
    public void Case5_LondonOpen_901_IsNotActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 9, 1, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.False(states.First(k => k.Name == "London Open").IsActive);
    }

    [Fact]
    public void Case6_AsianKillzone_130_IsActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 1, 30, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.True(states.First(k => k.Name == "Asian Killzone").IsActive);
    }

    [Fact]
    public void Case7_AsianKillzone_2330_IsNotActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 23, 30, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.False(states.First(k => k.Name == "Asian Killzone").IsActive);
    }

    [Fact]
    public void Case8_DisabledKillzone_IsNotActive()
    {
        var date = new DateTimeOffset(2026, 1, 1, 10, 30, 0, TimeSpan.Zero);
        var states = _engine.GetKillzoneStates(_definitions, date);
        Assert.False(states.First(k => k.Name == "Disabled KZ").IsActive);
    }

    [Fact]
    public void Case9_GetActiveKillzones_ReturnsOnlyLondonOpen()
    {
        var date = new DateTimeOffset(2026, 1, 1, 7, 0, 0, TimeSpan.Zero);
        var active = _engine.GetActiveKillzones(_definitions, date);
        Assert.Single(active);
        Assert.Equal("London Open", active.First().Name);
    }

    [Fact]
    public void Case10_GetActiveKillzones_ReturnsNYAndOverlap()
    {
        var date = new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero);
        var active = _engine.GetActiveKillzones(_definitions, date);
        Assert.Equal(2, active.Count);
        Assert.Contains(active, k => k.Name == "New York Open");
        Assert.Contains(active, k => k.Name == "London-NY Overlap");
    }

    [Fact]
    public void Case11_GetNextKillzone_ReturnsNYOpen()
    {
        var date = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var next = _engine.GetNextKillzone(_definitions, date);
        Assert.NotNull(next);
        Assert.Equal("New York Open", next.Name);
        Assert.Equal(TimeSpan.FromHours(2), next.TimeUntilStart);
    }

    [Fact]
    public void Case12_GetNextKillzone_EmptyList_ReturnsNull()
    {
        var date = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var next = _engine.GetNextKillzone(new List<KillzoneDefinition>(), date);
        Assert.Null(next);
    }
}
