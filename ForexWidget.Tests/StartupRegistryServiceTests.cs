namespace ForexWidget.Tests;

using ForexWidget.Infrastructure;
using System;
using Xunit;

public class StartupRegistryServiceTests
{
    // Cada test usa un nombre de valor único para no tocar la entrada real
    // de producción "ForexWidget", y limpia al terminar.
    private static string UniqueTestName() => "ForexWidget_Test_" + Guid.NewGuid().ToString("N");

    [Fact]
    public void Case1_SetTrue_IsEnabledReturnsTrue()
    {
        var service = new StartupRegistryService(UniqueTestName());
        try
        {
            service.SetStartWithWindows(true);
            Assert.True(service.IsStartWithWindowsEnabled());
        }
        finally
        {
            service.SetStartWithWindows(false);
        }
    }

    [Fact]
    public void Case2_SetFalseAfterTrue_IsEnabledReturnsFalse()
    {
        var service = new StartupRegistryService(UniqueTestName());
        try
        {
            service.SetStartWithWindows(true);
            service.SetStartWithWindows(false);
            Assert.False(service.IsStartWithWindowsEnabled());
        }
        finally
        {
            service.SetStartWithWindows(false);
        }
    }

    [Fact]
    public void Case3_CleanRegistry_IsEnabledReturnsFalseWithoutException()
    {
        var service = new StartupRegistryService(UniqueTestName());

        Assert.False(service.IsStartWithWindowsEnabled());
    }

    [Fact]
    public void Case4_SetFalseWithoutPriorSet_DoesNotThrow()
    {
        var service = new StartupRegistryService(UniqueTestName());

        service.SetStartWithWindows(false); // no debe lanzar

        Assert.False(service.IsStartWithWindowsEnabled());
    }
}
