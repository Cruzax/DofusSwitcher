using System;
using System.Globalization;
using System.Windows.Data;

namespace DofusSwitcher.UI;

public static class Converters
{
    /// <summary>Hauteur max de l'étiquette de raccourci à partir de la taille de police (texte + marge interne).</summary>
    public static readonly IValueConverter HotkeyLabelHeight = new FuncConverter(v => v is double size ? size * 1.45 : 15d);

    private sealed class FuncConverter : IValueConverter
    {
        private readonly Func<object?, object> _convert;

        public FuncConverter(Func<object?, object> convert) => _convert = convert;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => _convert(value);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
