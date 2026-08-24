using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Eclipse.Helpers;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// Builds the shape MainWindowView declares - the 19 row grid, the media grid's two row
    /// spans, Grid_Selected_Game_Heading's star rows and GameListGrid's split - lays it out at a
    /// given size, and reports the measurements. The row heights fed in come from LayoutGeometry,
    /// the same way ApplyStageGeometry gets them, so what is under test is the geometry contract
    /// rather than the wiring.
    ///
    /// These exist because the first attempt at this used MaxHeight plus VerticalAlignment="Top"
    /// to hold the media stage at 16:9, and that silently collapses a grid to its content's
    /// desired size instead of filling its row span - a grid whose images have no source yet
    /// measures zero. The surplus row approach needs neither, and these assert that the stage
    /// really does fill its span on both displays.
    ///
    /// WPF elements must be built on an STA thread, so the layout runs on one of its own and
    /// hands back plain numbers for the assertions.
    /// </summary>
    public class StageLayoutTests
    {
        /// <summary>What the laid-out stage measured, as numbers - no live WPF objects escape.</summary>
        private sealed class Measured
        {
            public double MediaWidth;
            public double MediaHeight;
            public double MediaX;
            public double MediaY;

            public double FeatureMediaWidth;
            public double FeatureMediaHeight;
            public double FeatureMediaY;

            public double HeadingHeight;
            public double HeadingY;
            public double LogoSlotHeight;
            public double LogoSlotY;

            public double GameListHeight;
            public double GameListY;
            public double CurrentListHeight;
            public double CurrentListY;
            public double TeaserHeight;
            public double TeaserBottom;

            public double OverlayHeight;
            public double OptionsIconY;

            public double[] RowHeights;
        }

        private static Measured Layout(double width, double height)
        {
            Measured measured = null;
            Exception failure = null;

            Thread staThread = new Thread(() =>
            {
                try
                {
                    measured = LayoutCore(width, height);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            if (failure != null)
            {
                throw new InvalidOperationException("laying out the stage failed", failure);
            }

            return measured;
        }

        private static Measured LayoutCore(double width, double height)
        {
            double unit = LayoutGeometry.Unit(width, height);

            Grid root = new Grid { Width = width, Height = height };

            // Rows 0..17 are the design stage and are held to the square unit; row 18 is star
            // sized and collects whatever a taller display has left over.
            for (int row = 0; row < LayoutGeometry.TotalRows; row++)
            {
                root.RowDefinitions.Add(new RowDefinition
                {
                    Height = row < LayoutGeometry.DesignRows
                        ? new GridLength(unit)
                        : new GridLength(1, GridUnitType.Star)
                });
            }

            for (int column = 0; column < LayoutGeometry.DesignColumns; column++)
            {
                root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Grid_Selected_Game_Background / _Video in normal mode: rows 0-8, columns 16-31.
            // Deliberately left with default alignment and no children, which is the case that
            // broke the earlier MaxHeight attempt.
            Grid media = AddChild(root, new Grid(), row: 0, rowSpan: 9, column: 16, columnSpan: 16);

            // the same grids in feature mode: the whole design stage, not the whole display
            Grid featureMedia = AddChild(root, new Grid(), row: 0, rowSpan: 18, column: 0, columnSpan: 32);

            // a full screen overlay - the zoom and voice scrims - which does reach the surplus row
            Grid overlay = AddChild(root, new Grid(), row: 0, rowSpan: 19, column: 0, columnSpan: 32);

            // StackPanel_OptionsIcon sits in row 10, column 0
            Grid optionsIcon = AddChild(root, new Grid(), row: 10, rowSpan: 1, column: 0, columnSpan: 1);

            // Grid_Selected_Game_Heading: rows 1-16, columns 1-16, star rows 5,1,3,1,1,1,1,1,1
            Grid heading = new Grid();
            double[] headingRowWeights = { 5, 1, 3, 1, 1, 1, 1, 1, 1 };
            foreach (double weight in headingRowWeights)
            {
                heading.RowDefinitions.Add(new RowDefinition { Height = new GridLength(weight, GridUnitType.Star) });
            }

            Grid logoSlot = new Grid();
            Grid.SetRow(logoSlot, 0);
            heading.Children.Add(logoSlot);

            AddChild(root, heading, row: 1, rowSpan: 16, column: 1, columnSpan: 16);

            // GameListGrid: row 8 to the bottom of the grid, current list band pinned, teaser
            // takes the remainder
            Grid gameList = new Grid();
            gameList.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(LayoutGeometry.CurrentListBandHeight(unit))
            });
            gameList.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid currentList = new Grid();
            Grid.SetRow(currentList, 0);
            gameList.Children.Add(currentList);

            Grid teaser = new Grid();
            Grid.SetRow(teaser, 1);
            gameList.Children.Add(teaser);

            AddChild(root, gameList, row: LayoutGeometry.GameListStartRow, rowSpan: 11, column: 0, columnSpan: 32);

            root.Measure(new Size(width, height));
            root.Arrange(new Rect(0, 0, width, height));
            root.UpdateLayout();

            double[] rowHeights = new double[root.RowDefinitions.Count];
            for (int row = 0; row < rowHeights.Length; row++)
            {
                rowHeights[row] = root.RowDefinitions[row].ActualHeight;
            }

            return new Measured
            {
                MediaWidth = media.ActualWidth,
                MediaHeight = media.ActualHeight,
                MediaX = XOf(media, root),
                MediaY = YOf(media, root),

                FeatureMediaWidth = featureMedia.ActualWidth,
                FeatureMediaHeight = featureMedia.ActualHeight,
                FeatureMediaY = YOf(featureMedia, root),

                HeadingHeight = heading.ActualHeight,
                HeadingY = YOf(heading, root),
                LogoSlotHeight = logoSlot.ActualHeight,
                LogoSlotY = YOf(logoSlot, root),

                GameListHeight = gameList.ActualHeight,
                GameListY = YOf(gameList, root),
                CurrentListHeight = currentList.ActualHeight,
                CurrentListY = YOf(currentList, root),
                TeaserHeight = teaser.ActualHeight,
                TeaserBottom = YOf(teaser, root) + teaser.ActualHeight,

                OverlayHeight = overlay.ActualHeight,
                OptionsIconY = YOf(optionsIcon, root),

                RowHeights = rowHeights
            };
        }

        private static Grid AddChild(Grid root, Grid child, int row, int rowSpan, int column, int columnSpan)
        {
            Grid.SetRow(child, row);
            Grid.SetRowSpan(child, rowSpan);
            Grid.SetColumn(child, column);
            Grid.SetColumnSpan(child, columnSpan);
            root.Children.Add(child);

            return child;
        }

        private static double YOf(FrameworkElement element, Visual root)
        {
            return element.TransformToAncestor(root).Transform(new Point(0, 0)).Y;
        }

        private static double XOf(FrameworkElement element, Visual root)
        {
            return element.TransformToAncestor(root).Transform(new Point(0, 0)).X;
        }

        // The media cell must be 960 x 540 on both displays. This is the reason for the whole
        // design: the bezel is a 16:9 image fitted into this cell, and if the cell stops being
        // 16:9 the bezel is letterboxed inside it while the video is not.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1920, 1200)]
        public void MediaCellStaysSixteenByNine(double width, double height)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(960, measured.MediaWidth, 3);
            Assert.Equal(540, measured.MediaHeight, 3);
            Assert.Equal(960, measured.MediaX, 3);
            Assert.Equal(0, measured.MediaY, 3);
        }

        // In feature mode the media grid spans the whole design stage. Eighteen rows used to be
        // the whole display; now it is 18u, so the stage stays 16:9 on a taller display and the
        // surplus row below it is left to the game list.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1920, 1200)]
        public void FeatureMediaStageStaysSixteenByNine(double width, double height)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(1920, measured.FeatureMediaWidth, 3);
            Assert.Equal(1080, measured.FeatureMediaHeight, 3);
            Assert.Equal(0, measured.FeatureMediaY, 3);
        }

        // A full screen scrim spans the surplus row too, so it still covers the display.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1920, 1200)]
        [InlineData(2560, 1600)]
        public void FullScreenOverlaysStillCoverTheDisplay(double width, double height)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(height, measured.OverlayHeight, 3);
        }

        // The clear logo slot is the heading grid's first star row. The grid spans rows 1-16,
        // which are all design stage rows, so the logo's baseline does not move.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1920, 1200)]
        public void ClearLogoSlotIsUnmovedAndUnresized(double width, double height)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(960, measured.HeadingHeight, 3);
            Assert.Equal(60, measured.HeadingY, 3);

            // 5 of 15 star units over 960, sitting one row down
            Assert.Equal(320, measured.LogoSlotHeight, 3);
            Assert.Equal(60, measured.LogoSlotY, 3);
            Assert.Equal(380, measured.LogoSlotY + measured.LogoSlotHeight, 3);
        }

        // The box row must not grow, so the current list band is pinned and starts in the same
        // place on both displays.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1920, 1200)]
        public void CurrentListBandIsUnmovedAndUnresized(double width, double height)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(480, measured.GameListY, 3);
            Assert.Equal(400, measured.CurrentListHeight, 3);
            Assert.Equal(480, measured.CurrentListY, 3);
        }

        // ...and the surplus arrives underneath it, as more of the next list.
        [Theory]
        [InlineData(1920, 1080, 600, 200)]
        [InlineData(1920, 1200, 720, 320)]
        [InlineData(2560, 1440, 800, 266.6667)]
        [InlineData(2560, 1600, 960, 426.6667)]
        [InlineData(1680, 1050, 630, 280)]
        public void SurplusHeightLandsInTheNextListTeaser(
            double width, double height, double expectedListHeight, double expectedTeaser)
        {
            Measured measured = Layout(width, height);

            Assert.Equal(expectedListHeight, measured.GameListHeight, 3);
            Assert.Equal(expectedTeaser, measured.TeaserHeight, 3);
            Assert.Equal(height, measured.TeaserBottom, 3);
        }

        // Nothing above the game list may move between the two displays. The options icon is in
        // row 10, which is a design stage row, so it does not drift either.
        [Fact]
        public void TopStageIsIdenticalBetweenSixteenNineAndSixteenTen()
        {
            Measured nine = Layout(1920, 1080);
            Measured ten = Layout(1920, 1200);

            Assert.Equal(nine.MediaHeight, ten.MediaHeight, 6);
            Assert.Equal(nine.MediaWidth, ten.MediaWidth, 6);
            Assert.Equal(nine.MediaX, ten.MediaX, 6);
            Assert.Equal(nine.MediaY, ten.MediaY, 6);

            Assert.Equal(nine.FeatureMediaHeight, ten.FeatureMediaHeight, 6);
            Assert.Equal(nine.FeatureMediaY, ten.FeatureMediaY, 6);

            Assert.Equal(nine.HeadingHeight, ten.HeadingHeight, 6);
            Assert.Equal(nine.HeadingY, ten.HeadingY, 6);

            Assert.Equal(nine.LogoSlotHeight, ten.LogoSlotHeight, 6);
            Assert.Equal(nine.LogoSlotY, ten.LogoSlotY, 6);

            Assert.Equal(nine.CurrentListHeight, ten.CurrentListHeight, 6);
            Assert.Equal(nine.CurrentListY, ten.CurrentListY, 6);
            Assert.Equal(nine.GameListY, ten.GameListY, 6);

            Assert.Equal(nine.OptionsIconY, ten.OptionsIconY, 6);
        }

        // A 16:9 display must lay out exactly as it did before any of this: eighteen uniform
        // rows, an empty surplus row, and a feature stage the size of the display.
        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(2560, 1440)]
        [InlineData(3840, 2160)]
        [InlineData(1280, 720)]
        public void SixteenByNineIsUnchanged(double width, double height)
        {
            AssertDesignStageFillsTheDisplay(width, height);
        }

        // Wider than 16:9: the unit clamps to the height term, so this is the same story - the
        // stage is the display and the surplus row is empty.
        [Theory]
        [InlineData(2560, 1080)]
        [InlineData(3440, 1440)]
        public void DisplaysWiderThanSixteenByNineAreUnaffected(double width, double height)
        {
            AssertDesignStageFillsTheDisplay(width, height);
        }

        private static void AssertDesignStageFillsTheDisplay(double width, double height)
        {
            Measured measured = Layout(width, height);
            double expected = height / LayoutGeometry.DesignRows;

            for (int row = 0; row < LayoutGeometry.DesignRows; row++)
            {
                Assert.Equal(expected, measured.RowHeights[row], 6);
            }

            Assert.Equal(0, measured.RowHeights[LayoutGeometry.SurplusRow], 6);
            Assert.Equal(height, measured.FeatureMediaHeight, 6);
            Assert.Equal(height * 10.0 / 18.0, measured.GameListHeight, 6);
        }
    }
}
