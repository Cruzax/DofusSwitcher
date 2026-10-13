using System.Linq;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public sealed class InitiativeOrderTests
{
    [Fact]
    public void Highest_initiative_plays_first()
    {
        var team = new[] { ("Un", (int?)900), ("Deu", 1500), ("Troi", 1200) };
        Assert.Equal(new[] { "Deu", "Troi", "Un" }, InitiativeOrder.Sort(team, t => t.Item2).Select(t => t.Item1));
    }

    [Fact]
    public void Characters_without_initiative_go_last_in_their_order()
    {
        var team = new[] { ("Un", (int?)null), ("Deu", 800), ("Troi", null), ("Katr", 1000) };
        Assert.Equal(new[] { "Katr", "Deu", "Un", "Troi" }, InitiativeOrder.Sort(team, t => t.Item2).Select(t => t.Item1));
    }

    [Fact]
    public void Ties_keep_the_current_order()
    {
        var team = new[] { ("Un", (int?)1000), ("Deu", 1000) };
        Assert.Equal(new[] { "Un", "Deu" }, InitiativeOrder.Sort(team, t => t.Item2).Select(t => t.Item1));
    }

    [Theory]
    [InlineData("1250", 1250)]
    [InlineData(" 1 250 ", 1250)]
    [InlineData("0", 0)]
    public void Parses_numbers_with_spaces(string text, int expected)
    {
        Assert.True(InitiativeOrder.TryParse(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Empty_means_no_initiative(string? text)
    {
        Assert.True(InitiativeOrder.TryParse(text, out var value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("12a")]
    public void Rejects_anything_else(string text)
    {
        Assert.False(InitiativeOrder.TryParse(text, out _));
    }
}
