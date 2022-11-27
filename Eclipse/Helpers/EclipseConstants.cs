using System;

namespace Eclipse.Helpers
{
    public static class EclipseConstants
    {
        // todo: rework this
        // this is hacky as shit...the integers define the row/column span to use for background image/videos, dependent on whether we are in featured game or regular results mode
        public static int BackgroundRowSpanFeature = 18;
        public static int BackgroundRowSpanNormal = 9;
        public static int BackgroundColumnStartFeature = 0;
        public static int BackgroundColumnStartNormal = 16;
        public static int BackgroundColumnSpanFeature = 32;
        public static int BackgroundColumnSpanNormal = 16;

        public static int GamesToPage = 7;
    }
}
