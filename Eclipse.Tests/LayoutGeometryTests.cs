using Eclipse.Helpers;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// The 16:9 experience is the regression baseline, so the claim that matters is not that
    /// the new geometry is reasonable - it is that on a 16:9 display it is arithmetically the
    /// same as what it replaced. These assert that, at every resolution Eclipse is likely to
    /// meet, against the formulas the layout used before the design unit existed.
    /// </summary>
    public class LayoutGeometryTests
    {
        // What ImageScaler.GetDesiredHeight() computed before this change: the box row is
        // 100/324 of the display height, less the box's two pixel margin top and bottom.
        private static int LegacyBoxArtCacheHeight(int displayHeight) => (displayHeight * 100 / 324) - 4;

        [Theory]
        [InlineData(1280, 720)]
        [InlineData(1366, 768)]
        [InlineData(1600, 900)]
        [InlineData(1920, 1080)]
        [InlineData(2560, 1440)]
        [InlineData(3840, 2160)]
        public void BoxArtCacheHeightIsUnchangedOnSixteenByNine(int width, int height)
        {
            Assert.Equal(LegacyBoxArtCacheHeight(height), LayoutGeometry.BoxArtCacheHeight(width, height));
        }

        // Wider than 16:9 clamps to the height term, which is exactly what the layout did
        // before, so an ultrawide display is left alone too.
        [Theory]
        [InlineData(2560, 1080)]
        [InlineData(3440, 1440)]
        public void BoxArtCacheHeightIsUnchangedOnDisplaysWiderThanSixteenByNine(int width, int height)
        {
            Assert.Equal(LegacyBoxArtCacheHeight(height), LayoutGeometry.BoxArtCacheHeight(width, height));
        }

        // The point of the change: a 16:10 display pre-scales box art to the same size as the
        // 16:9 display of the same width, so both machines render identical boxes and neither
        // cache has to be rebuilt when moving between them.
        [Theory]
        [InlineData(1920, 1080, 1920, 1200)]
        [InlineData(2560, 1440, 2560, 1600)]
        [InlineData(1680, 945, 1680, 1050)]
        public void BoxArtCacheHeightMatchesTheSixteenByNineDisplayOfTheSameWidth(
            int sixteenNineWidth, int sixteenNineHeight, int tallWidth, int tallHeight)
        {
            Assert.Equal(
                LayoutGeometry.BoxArtCacheHeight(sixteenNineWidth, sixteenNineHeight),
                LayoutGeometry.BoxArtCacheHeight(tallWidth, tallHeight));
        }

        [Theory]
        [InlineData(1920, 1080, 60.0)]
        [InlineData(1920, 1200, 60.0)]
        [InlineData(2560, 1440, 80.0)]
        [InlineData(2560, 1600, 80.0)]
        [InlineData(1680, 1050, 52.5)]
        [InlineData(3840, 2160, 120.0)]
        [InlineData(2560, 1080, 60.0)]
        public void UnitIsTheSquareDesignCell(int width, int height, double expected)
        {
            Assert.Equal(expected, LayoutGeometry.Unit(width, height), 6);
        }

        // On 16:9 the unit is both width/32 and height/18, which is what makes every value
        // derived from it identical to the star sizing it replaced.
        [Theory]
        [InlineData(1280, 720)]
        [InlineData(1920, 1080)]
        [InlineData(2560, 1440)]
        [InlineData(3840, 2160)]
        public void UnitIsWidthAndHeightDerivedAtSixteenByNine(int width, int height)
        {
            double unit = LayoutGeometry.Unit(width, height);

            Assert.Equal(width / 32.0, unit, 6);
            Assert.Equal(height / 18.0, unit, 6);
        }

        // The stage is the whole design grid, so at 16:9 it is the display itself and the
        // MaxHeight it drives can never clamp anything.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(2560, 1440)]
        [InlineData(3840, 2160)]
        public void StageHeightIsTheWholeDisplayAtSixteenByNine(int width, int height)
        {
            Assert.Equal(height, LayoutGeometry.StageHeight(LayoutGeometry.Unit(width, height)), 6);
        }

        [Theory]
        [InlineData(1920, 1200, 1080.0)]
        [InlineData(2560, 1600, 1440.0)]
        [InlineData(1680, 1050, 945.0)]
        public void StageHeightStaysSixteenByNineOnATallerDisplay(int width, int height, double expected)
        {
            Assert.Equal(expected, LayoutGeometry.StageHeight(LayoutGeometry.Unit(width, height)), 6);
        }

        // GameListGrid spans rows 8..17 and splits 2*/1*, so before the change the current list
        // band was two thirds of ten eighteenths of the display height.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(2560, 1440)]
        [InlineData(3840, 2160)]
        public void CurrentListBandIsUnchangedOnSixteenByNine(int width, int height)
        {
            double legacy = height * (10.0 / 18.0) * (2.0 / 3.0);

            Assert.Equal(legacy, LayoutGeometry.CurrentListBandHeight(LayoutGeometry.Unit(width, height)), 6);
        }

        // CurrentListGrid splits 1*/5* between its heading and the row of images, so the box row
        // is five sixths of the band above it.
        [Theory]
        [InlineData(1920, 1080, 333.3333)]
        [InlineData(1920, 1200, 333.3333)]
        [InlineData(2560, 1440, 444.4444)]
        [InlineData(2560, 1600, 444.4444)]
        public void BoxArtHeightIsFiveSixthsOfTheBand(int width, int height, double expected)
        {
            double unit = LayoutGeometry.Unit(width, height);

            Assert.Equal(expected, LayoutGeometry.BoxArtHeight(unit), 3);
            Assert.Equal(LayoutGeometry.CurrentListBandHeight(unit) * 5.0 / 6.0, LayoutGeometry.BoxArtHeight(unit), 6);
        }

        // The cached artwork is four pixels shorter than the row, which is the two pixel margin
        // an unselected box carries on each side. Truncated, because a cached image is whole
        // pixels.
        [Theory]
        [InlineData(1920, 1080, 329)]
        [InlineData(1920, 1200, 329)]
        [InlineData(2560, 1440, 440)]
        [InlineData(2560, 1600, 440)]
        [InlineData(1680, 1050, 287)]
        [InlineData(3840, 2160, 662)]
        public void BoxArtCacheHeightIsTheRenderedRowLessTheMargin(int width, int height, int expected)
        {
            Assert.Equal(expected, LayoutGeometry.BoxArtCacheHeight(width, height));
        }

        // A surplus only ever appears on a display taller than 16:9, and all of it belongs to
        // the rows below the top stage.
        [Theory]
        [InlineData(1920, 1080, 0.0)]
        [InlineData(1920, 1200, 120.0)]
        [InlineData(2560, 1440, 0.0)]
        [InlineData(2560, 1600, 160.0)]
        [InlineData(2560, 1080, 0.0)]
        public void SurplusHeightGoesToTheGameList(int width, int height, double expected)
        {
            double surplus = height - LayoutGeometry.StageHeight(LayoutGeometry.Unit(width, height));

            Assert.Equal(expected, surplus, 6);
        }

        [Theory]
        [InlineData(0, 1080)]
        [InlineData(1920, 0)]
        [InlineData(-1, -1)]
        public void UnitIsZeroBeforeThereIsASize(int width, int height)
        {
            Assert.Equal(0, LayoutGeometry.Unit(width, height));
            Assert.Equal(0, LayoutGeometry.BoxArtCacheHeight(width, height));
        }
    }
}
