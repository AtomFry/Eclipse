using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// pre-scale images so that we dont incur the cost of scaling while moving around in the front end
// see formula below for logic 
// only scaling the game front images - background and logo images do not incur such a performance 
// hit because they load up far less frequently
namespace Eclipse.Service
{
    public sealed class ImageScaler
    {
        private static readonly ImageScaler instance = new ImageScaler();

        static ImageScaler()
        {
        }

        private ImageScaler()
        {
        }

        public static ImageScaler Instance
        {
            get
            {
                return instance;
            }
        }

        private int desiredFrontImageHeight = 0;
        public int DesiredFrontImageHeight
        {
            get
            {
                if(desiredFrontImageHeight == 0)
                {
                    desiredFrontImageHeight = GetDesiredHeight();
                }
                return desiredFrontImageHeight;
            }
        }
 
        public static int GetDesiredHeight()
        {
            /*
             * Pre-scaled box height, for the displays this is likely to meet.
             *
             *   Display       Aspect   Unit u   Box row (100u/18)   Cached height (-4)
             *   1366 x  768    16:9    42.667        237.04                233
             *   1920 x 1080    16:9    60.000        333.33                329
             *   1920 x 1200    16:10   60.000        333.33                329
             *   2560 x 1440    16:9    80.000        444.44                440
             *   2560 x 1600    16:10   80.000        444.44                440
             *   3840 x 2160    16:9   120.000        666.67                662
             *
             * The two 16:10 rows share their cache size with the 16:9 display of the same width,
             * which is the point: the box row is held to its 16:9 height on a taller display, so
             * the artwork is the same size on both and neither has to be regenerated.
             */
            // The box row is five sixths of the current list band, which is two thirds of the
            // ten rows the game list occupies: (10/18) * (2/3) * (5/6) = 100/324 of the design
            // stage. The -4 is the box's two pixel margin, top and bottom.
            //
            // This used to read the monitor height directly, which is the same number only while
            // the display is 16:9. LayoutGeometry derives it from the square design unit the
            // layout is actually built on, so the height artwork is pre-scaled to and the height
            // it is rendered at come from one expression and cannot drift apart. On any 16:9
            // display the two agree exactly, at every resolution - asserted in LayoutGeometryTests.
            return LayoutGeometry.BoxArtCacheHeight(GetMonitorWidth(), GetMonitorHeight());
        }

        public static List<FileInfo> GetMissingPlatformClearLogoFiles()
        {
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(DirectoryInfoHelper.Instance.LaunchboxImagesPlatformsPath);
            string[] imageFolders = GetClearLogoFolders();
            List<string> foldersToProcess = new List<string>();
            List<FileInfo> filesToProcess = new List<FileInfo>();

            foreach(string platformImageDirectory in platformImageDirectories)
            {
                foreach(string imageFolder in imageFolders)
                {
                    string path = Path.Combine(platformImageDirectory, imageFolder);
                    if(Directory.Exists(path))
                    {
                        foldersToProcess.Add(path);
                    }
                }
            }

            foreach(string folder in foldersToProcess)
            {
                filesToProcess.AddRange(GetMissingFilesInFolder(folder));
            }

            return filesToProcess;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            // The bitmap exists before anything is drawn into it, so a failure part-way has to
            // release it - the caller never receives it and has nothing to dispose. This was the
            // residual left behind by B-06, whose using blocks covered every path but this one.
            try
            {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                return destImage;
            }
            catch
            {
                destImage.Dispose();
                throw;
            }
        }

        public static IEnumerable<FileInfo> GetMissingFilesInFolder(string directory)
        {
            string pathA = directory;
            string pathB = directory.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);

            if(!Directory.Exists(pathB))
            {
                Directory.CreateDirectory(pathB);
            }

            DirectoryInfo dir1 = new DirectoryInfo(pathA);
            DirectoryInfo dir2 = new DirectoryInfo(pathB);

            // Take a snapshot of the file system.  
            IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            IEnumerable<FileInfo> list2 = dir2.GetFiles("*.*", SearchOption.TopDirectoryOnly);

            FileCompare fileCompare = new FileCompare();

