using System;
using System.Collections.Generic;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class CharacterCycleTests
{
    private static readonly string[] Order = { "A", "B", "C", "D" };

    private static Func<string, bool> Online(params string[] names)
    {
        var set = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return set.Contains;
    }

    [Theory]
    [InlineData("A", 1, "B")]
    [InlineData("D", 1, "A")]
    [InlineData("A", -1, "D")]
    [InlineData("C", -1, "B")]
    [InlineData("b", 1, "C")]
    public void Steps_through_all_online(string current, int direction, string expected)
    {
        Assert.Equal(expected, CharacterCycle.Step(Order, Online("A", "B", "C", "D"), current, direction));
    }

    [Fact]
    public void Skips_offline_characters()
    {
        Assert.Equal("D", CharacterCycle.Step(Order, Online("A", "D"), "A", 1));
        Assert.Equal("A", CharacterCycle.Step(Order, Online("A", "D"), "D", 1));
        Assert.Equal("A", CharacterCycle.Step(Order, Online("A", "D"), "D", -1));
    }

    [Fact]
    public void Without_current_starts_at_first_or_last()
    {
        Assert.Equal("A", CharacterCycle.Step(Order, Online("A", "B", "C", "D"), null, 1));
        Assert.Equal("D", CharacterCycle.Step(Order, Online("A", "B", "C", "D"), null, -1));
        Assert.Equal("B", CharacterCycle.Step(Order, Online("B", "C"), "Inconnu", 1));
    }

    [Fact]
    public void Stays_on_current_when_only_one_online()
    {
        Assert.Equal("B", CharacterCycle.Step(Order, Online("B"), "B", 1));
    }

    [Fact]
    public void Returns_null_when_nobody_online_or_empty()
    {
        Assert.Null(CharacterCycle.Step(Order, Online(), "A", 1));
        Assert.Null(CharacterCycle.Step(Array.Empty<string>(), Online("A"), null, 1));
    }
}
