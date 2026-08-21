using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Eclipse.View
{
    // Five stars filled to a rating, drawn as vectors.
    //
    // Replaces two renderings of the same number: 104 pre-rasterised PNGs for the display, and
    // five gradient-filled polygons wired through 25 MultiBindings for the editor. One control
    // now serves both, and because it is bound rather than assigned in code-behind, changing a
    // rating no longer needs the view model to call back into the view.
    //
    // Layer two of these to show a user rating over a community rating: leave Empty as the
    // default transparent and the lower one shows through.
    public partial class StarRatingView : UserControl
    {
        public StarRatingView()
        {
            InitializeComponent();
            UpdateStars();
        }

        // Sampled from the star images this control replaced: unfilled stars were #FEFEFE at
        // alpha 77, which over the dark background reads as light grey. Both the community and
        // the user layer drew them, so the faint stars survive under whichever rating is lower.
        private static readonly SolidColorBrush UnfilledStar = CreateUnfilledStarBrush();

        private static SolidColorBrush CreateUnfilledStarBrush()
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(77, 254, 254, 254));
            brush.Freeze();
            return brush;
        }

        // The stars are drawn on a fixed 856x147 canvas, so the control has a fixed aspect
        // ratio - the same one the pre-rendered images had.
        private const double AspectRatio = 856.0 / 147.0;

        // Claim the width that matches the height being offered, the way an Image with
        // Stretch="Uniform" did. Inside a horizontal StackPanel the width constraint is
        // infinite, and without this the control reports a width measured against one height
        // and is then arranged at another - which scales the stars up past the width it was
        // given and clips the last one.
        protected override Size MeasureOverride(Size constraint)
        {
            base.MeasureOverride(constraint);

            if (!double.IsInfinity(constraint.Height) && constraint.Height > 0)
            {
                return new Size(constraint.Height * AspectRatio, constraint.Height);
            }

            return new Size(856, 147);
        }

        /// <summary>How many stars are filled. Fractional values fill part of a star.</summary>
        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            nameof(Rating),
            typeof(double),
            typeof(StarRatingView),
            new PropertyMetadata(0.0, OnStarPropertyChanged));

        public double Rating
        {
            get => (double)GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }

        /// <summary>Colour of the filled part of a star.</summary>
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(StarRatingView),
            new PropertyMetadata(Brushes.White, OnStarPropertyChanged));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        /// <summary>
        /// Colour of the unfilled part of a star. Every star is drawn, not just the rated ones -
        /// the unrated remainder sits behind at low opacity so it reads as a faint outline.
        /// </summary>
        public static readonly DependencyProperty EmptyProperty = DependencyProperty.Register(
            nameof(Empty),
            typeof(Brush),
            typeof(StarRatingView),
            new PropertyMetadata(UnfilledStar, OnStarPropertyChanged));

        public Brush Empty
        {
            get => (Brush)GetValue(EmptyProperty);
            set => SetValue(EmptyProperty, value);
        }

        private static void OnStarPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as StarRatingView)?.UpdateStars();
        }

        // Done here rather than through converters in markup. A converter would need the rating,
        // the star's position and both brushes, which means a four-way MultiBinding per star -
        // and passing the star number in was exactly what put Star1..Star5 and StarOffset00..10
        // on the view model as properties that existed only for XAML to read.
        private void UpdateStars()
        {
            Polygon[] stars = { Star1, Star2, Star3, Star4, Star5 };

            for (int index = 0; index < stars.Length; index++)
            {
                stars[index].Fill = BrushForStar(Rating - index);
            }
        }

        /// <param name="fillAmount">
        /// How much of this star is filled: at or below zero none of it, at or above one all of it.
        /// </param>
        private Brush BrushForStar(double fillAmount)
        {
            if (fillAmount <= 0)
            {
                return Empty;
            }

            if (fillAmount >= 1)
            {
                return Fill;
            }

            // Two stops at the same offset give a hard edge rather than a blend, so a half
            // rating reads as half a star rather than a gradient across it.
            LinearGradientBrush partial = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };

            partial.GradientStops.Add(new GradientStop(ColorOf(Fill), 0));
            partial.GradientStops.Add(new GradientStop(ColorOf(Fill), fillAmount));
            partial.GradientStops.Add(new GradientStop(ColorOf(Empty), fillAmount));
            partial.GradientStops.Add(new GradientStop(ColorOf(Empty), 1));

            partial.Freeze();
            return partial;
        }

        // A gradient needs colours, so only solid brushes can be split part way. Anything else
        // fills the whole star rather than rendering nothing.
        private static Color ColorOf(Brush brush)
        {
            return (brush as SolidColorBrush)?.Color ?? Colors.Transparent;
        }
    }
}
