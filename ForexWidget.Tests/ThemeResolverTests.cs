namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Theming;
using Xunit;

public class ThemeResolverTests
{
    [Fact]
    public void Case1_Light_ResolvesToLightXaml()
        => Assert.Equal("/Themes/Light.xaml", ThemeResolver.Resolve("Light"));

    [Fact]
    public void Case2_Dark_ResolvesToDarkXaml()
        => Assert.Equal("/Themes/Dark.xaml", ThemeResolver.Resolve("Dark"));

    [Fact]
    public void Case3_Empty_DefaultsToDark()
        => Assert.Equal("/Themes/Dark.xaml", ThemeResolver.Resolve(""));

    [Fact]
    public void Case4_LowercaseLight_IsCaseInsensitive()
        => Assert.Equal("/Themes/Light.xaml", ThemeResolver.Resolve("light"));
}
