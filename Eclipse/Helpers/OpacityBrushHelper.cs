using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace Eclipse.Helpers
{
    public class OpacityBrushHelper
    {
        private LinearGradientBrush opacityBrush;
        public LinearGradientBrush OpacityBrush
        {
            get
            {
                if(opacityBrush == null)
                {
                    opacityBrush = GetOpacityBrush();

                }
                return opacityBrush;
            }
        }
     
        private LinearGradientBrush GetOpacityBrush()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(1, 0.5);
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.00));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0.20));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0.80));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 1.00));
            brush.Freeze();
            return brush;
        }

        #region singleton implementation 
        public static OpacityBrushHelper Instance { get; } = new OpacityBrushHelper();

        static OpacityBrushHelper()
        {
        }

        private OpacityBrushHelper()
        {
        }
        #endregion
    }
}
