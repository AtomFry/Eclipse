using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

// pre-scale images so that we dont incur the cost of scaling while moving around in the front end
// see formula below for logic 
// only scaling the game front images - background and logo images do not incur such a performance 
// hit because they load up far less frequently
namespace Eclipse.Service
{
    public class ImageScaler
    {
        public static int GetDesiredHeight()
        {
            /*
             *                                      = (5/18)*Height - 4         =Height / 2         =(4/18)*Height
             * Resolution	Width	Height	        Game Front Height	        Background Height	Logo Height
                Laptop	    1366	768	            209.3333333	                384	                170.6666667
                1080	    1920	1080	        296	                        540	                240
                1440	    2560	1440	        396	                        720	                320
                4K	        3840	2160	        596	                        1080	            480
             */
            // pick up the monitor from the big box settings file and determine it's height
            // then set the desired size of box images to 5/18 -4 of that size
            // front end UI has 18 rows.  The boxes scale to fit 5 rows and have a 2 pixel border on all sides 
            // so the scaled image height should be = (monitor height * 5/18) - 4
            return (int)(GetMonitorHeight() * 5 / 18) - 4;
        }

        public static List<FileInfo> GetMissingPlatformClearLogoFiles()
        {
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(Helpers.PlatformImagesPath);
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
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(Helpers.LaunchboxImagesPath);
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
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(Helpers.LaunchboxImagesPath);
            List<string> foldersToProcess = new List<string>();
            List<FileInfo> filesToProcess = new List<FileInfo>();
            string[] imageFolders = GetClearLogoFolders();

            // loop through platform folders
            foreach (string platformImageDirectory in platformImageDirectories)
            {
                // loop through box front image folders 
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
            string pathB = directory.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);

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
                        string newFileName = file.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                        string newFolder = Path.GetDirectoryName(newFileName);

                        if (!Directory.Exists(newFolder))
                        {
                            Directory.CreateDirectory(newFolder);
                        }

                        Bitmap croppedBitmap = Crop(newBitmap);
                        croppedBitmap.Save(newFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while scaling an image: {0}", ex.Message);
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
                        string newFileName = file.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
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
                Console.WriteLine($"An error occurred while scaling an image: {0}", ex.Message);
            }
        }

        // gets the index of the monitor from the big box settings file and returns it's height
        // defaults to 1440 if anything goes wrong
        // this height is used for prescaling images to the right size
        public static int GetMonitorHeight()
        {
            int defaultHeight = 1440;
            int monitorHeight;
            try
            {
                monitorHeight = Screen.FromHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle).Bounds.Height;
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetMonitorHeight");
                Helpers.Log($"Monitor height not found - defaulting to {defaultHeight}");
                monitorHeight = defaultHeight;
            }

            return monitorHeight;
        }

        public static int GetMonitorWidth()
        {
            int defaultWidth = 2560;
            int monitorWidth;
            try
            {
                monitorWidth = Screen.FromHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle).Bounds.Width;
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetMonitorWidth");
                Helpers.Log($"Monitor width not found - defaulting to {defaultWidth}");
                monitorWidth = defaultWidth;
            }

            return monitorWidth;
        }

        public static string[] GetClearLogoFolders()
        {
            return new string[] { Helpers.ClearLogoFolder };
        }

        public static string[] GetFrontImageFolders()
        {
            string[] imageFrontFolders;
            try
            {
                // get the index from the big box xml file
                var launchBoxSettingsDocument = XDocument.Load(Helpers.LaunchBoxSettingsFile);
                var setting = from xmlElement in launchBoxSettingsDocument.Root.Descendants("Settings")
                              select xmlElement.Element("FrontImageTypePriorities").Value;
                string value = setting.FirstOrDefault();

                string[] splitter = new string[] { "," };
                imageFrontFolders = value.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetFrontImageFolders");

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
