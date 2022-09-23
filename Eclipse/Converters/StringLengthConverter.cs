using System;
using System.Globalization;
using System.Windows.Data;

namespace Eclipse.Converters
{
    public class StringLengthConverter<T> : IValueConverter
    {
        public T Empty { get; set; }
        public T NonEmpty { get; set; }

        public StringLengthConverter(T EmptyValue, T NonEmptyValue)
        {
            Empty = EmptyValue;
            NonEmpty = NonEmpty;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = value as string;
            if(string.IsNullOrWhiteSpace(param))
            {
                return Empty;
            }
            else
            {
                return NonEmpty;
            }
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
