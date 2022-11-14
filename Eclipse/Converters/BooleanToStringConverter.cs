using System.Windows;
using System.Windows.Media;

namespace Eclipse.Converters
{
    public sealed class BooleanToStringConverter : BooleanConverter<string>
    {
        public BooleanToStringConverter() : base(string.Empty, string.Empty) { }
    }

    public sealed class BooleanToFontWeightConverter : BooleanConverter<FontWeight>
    {
        public BooleanToFontWeightConverter() : base(FontWeights.UltraBold, FontWeights.Light) { }
    }

    public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
    {
        public BooleanToBrushConverter() : base(Brushes.GhostWhite, Brushes.DimGray) { }
    }
}
