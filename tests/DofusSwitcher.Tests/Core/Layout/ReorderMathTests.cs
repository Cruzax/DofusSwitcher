using System.Linq;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class ReorderMathTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 20, 0)]     // moins d'une demi-place : reste en place
    [InlineData(0, 40, 1)]     // plus d'une demi-place : passe à la suivante
    [InlineData(0, 130, 2)]
    [InlineData(0, 999, 3)]    // borné à la dernière place
    [InlineData(3, -70, 2)]
    [InlineData(3, -999, 0)]   // borné à la première place
    public void Finds_target_slot(int from, double offset, int expected)
    {
        Assert.Equal(expected, ReorderMath.TargetIndex(from, offset, stride: 62, count: 4));
    }

    [Fact]
    public void Icons_between_origin_and_target_slide_back_when_moving_forward()
    {
        // L'icône 0 est emmenée à la place 2 : les icônes 1 et 2 reculent d'une place.
        Assert.Equal(new[] { 0, -1, -1, 0 }, new[] { 0, 1, 2, 3 }.Select(i => ReorderMath.Shift(i, from: 0, target: 2)));
    }

    [Fact]
    public void Icons_between_target_and_origin_slide_forward_when_moving_back()
    {
        // L'icône 3 est emmenée à la place 1 : les icônes 1 et 2 avancent d'une place.
        Assert.Equal(new[] { 0, 1, 1, 0 }, new[] { 0, 1, 2, 3 }.Select(i => ReorderMath.Shift(i, from: 3, target: 1)));
    }

    [Fact]
    public void Nothing_moves_when_target_is_origin()
    {
        Assert.Equal(new[] { 0, 0, 0, 0 }, new[] { 0, 1, 2, 3 }.Select(i => ReorderMath.Shift(i, from: 2, target: 2)));
    }
}
