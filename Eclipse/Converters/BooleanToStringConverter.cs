namespace Eclipse.Converters
{
    public sealed class BooleanToStringConverter : BooleanConverter<string>
    {
        public BooleanToStringConverter() : base(string.Empty, string.Empty) { }
    }

}
