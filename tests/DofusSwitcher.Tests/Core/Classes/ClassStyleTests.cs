using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class ClassStyleTests
{
    [Theory]
    [InlineData("Iop", "iop")]
    [InlineData("Xélor", "xelor")]
    [InlineData("Féca", "feca")]
    [InlineData("CRA", "cra")]
    [InlineData(" Sacrieur ", "sacrieur")]
    public void Builds_file_key(string className, string expected)
    {
        Assert.Equal(expected, ClassStyle.ToFileKey(className));
    }

    [Fact]
    public void Known_classes_get_their_color_and_unknown_get_default()
    {
        Assert.Equal("#B23A2E", ClassStyle.ColorFor("Iop"));
        Assert.Equal("#3A5BA0", ClassStyle.ColorFor("Xélor"));
        Assert.Equal(ClassStyle.DefaultColor, ClassStyle.ColorFor("Inconnue"));
        Assert.Equal(ClassStyle.DefaultColor, ClassStyle.ColorFor(null));
    }
}
