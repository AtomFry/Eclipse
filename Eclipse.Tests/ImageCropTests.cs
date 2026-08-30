using Eclipse.Service;
using System.Drawing;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-MEDIA-010. Every expected rectangle here was worked out by hand from the fixture,
    /// not read off the implementation - a test that agrees with whatever the code does would
    /// have passed against the defect this stage fixes.
    ///
    /// The defect: the top and left scans recorded the last *empty* row or column rather than
    /// the first *occupied* one, while the bottom and right scans recorded an exclusive bound
    /// and were correct. Every logo with a transparent border came out one pixel too tall and
    /// one too wide, off centre on two sides only.
    ///
    /// Fixtures are built in code rather than checked in as files. Crop takes a Bitmap and
    /// returns a Bitmap, so nothing here touches the filesystem, LaunchBox or a display.
    /// </summary>
    public class ImageCropTests
    {
        /// <summary>
        /// A transparent bitmap with one opaque rectangle in it. A new 32bpp bitmap starts
        /// fully transparent, so only the visible part has to be filled in.
        /// </summary>
        private static Bitmap WithOpaqueRegion(int width, int height, Rectangle opaque)
        {
            Bitmap bitmap = new Bitmap(width, height);

            for (int x = opaque.Left; x < opaque.Right; x++)
            {
                for (int y = opaque.Top; y < opaque.Bottom; y++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(255, 10, 20, 30));
                }
            }

            return bitmap;
        }

        [Fact]
        public void A_transparent_border_is_trimmed_to_exactly_the_visible_pixels()
        {
            // visible pixels occupy columns 3..6 and rows 2..7, so the crop is 4 wide by 6 high
            using (Bitmap source = WithOpaqueRegion(10, 10, new Rectangle(3, 2, 4, 6)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                // the old code produced 5 x 7 here, keeping one transparent column and row
                Assert.Equal(4, cropped.Width);
                Assert.Equal(6, cropped.Height);

                // and the retained row and column meant the content started one pixel in
                Assert.Equal(255, cropped.GetPixel(0, 0).A);
                Assert.Equal(255, cropped.GetPixel(cropped.Width - 1, cropped.Height - 1).A);
            }
        }

        [Fact]
        public void An_image_with_no_transparent_border_is_unchanged()
        {
            using (Bitmap source = WithOpaqueRegion(8, 5, new Rectangle(0, 0, 8, 5)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(8, cropped.Width);
                Assert.Equal(5, cropped.Height);
            }
        }

        [Fact]
        public void A_border_on_the_top_only_trims_only_the_top()
        {
            // rows 0..2 empty, everything else visible across the full width
            using (Bitmap source = WithOpaqueRegion(6, 9, new Rectangle(0, 3, 6, 6)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(6, cropped.Width);
                Assert.Equal(6, cropped.Height);
            }
        }

        [Fact]
        public void A_border_on_the_left_only_trims_only_the_left()
        {
            using (Bitmap source = WithOpaqueRegion(9, 6, new Rectangle(3, 0, 6, 6)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(6, cropped.Width);
                Assert.Equal(6, cropped.Height);
            }
        }

        [Fact]
        public void A_border_on_the_bottom_and_right_only_is_trimmed_as_it_always_was()
        {
            // the two edges the old code got right - this pins them so the fix does not move them
            using (Bitmap source = WithOpaqueRegion(9, 9, new Rectangle(0, 0, 5, 4)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(5, cropped.Width);
                Assert.Equal(4, cropped.Height);
            }
        }

        [Fact]
        public void A_single_visible_pixel_crops_to_one_pixel()
        {
            using (Bitmap source = WithOpaqueRegion(7, 7, new Rectangle(4, 5, 1, 1)))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(1, cropped.Width);
                Assert.Equal(1, cropped.Height);
                Assert.Equal(255, cropped.GetPixel(0, 0).A);
            }
        }

        /// <summary>
        /// A row that is entirely transparent but has visible rows above and below it belongs to
        /// the picture. The scan stops at the first occupied row from each side, so an internal
        /// gap is never treated as border.
        /// </summary>
        [Fact]
        public void A_transparent_row_inside_the_picture_is_kept()
        {
            Bitmap source = new Bitmap(5, 7);
            try
            {
                for (int x = 0; x < 5; x++)
                {
                    source.SetPixel(x, 1, Color.FromArgb(255, 10, 20, 30));
                    source.SetPixel(x, 5, Color.FromArgb(255, 10, 20, 30));
                }

                using (Bitmap cropped = ImageScaler.Crop(source))
                {
                    // rows 1..5 inclusive, gap and all
                    Assert.Equal(5, cropped.Height);
                    Assert.Equal(5, cropped.Width);

                    // the gap survived
                    Assert.Equal(0, cropped.GetPixel(0, 2).A);
                }
            }
            finally
            {
                source.Dispose();
            }
        }

        /// <summary>
        /// Nothing visible anywhere means there are no bounds to crop to, so the image is kept
        /// whole. The old code returned a 1x1 sliver of the bottom-right corner - not a decision,
        /// just the same off-by-one meeting a degenerate input.
        /// </summary>
        [Fact]
        public void A_fully_transparent_image_is_kept_whole()
        {
            using (Bitmap source = new Bitmap(6, 4))
            using (Bitmap cropped = ImageScaler.Crop(source))
            {
                Assert.Equal(6, cropped.Width);
                Assert.Equal(4, cropped.Height);
            }
        }

        [Fact]
        public void A_single_pixel_image_survives_both_ways()
        {
            using (Bitmap opaque = WithOpaqueRegion(1, 1, new Rectangle(0, 0, 1, 1)))
            using (Bitmap croppedOpaque = ImageScaler.Crop(opaque))
            {
                Assert.Equal(1, croppedOpaque.Width);
                Assert.Equal(1, croppedOpaque.Height);
            }

            using (Bitmap empty = new Bitmap(1, 1))
            using (Bitmap croppedEmpty = ImageScaler.Crop(empty))
            {
                Assert.Equal(1, croppedEmpty.Width);
                Assert.Equal(1, croppedEmpty.Height);
            }
        }

        /// <summary>
        /// Partial alpha is visible. The scan tests for "not fully transparent", not for opaque,
        /// so an anti-aliased logo edge counts as picture rather than as border.
        /// </summary>
        [Fact]
        public void A_partly_transparent_pixel_counts_as_visible()
        {
            Bitmap source = new Bitmap(5, 5);
            try
            {
                source.SetPixel(2, 2, Color.FromArgb(1, 255, 255, 255));

                using (Bitmap cropped = ImageScaler.Crop(source))
                {
                    Assert.Equal(1, cropped.Width);
                    Assert.Equal(1, cropped.Height);
                }
            }
            finally
            {
                source.Dispose();
            }
        }
    }
}
