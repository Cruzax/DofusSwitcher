using System;

namespace DofusSwitcher.Core;

/// <summary>Calculs du glisser-déposer "à la iPhone" : les icônes s'écartent pour laisser la place.</summary>
public static class ReorderMath
{
    /// <summary>Emplacement visé par l'icône partie de <paramref name="from"/> et déplacée de <paramref name="offset"/>.</summary>
    public static int TargetIndex(int from, double offset, double stride, int count)
    {
        if (count <= 0 || stride <= 0) return from;
        return Math.Clamp(from + (int)Math.Round(offset / stride), 0, count - 1);
    }

    /// <summary>
    /// De combien d'emplacements l'icône <paramref name="index"/> doit glisser pendant que l'icône
    /// <paramref name="from"/> vise <paramref name="target"/> : -1 (recule), 1 (avance) ou 0.
    /// </summary>
    public static int Shift(int index, int from, int target)
    {
        if (index == from) return 0;
        if (from < target && index > from && index <= target) return -1;
        if (target < from && index >= target && index < from) return 1;
        return 0;
    }
}
