using System.Linq;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public sealed class MosaicLayoutTests
{
    private static readonly TileRect Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void Four_windows_make_a_two_by_two_grid()
    {
        Assert.Equal(new[]
        {
            new TileRect(0, 0, 960, 540), new TileRect(960, 0, 960, 540),
            new TileRect(0, 540, 960, 540), new TileRect(960, 540, 960, 540),
        }, MosaicLayout.Tiles(4, Screen));
    }

    [Fact]
    public void Two_windows_are_side_by_side()
    {
        Assert.Equal(new[] { new TileRect(0, 0, 960, 1080), new TileRect(960, 0, 960, 1080) },
            MosaicLayout.Tiles(2, Screen));
    }

    [Fact]
    public void Three_windows_use_the_two_by_two_grid()
    {
        var tiles = MosaicLayout.Tiles(3, Screen);
        Assert.Equal(3, tiles.Count);
        Assert.Equal(new TileRect(0, 540, 960, 540), tiles[2]);
    }

    [Fact]
    public void One_window_fills_the_area()
    {
        Assert.Equal(new[] { Screen }, MosaicLayout.Tiles(1, Screen));
    }

    [Fact]
    public void Odd_sizes_leave_no_gap_and_respect_the_offset()
    {
        var tiles = MosaicLayout.Tiles(4, new TileRect(-1920, 10, 1921, 1081));
        Assert.Equal(-1920, tiles[0].X);
        Assert.Equal(tiles[0].X + tiles[0].Width, tiles[1].X);
        Assert.Equal(1, tiles[1].X + tiles[1].Width);
        Assert.Equal(10 + 1081, tiles[3].Y + tiles[3].Height);
    }

    [Fact]
    public void Focus_puts_the_active_window_big_and_the_others_in_a_column()
    {
        Assert.Equal(new[]
        {
            new TileRect(0, 0, 1440, 1080),
            new TileRect(1440, 0, 480, 360), new TileRect(1440, 360, 480, 360), new TileRect(1440, 720, 480, 360),
        }, MosaicLayout.Focus(4, Screen));
    }

    [Fact]
    public void Focus_with_two_windows_and_alone()
    {
        Assert.Equal(new[] { new TileRect(0, 0, 1440, 1080), new TileRect(1440, 0, 480, 1080) }, MosaicLayout.Focus(2, Screen));
        Assert.Equal(new[] { Screen }, MosaicLayout.Focus(1, Screen));
        Assert.Empty(MosaicLayout.Focus(0, Screen));
    }

    [Fact]
    public void Eight_windows_make_a_three_by_three_grid_of_screen_shaped_tiles()
    {
        var tiles = MosaicLayout.Tiles(8, Screen);
        Assert.Equal(8, tiles.Count);
        Assert.All(tiles, t => Assert.Equal((640, 360), (t.Width, t.Height)));
        Assert.Equal(new TileRect(640, 720, 640, 360), tiles[7]);
    }

    [Fact]
    public void Six_windows_use_three_columns_and_two_rows()
    {
        var tiles = MosaicLayout.Tiles(6, Screen);
        Assert.All(tiles, t => Assert.Equal((640, 540), (t.Width, t.Height)));
    }

    [Fact]
    public void Focus_with_eight_surrounds_the_active_window_with_an_L()
    {
        Assert.Equal(new[]
        {
            new TileRect(0, 0, 1440, 810),
            // Colonne de droite, de haut en bas
            new TileRect(1440, 0, 480, 270), new TileRect(1440, 270, 480, 270),
            new TileRect(1440, 540, 480, 270), new TileRect(1440, 810, 480, 270),
            // Rangée du bas, de gauche à droite
            new TileRect(0, 810, 480, 270), new TileRect(480, 810, 480, 270), new TileRect(960, 810, 480, 270),
        }, MosaicLayout.Focus(8, Screen));
    }

    [Fact]
    public void Focus_with_five_fills_the_right_column_first()
    {
        var tiles = MosaicLayout.Focus(5, Screen);
        Assert.Equal(5, tiles.Count);
        Assert.Equal(new TileRect(0, 0, 1440, 810), tiles[0]);
        Assert.All(tiles.Skip(1), t => Assert.Equal(1440, t.X));
    }

    [Fact]
    public void Focus_beyond_the_L_falls_back_to_the_grid()
    {
        Assert.Equal(MosaicLayout.Tiles(9, Screen), MosaicLayout.Focus(9, Screen));
    }

    [Fact]
    public void Interpolation_starts_at_from_and_ends_at_to()
    {
        var from = new TileRect(1440, 0, 480, 360);
        var to = new TileRect(0, 0, 1440, 1080);
        Assert.Equal(from, MosaicLayout.Interpolate(from, to, 0));
        Assert.Equal(to, MosaicLayout.Interpolate(from, to, 1));
        Assert.Equal(to, MosaicLayout.Interpolate(from, to, 5)); // au-delà : reste à l'arrivée
    }

    [Fact]
    public void Interpolation_moves_fast_then_slows_down()
    {
        var from = new TileRect(0, 0, 100, 100);
        var to = new TileRect(1000, 0, 100, 100);
        int half = MosaicLayout.Interpolate(from, to, 0.5).X;
        Assert.True(half > 500, $"à mi-temps on devrait avoir fait plus de la moitié du chemin (x={half})");
        Assert.True(half < 1000);
    }

    [Fact]
    public void No_window_gives_no_tile()
    {
        Assert.Empty(MosaicLayout.Tiles(0, Screen));
    }
}
