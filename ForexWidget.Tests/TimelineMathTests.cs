namespace ForexWidget.Tests;

using ForexWidget.Core;
using ForexWidget.Domain.Models;
using ForexWidget.Infrastructure.Theming;
using System;
using System.Collections.Generic;
using Xunit;

public class TimelineMathTests
{
    private const double Tolerance = 0.0001;

    [Fact]
    public void Case1_NonOverlappingRanges_ReturnsNull()
    {
        Assert.Null(TimelineMath.IntersectRanges(0.1, 0.1, 0.5, 0.1));
    }

    [Fact]
    public void Case2_PartialOverlap_ReturnsIntersection()
    {
        var result = TimelineMath.IntersectRanges(0.3, 0.3, 0.5, 0.3); // 0.3-0.6 vs 0.5-0.8

        Assert.NotNull(result);
        Assert.Equal(0.5, result.Value.Start, Tolerance);
        Assert.Equal(0.1, result.Value.Width, Tolerance);
    }

    [Fact]
    public void Case3_RangeFullyInsideOther_ReturnsSmallerRange()
    {
        var result = TimelineMath.IntersectRanges(0.2, 0.6, 0.4, 0.1); // 0.4-0.5 dentro de 0.2-0.8

        Assert.NotNull(result);
        Assert.Equal(0.4, result.Value.Start, Tolerance);
        Assert.Equal(0.1, result.Value.Width, Tolerance);
    }

    [Fact]
    public void Case4_IdenticalRanges_ReturnsSameRange()
    {
        var result = TimelineMath.IntersectRanges(0.25, 0.5, 0.25, 0.5);

        Assert.NotNull(result);
        Assert.Equal(0.25, result.Value.Start, Tolerance);
        Assert.Equal(0.5, result.Value.Width, Tolerance);
    }

    [Fact]
    public void Case5_RangesTouchingAtExactEdge_ReturnsNull()
    {
        // aEnd (0.5) == bStart (0.5): contacto, no solapamiento
        Assert.Null(TimelineMath.IntersectRanges(0.3, 0.2, 0.5, 0.2));
    }

    [Fact]
    public void Case6_NoActiveKillzones_GetNextKillzoneReturnsCorrectName()
    {
        var engine = new KillzoneEngine();
        var defs = new List<KillzoneDefinition>
        {
            new("London Open", new TimeOnly(6, 0), new TimeOnly(9, 0), "#00AA00", "MMM", Enabled: true),
            new("New York Open", new TimeOnly(12, 0), new TimeOnly(15, 0), "#FF8800", "MMM", Enabled: true),
        };
        // 10:00 UTC: ninguna activa; la próxima es NY Open (en 2h), no London (mañana)
        var utcNow = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        var next = engine.GetNextKillzone(defs, utcNow);

        Assert.NotNull(next);
        Assert.Equal("New York Open", next.Name);
        Assert.NotNull(next.TimeUntilStart);
        Assert.Equal(2, next.TimeUntilStart.Value.TotalHours, 0.01);
    }
}
