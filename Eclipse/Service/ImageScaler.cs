using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

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

        public static List<FileInfo> GetMissingGameFrontImageFiles()
        {
            // enumerate platform directories 
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(DirectoryInfoHelper.Instance.LaunchboxImagesPath);
            List<string> foldersToProcess = new List<string>();
            List<FileInfo> filesToProcess = new List<FileInfo>();
            string[] imageFolders = GetFrontImageFolders();

            // loop through platform folders
            foreach (string platformImageDirectory in platformImageDirectories)
            {
                // loop through box front image folders 
                foreach(string imageFolder in imageFolders)
                {
                    string path = Path.Combine(platformImageDirectory, imageFolder);
                    if(Directory.Exists(path))
                    {
                        IEnumerable<string> folders = Directory.EnumerateDirectories(path);
                        foreach (string folder in folders)
                        {
                            foldersToProcess.Add(folder);
                        }
                        foldersToProcess.Add(path);
                    }
                }
            }

            // get the list of files that are in the launchbox image folders but not in the plug-in image folders
            foreach(string folder in foldersToProcess)
            {
                filesToProcess.AddRange(GetMissingFilesInFolder(folder));
            }

            return filesToProcess;
        }

        public static List<FileInfo> GetMissingGameClearLogoFiles()
        {
            // enumerate platform directories 
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(DirectoryInfoHelper.Instance.LaunchboxImagesPath);
            List<string> foldersToProcess = new List<string>();
            List<FileInfo> filesToProcess = new List<FileInfo>();
            string[] imageFolders = GetClearLogoFolders();

            // loop through platform folders
            foreach (string platformImageDirectory in platformImageDirectories)
            {
                // loop through clear logo image folders 
                foreach (string imageFolder in imageFolders)
                {
                    string path = Path.Combine(platformImageDirectory, imageFolder);
                    if (Directory.Exists(path))
                    {
                        IEnumerable<string> folders = Directory.EnumerateDirectories(path);
                        foreach (string folder in folders)
                        {
                            foldersToProcess.Add(folder);
                        }
                        foldersToProcess.Add(path);
                    }
                }
            }

            // get the list of files that are in the launchbox image folders but not in the plug-in image folders
            foreach (string folder in foldersToProcess)
            {
                filesToProcess.AddRange(GetMissingFilesInFolder(folder));
            }

            return filesToProcess;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

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

        public static void ScaleImage(FileInfo fileInfo, int desiredHeight)
        {
            try
            {
                string file = fileInfo.FullName;
                int originalHeight, originalWidth, desiredWidth;
                double scale;

                using (Image originalImage = Image.FromFile(file))
                {
                    originalHeight = originalImage.Height;
                    originalWidth = originalImage.Width;
                    
                    scale = (double)((double)desiredHeight / (double)originalHeight);
                    desiredWidth = (int)(originalWidth * scale);

                    using (Bitmap newBitmap = ResizeImage(originalImage, desiredWidth, desiredHeight))
                    {
                        string newFileName = file.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
                        string newFolder = Path.GetDirectoryName(newFileName);

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }
                        newBitmap.Save(newFileName);
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

        public static string[] GetFrontImageFolders()
        {
            string[] imageFrontFolders;
            try
            {
                // get the index from the big box xml file
                var launchBoxSettingsDocument = XDocument.Load(DirectoryInfoHelper.Instance.LaunchBoxSettingsFile);
                var setting = from xmlElement in launchBoxSettingsDocument.Root.Descendants("Settings")
                              select xmlElement.Element("FrontImageTypePriorities").Value;
                string value = setting.FirstOrDefault();

                string[] splitter = new string[] { "," };
                imageFrontFolders = value.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "GetFrontImageFolders");

                // default folders 
                imageFrontFolders = new string[]
                {
                    "GOG Poster",
                    "Steam Poster",
                    "Epic Games Poster",
                    "Box - Front",
                    "Box - Front - Reconstructed",
                    "Advertisement Flyer - Front",
                    "Origin Poster",
                    "Uplay Thumbnail",
                    "Fanart - Box - Front",
                    "Steam Banner"
                };
            }

            return imageFrontFolders;
        }

        public static Bitmap Crop(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, bool> allWhiteRow = row =>
            {
                for (int i = 0; i < w; ++i)
                {
                    Color color = bmp.GetPixel(i, row);
                    byte aValue = color.A;

                    if(aValue != 0)
                    {
                        return false;
                    }
                }
                return true;
            };

            Func<int, bool> allWhiteColumn = col =>
            {
                for (int i = 0; i < h; ++i)
                {
                    Color color = bmp.GetPixel(col, i);
                    byte aValue = color.A;

                    if(aValue != 0)
                    {
                        return false;
                    }
                }
                return true;
            };

            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (allWhiteRow(row))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (allWhiteRow(row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (allWhiteColumn(col))
                    leftmost = col;
                else
                    break;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                if (allWhiteColumn(col))
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
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
