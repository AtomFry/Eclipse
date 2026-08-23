using System.Windows;
using System.Windows.Controls;

namespace Eclipse.View.EclipseSettings
{
    /// <summary>
    /// One slider setting: a caption, its current value with a unit, and the track.
    ///
    /// Each of the eighteen sliders in the settings window used to be a caption `StackPanel` of
    /// two or three `Label`s followed by a `Slider`, placed by hand into two numbered grid rows -
    /// about ten lines and two indices each, all of them free to drift apart. They are four lines
    /// each now, and the shape is defined once in the template.
    ///
    /// <see cref="Step"/> is the value the slider snaps to as well as the keyboard increment.
    /// Nothing snapped before, which is how a video delay of 746 milliseconds - a number no one
    /// would choose - ended up in a settings file.
    /// </summary>
    public class SliderRow : Control
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(SliderRow));

        /// <summary>Shown after the value. Empty where the format already carries it, as "50%" does.</summary>
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(SliderRow),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(SliderRow),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(SliderRow),
                new PropertyMetadata(0d));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(SliderRow),
                new PropertyMetadata(100d));

        /// <summary>What the slider snaps to, and what an arrow key moves it by.</summary>
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(double), typeof(SliderRow),
                new PropertyMetadata(1d));

        /// <summary>How the value is written. "N0" for a whole number, "P0" for a percentage.</summary>
        public static readonly DependencyProperty ValueFormatProperty =
            DependencyProperty.Register(nameof(ValueFormat), typeof(string), typeof(SliderRow),
                new PropertyMetadata("N0"));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Step
        {
            get { return (double)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        public string ValueFormat
        {
            get { return (string)GetValue(ValueFormatProperty); }
            set { SetValue(ValueFormatProperty, value); }
        }
    }
}
