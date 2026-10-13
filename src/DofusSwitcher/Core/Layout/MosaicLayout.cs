using System;
using System.Collections.Generic;
using System.Linq;

namespace DofusSwitcher.Core;

public readonly record struct TileRect(int X, int Y, int Width, int Height);

/// <summary>Découpe l'écran en cases pour la mosaïque : 2 côte à côte, 3-4 en 2×2, plus en grille.</summary>
public static class MosaicLayout
{
    public static IReadOnlyList<TileRect> Tiles(int count, TileRect area)
    {
        var tiles = new List<TileRect>();
        if (count <= 0) return tiles;

        int columns = count <= 2 ? count : (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (int)Math.Ceiling(count / (double)columns);
        for (int i = 0; i < count; i++)
        {
            int column = i % columns, row = i / columns;
            // Bornes calculées case par case : pas de pixel perdu quand la taille ne se divise pas juste.
            int left = area.X + area.Width * column / columns;
            int right = area.X + area.Width * (column + 1) / columns;
            int top = area.Y + area.Height * row / rows;
            int bottom = area.Y + area.Height * (row + 1) / rows;
            tiles.Add(new TileRect(left, top, right - left, bottom - top));
        }
        return tiles;
    }

    /// <summary>Nombre de petites cases autour du perso actif quand l'équipe dépasse 4 persos.</summary>
    public const int RingSize = 7;

    /// <summary>
    /// Mosaïque "perso actif en grand" : la case 0 est le perso actif. Jusqu'à 4 persos, il prend les 3/4 de la
    /// largeur et les autres se partagent la colonne de droite ; au-delà, il prend 3/4 × 3/4 et les autres
    /// l'entourent en L (colonne de droite puis rangée du bas), dans des cases à la forme de l'écran.
    /// Seul, le perso prend tout l'écran.
    /// </summary>
    public static IReadOnlyList<TileRect> Focus(int count, TileRect area)
    {
        var tiles = new List<TileRect>();
        if (count <= 0) return tiles;
        if (count == 1)
        {
            tiles.Add(area);
            return tiles;
        }
        if (count > 4) return FocusWithRing(count, area);

        int mainWidth = area.Width * 3 / 4;
        tiles.Add(new TileRect(area.X, area.Y, mainWidth, area.Height));
        int others = count - 1;
        for (int i = 0; i < others; i++)
        {
            int top = area.Y + area.Height * i / others;
            int bottom = area.Y + area.Height * (i + 1) / others;
            tiles.Add(new TileRect(area.X + mainWidth, top, area.Width - mainWidth, bottom - top));
        }
        return tiles;
    }

    /// <summary>5 à 8 persos : grande case en haut à gauche, petites cases en L sur une grille de 4×4.</summary>
    private static IReadOnlyList<TileRect> FocusWithRing(int count, TileRect area)
    {
        // Au-delà de ce que le L peut accueillir, la grille reste la seule disposition lisible.
        if (count - 1 > RingSize) return Tiles(count, area);

        int X(int column) => area.X + area.Width * column / 4;
        int Y(int row) => area.Y + area.Height * row / 4;
        TileRect Cell(int column, int row) => new(X(column), Y(row), X(column + 1) - X(column), Y(row + 1) - Y(row));

        var tiles = new List<TileRect> { new(area.X, area.Y, X(3) - area.X, Y(3) - area.Y) };
        // Colonne de droite de haut en bas, puis rangée du bas de gauche à droite.
        var ring = new List<TileRect>();
        for (int row = 0; row < 4; row++) ring.Add(Cell(3, row));
        for (int column = 0; column < 3; column++) ring.Add(Cell(column, 3));
        tiles.AddRange(ring.Take(count - 1));
        return tiles;
    }

    /// <summary>Position intermédiaire de la transition : <paramref name="progress"/> va de 0 (départ) à 1 (arrivée).</summary>
    public static TileRect Interpolate(TileRect from, TileRect to, double progress)
    {
        double eased = EaseOut(Math.Clamp(progress, 0, 1));
        static int Lerp(int a, int b, double t) => (int)Math.Round(a + (b - a) * t);
        return new TileRect(Lerp(from.X, to.X, eased), Lerp(from.Y, to.Y, eased),
            Lerp(from.Width, to.Width, eased), Lerp(from.Height, to.Height, eased));
    }

    /// <summary>Démarre vite et ralentit à l'arrivée : le mouvement paraît plus naturel.</summary>
    public static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);
}
