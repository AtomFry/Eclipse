using System;

namespace Eclipse.Helpers
{
    /// <summary>
    /// The one place Eclipse reasons about display aspect ratio.
    ///
    /// The theme is composed on a 32 x 18 grid. At 16:9 that grid's cells are square - 60 x 60
    /// at 1080p, 80 x 80 at 1440p - and the whole composition is built on that squareness. The
    /// design unit below is that cell, and every dimension here is a multiple of it.
    ///
    ///     u = min(width / 32, height / 18)
    ///
    /// On a 16:9 display width/32 and height/18 are the same number, so every function here
    /// returns exactly what the star sizing it replaces returned. That identity is what makes
    /// this change a no-op on the arcade cabinet at any resolution, and it is asserted in
    /// LayoutGeometryTests rather than left to inspection.
    ///
    /// On a taller display - 16:10 - the width term wins. The 18 rows of the design stage keep
    /// their 16:9 height, so the background artwork, the video and its bezel, the clear logo and
    /// the game details are all the same size and in the same place as they are on the cabinet.
    /// The surplus height goes to a nineteenth row that only the game list and the full screen
    /// overlays reach into, where it shows more of the next list at the same box size.
    ///
    /// On a wider display - 21:9 - the height term wins, the surplus row is empty and the
    /// layout behaves exactly as it does today.
    ///
    /// Deliberately free of WPF types. The same functions size the rendered layout and the
    /// pre-scaled box art cache, so those two cannot drift apart.
    /// </summary>
    public static class LayoutGeometry
    {
        /// <summary>Columns in the design grid.</summary>
        public const int DesignColumns = 32;

        /// <summary>
        /// Rows in the design stage - the 16:9 composition. These are held to the square unit,
        /// so everything placed in them keeps its 16:9 geometry on any display.
        /// </summary>
        public const int DesignRows = 18;

        /// <summary>
        /// Index of the row that absorbs a taller display's surplus height. It is star sized and
        /// measures zero at 16:9, so a row span that reaches it is unchanged there.
        /// </summary>
        public const int SurplusRow = DesignRows;

        /// <summary>Total rows declared in the markup: the design stage plus the surplus row.</summary>
        public const int TotalRows = DesignRows + 1;

        /// <summary>
        /// The row GameListGrid starts on. It deliberately overlaps the design stage's lower
        /// half by one row, which is what hides the background artwork's bottom edge behind the
        /// list heading. Preserved as-is.
        /// </summary>
        public const int GameListStartRow = 8;

        /// <summary>The square design unit, in whatever units width and height are given in.</summary>
        public static double Unit(double width, double height)
        {
            if (width <= 0 || height <= 0)
            {
                return 0;
            }

            return Math.Min(width / DesignColumns, height / DesignRows);
        }

        /// <summary>
        /// Height of the 16:9 design stage. Equal to the display height at 16:9 and below it on
        /// anything taller, and the difference is what the surplus row receives.
        /// </summary>
        public static double StageHeight(double unit)
        {
            return unit * DesignRows;
        }

        /// <summary>
        /// Height of the current list band - the list heading plus the row of box art.
        ///
        /// GameListGrid used to span rows 8..17 (10u at 16:9) and split 2*/1* between the
        /// current list and the next-list teaser, making the current list 10u x 2/3 = 20u/3.
        /// It now runs to the bottom of the grid instead, so the band is given that height
        /// outright and the teaser takes whatever is left. At 16:9 there is nothing left over
        /// and both come out exactly as they did before.
        ///
        /// Pinning the band is what stops box art growing on a taller display.
        /// </summary>
        public static double CurrentListBandHeight(double unit)
        {
            return unit * 20.0 / 3.0;
        }

        /// <summary>
        /// Height of the row of box art. CurrentListGrid splits 1*/5* between its heading and
        /// its images, so this is 5/6 of the band above: 20u/3 x 5/6 = 100u/18.
        ///
        /// This is the height box art is rendered at, and - less the margin - the height it is
        /// pre-scaled to on disk.
        /// </summary>
        public static double BoxArtHeight(double unit)
        {
            return unit * 100.0 / DesignRows;
        }

        /// <summary>
        /// Pixel height to pre-scale box art to, for a display of the given size.
        ///
        /// The four is the box's two pixel margin, top and bottom (FEAT-PRESENT-010). It matches
        /// the slot an unselected box is given; the selected box carries an additional two pixel
        /// border and is rendered four pixels shorter, which is pre-existing and unchanged here.
        /// </summary>
        public static int BoxArtCacheHeight(double displayWidth, double displayHeight)
        {
            double unit = Unit(displayWidth, displayHeight);
            if (unit <= 0)
            {
                return 0;
            }

            return (int)BoxArtHeight(unit) - 4;
        }
    }
}
