using System.Linq;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class AppConfigTests
{
    private static AppConfig WithCharacters(params string[] names)
    {
        var config = new AppConfig();
        foreach (var name in names) config.Characters.Add(new CharacterConfig { Name = name });
        return config;
    }

    private static string Order(AppConfig config) => string.Join(",", config.Characters.Select(c => c.Name));

    [Theory]
    [InlineData("A", 2, "B,C,A,D")]
    [InlineData("D", 0, "D,A,B,C")]
    [InlineData("b", 3, "A,C,D,B")]
    [InlineData("C", 99, "A,B,D,C")]
    public void Moves_character_to_new_position(string name, int index, string expected)
    {
        var config = WithCharacters("A", "B", "C", "D");
        Assert.True(config.MoveCharacter(name, index));
        Assert.Equal(expected, Order(config));
    }

    [Fact]
    public void Ignores_unknown_character_or_same_position()
    {
        var config = WithCharacters("A", "B", "C");
        Assert.False(config.MoveCharacter("Z", 0));
        Assert.False(config.MoveCharacter("B", 1));
        Assert.Equal("A,B,C", Order(config));
    }
}
