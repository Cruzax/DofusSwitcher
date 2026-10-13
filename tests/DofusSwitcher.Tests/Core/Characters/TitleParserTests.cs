using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class TitleParserTests
{
    [Theory]
    [InlineData("Benji-Iop - Iop - 3.1.10.5 - Release", "Benji-Iop")]
    [InlineData("Soignator - Eniripsa - 3.2.0.1 - Release", "Soignator")]
    [InlineData("  Spaces  - Cra - 3.0 - Beta", "Spaces")]
    public void Extracts_character_name(string title, string expected)
    {
        Assert.Equal(expected, TitleParser.Parse(title)?.CharacterName);
    }

    [Theory]
    [InlineData("Benji-Iop - Iop - 3.1.10.5 - Release", "Iop")]
    [InlineData("Tic-Tac - Xélor - 3.2.0.1 - Release", "Xélor")]
    [InlineData("Perso - 3.2.0.1 - Release", null)]
    public void Extracts_class_name(string title, string? expected)
    {
        Assert.Equal(expected, TitleParser.Parse(title)?.ClassName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Dofus")]
    [InlineData("Dofus 3.1.10.5 - Release")]
    [InlineData("Sans separateur")]
    [InlineData(" - Iop - 3.0 - Release")]
    public void Returns_null_when_no_character(string? title)
    {
        Assert.Null(TitleParser.Parse(title));
    }
}