            // Find the files in the LB folder that are not in the plugin folder
            return (from file in list1 select file).Except(list2, fileCompare);
        }

        public static void CropImage(FileInfo fileInfo)
        {
            try
            {
                string file = fileInfo.FullName;
                int originalHeight, originalWidth;

                using (Image originalImage = Image.FromFile(file))
                {
                    originalHeight = originalImage.Height;
                    originalWidth = originalImage.Width;

                    using (Bitmap newBitmap = ResizeImage(originalImage, originalWidth, originalHeight))
                    {
                        string newFileName = file.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
                        string newFolder = Path.GetDirectoryName(newFileName);

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }

                        using (Bitmap croppedBitmap = Crop(newBitmap))
                        {
                            croppedBitmap.Save(newFileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "CropImage");
            }
        }

        public static void CropImage(string sourceFile, string destinationFile)
        {
            try
            {
                int originalHeight, originalWidth;

                using (Image originalImage = Image.FromFile(sourceFile))
                {
                    originalHeight = originalImage.Height;
                    originalWidth = originalImage.Width;

                    using (Bitmap newBitmap = ResizeImage(originalImage, originalWidth, originalHeight))
                    {
                        string newFolder = Path.GetDirectoryName(destinationFile);

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }

                        using (Bitmap croppedBitmap = Crop(newBitmap))
                        {
                            croppedBitmap.Save(destinationFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, $"CropImage: {sourceFile} | {destinationFile}");
            }
        }

        public static bool DefaultBoxFrontExists()
        {
            bool ret = false;

            if (File.Exists(DirectoryInfoHelper.Instance.DefaultBoxFrontImageFullPath))
            {
                ret = true;
            }            

            return ret;
        }

        public static void ScaleDefaultBoxFront(int desiredHeight)
        {
            try
            {                
                int originalHeight, originalWidth, desiredWidth;
                double scale;               

                var originalBitmapImage = new System.Windows.Media.Imaging.BitmapImage(Models.ResourceImages.GameFrontDummy);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(originalBitmapImage));
                // The stream must stay open for the whole lifetime of the Bitmap built from it -
                // GDI+ reads from the stream lazily - so this using encloses the Bitmap's scope
                // rather than being disposed straight after the encoder writes to it.
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Flush();

                    using (Image originalImage = new Bitmap(stream))
                    {
                        originalHeight = originalImage.Height;
                        originalWidth = originalImage.Width;

                        scale = (double)((double)desiredHeight / (double)originalHeight);

                        desiredWidth = (int)(originalWidth * scale);

                        using (Bitmap newBitmap = ResizeImage(originalImage, desiredWidth, desiredHeight))
                        {
                            string newFileName = DirectoryInfoHelper.Instance.DefaultBoxFrontImageFileName;
                            string newFolder = DirectoryInfoHelper.Instance.DefaultBoxFrontImageFilePath;

                            if (!Directory.Exists(newFolder))
                            {
                                Directory.CreateDirectory(newFolder);
                            }

                            newBitmap.Save(DirectoryInfoHelper.Instance.DefaultBoxFrontImageFullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "ScaleDefaultBoxFront");
            }
        }

        public static void ScaleImage(string sourceFile, string destinationFile)
        {
            int desiredHeight = Instance.DesiredFrontImageHeight;

            try
            {
                int originalHeight, originalWidth, desiredWidth;
                double scale;

                using (Image originalImage = Image.FromFile(sourceFile))
                {
                    originalHeight = originalImage.Height;
                    originalWidth = originalImage.Width;

                    scale = (double)((double)desiredHeight / (double)originalHeight);
                    desiredWidth = (int)(originalWidth * scale);

                    using (Bitmap newBitmap = ResizeImage(originalImage, desiredWidth, desiredHeight))
                    {
                        string newFolder = Path.GetDirectoryName(destinationFile);

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }
                        newBitmap.Save(destinationFile);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "ScaleImage");
            }
        }

        // The display Eclipse is running on. DisplayInfoHelper reads it once and is also what
        // names the resolution specific cache folder, so the artwork's size and the folder it is
        // filed under can no longer come from two different monitors.
        public static int GetMonitorHeight()
        {
            return DisplayInfoHelper.Instance.displayHeight;
        }

        public static int GetMonitorWidth()
        {
            return DisplayInfoHelper.Instance.displayWidth;
        }

        public static string[] GetClearLogoFolders()
        {
            return new string[] { DirectoryInfoHelper.Instance.ClearLogoFolder };
        }

        /// <summary>
        /// The image trimmed to the bounds of its visible pixels - the transparent border a
        /// clear logo is usually shipped with, removed before it is cached.
        ///
        /// A row or column counts as border only while it is *entirely* transparent, and only
        /// from the outside in: the scan stops at the first row that holds anything, so a fully
        /// transparent row inside the picture is part of the picture and is kept.
        ///
        /// This used to keep one transparent row at the top and one column at the left. The
        /// scans recorded the last *empty* row rather than the first *occupied* one, while the
        /// bottom and right scans recorded an exclusive bound and were correct - so every logo
        /// with a border came out one pixel off centre, on two sides only. Cropped output
        /// therefore changed when this was fixed; see the note in
        /// docs/plans/media-and-presentation-refactor.md A4.
        /// </summary>
        public static Bitmap Crop(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, bool> rowIsEmpty = row =>
            {
                for (int i = 0; i < w; ++i)
                {
                    if (bmp.GetPixel(i, row).A != 0)
                    {
                        return false;
                    }
                }
                return true;
            };

            Func<int, bool> columnIsEmpty = col =>
            {
                for (int i = 0; i < h; ++i)
                {
                    if (bmp.GetPixel(col, i).A != 0)
                    {
                        return false;
                    }
                }
                return true;
            };

            // the first row holding anything
            int topmost = 0;
            while ((topmost < h) && rowIsEmpty(topmost))
            {
                topmost++;
            }

            if (topmost == h)
            {
                // Nothing visible anywhere, so there are no bounds to crop to and the whole
                // image is kept. The old code produced a 1x1 sliver here, which was not a
                // decision - it was the same off-by-one arriving at a degenerate input.
                return CopyRegion(bmp, 0, 0, w, h);
            }

            // one past the last row holding anything
            int bottommost = h;
            while ((bottommost > topmost) && rowIsEmpty(bottommost - 1))
            {
                bottommost--;
            }

            int leftmost = 0;
            while ((leftmost < w) && columnIsEmpty(leftmost))
            {
                leftmost++;
            }

            int rightmost = w;
            while ((rightmost > leftmost) && columnIsEmpty(rightmost - 1))
            {
                rightmost--;
            }

            return CopyRegion(bmp, leftmost, topmost, rightmost - leftmost, bottommost - topmost);
        }

        /// <summary>
        /// A new bitmap holding one rectangle of another.
        ///
        /// The target is created before it is drawn into, so a failure part-way has to release
        /// it - the caller never receives it and has nothing to dispose. That was the residual
        /// left behind by B-06.
        /// </summary>
        private static Bitmap CopyRegion(Bitmap source, int x, int y, int width, int height)
        {
            Bitmap target = new Bitmap(width, height);

            try
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(source,
                      new RectangleF(0, 0, width, height),
                      new RectangleF(x, y, width, height),
                      GraphicsUnit.Pixel);
                }

                return target;
            }
            catch (Exception ex)
            {
                target.Dispose();

                throw new Exception(
                  string.Format("Values are x={0} y={1} width={2} height={3} source={4}x{5}", x, y, width, height, source.Width, source.Height),
                  ex);
            }
        }
    }

    // This implementation defines a very simple comparison  
    // between two FileInfo objects. It only compares the name  
    // of the files being compared  
    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            return (f1.Name == f2.Name);
        }

        // Return a hash that reflects the comparison criteria. According to the
        // rules for IEqualityComparer<T>, if Equals is true, then the hash codes must  
        // also be equal. Because equality as defined here is a simple value equality, not  
        // reference identity, it is possible that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(FileInfo fi)
        {
            string s = $"{fi.Name}";
            return s.GetHashCode();
        }
    }
}
