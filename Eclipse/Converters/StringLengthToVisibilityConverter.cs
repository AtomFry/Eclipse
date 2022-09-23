using System.Windows;

namespace Eclipse.Converters
{
    public class StringLengthToVisibilityConverter : StringLengthConverter<Visibility>
    {
        public StringLengthToVisibilityConverter() : base(Visibility.Collapsed, Visibility.Visible) { }
    }

}
