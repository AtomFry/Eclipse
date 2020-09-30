using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBoxNetflixUI.Service
{
    public class ImageScaler
    {
        public static void ScaleImages()
        {
            // enumerate platform directories 
            IEnumerable<string> platformImageDirectories = Directory.EnumerateDirectories(Helpers.LaunchboxImagesPath);
            foreach (string platformImageDirectory in platformImageDirectories)
            {
                IEnumerable<string> imageDirectories = Directory.EnumerateDirectories(platformImageDirectory);
                foreach (string imageDirectory in imageDirectories)
                {
                    // todo: get list of directories from launchbox settings
                    if (imageDirectory.EndsWith("GOG Poster", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Steam Poster", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Epic Games Poster", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Box - Front", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Box - Front - Reconstructed", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Advertisement Flyer - Front", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Origin Poster", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Uplay Thumbnail", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Fanart - Box - Front", StringComparison.InvariantCultureIgnoreCase)
                    || imageDirectory.EndsWith("Steam Banner", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // todo: determine desired height from the system resolution
                        /*
                         *                                      = (5/18)*Height - 4         =Height / 2         =(4/18)*Height
                         * Resolution	Width	Height	        Game Front Height	        Background Height	Logo Height
                            Laptop	    1366	768	            209.3333333	                384	                170.6666667
                            1080	    1920	1080	        296	                        540	                240
                            1440	    2560	1440	        396	                        720	                320
                            4K	        3840	2160	        596	                        1080	            480
                         */
                        ProcessImagesInDirectory(imageDirectory, 209);
                    }
                }
            }
        }

        public static System.Drawing.Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new System.Drawing.Bitmap(width, height);

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

        public static void ProcessImagesInDirectory(string directory, int desiredHeight)
        {
            IEnumerable<string> folders = Directory.EnumerateDirectories(directory);
            foreach (string folder in folders)
            {
                ProcessDirectory(folder, desiredHeight);
            }
            ProcessDirectory(directory, desiredHeight);
        }


        public static void ProcessDirectory(string directory, int desiredHeight)
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
            var queryList1Only = (from file in list1
                                  select file).Except(list2, fileCompare);


            foreach (var v in queryList1Only)
            {
                try
                {
                    string file = v.FullName;
                    int originalHeight, originalWidth, desiredWidth;
                    double scale;

                    using (System.Drawing.Image originalImage = System.Drawing.Bitmap.FromFile(file))
                    {
                        originalHeight = originalImage.Height;
                        originalWidth = originalImage.Width;
                        scale = (double)((double)desiredHeight / (double)originalHeight);
                        desiredWidth = (int)(originalWidth * scale);

                        using (System.Drawing.Bitmap newBitmap = ResizeImage(originalImage, desiredWidth, desiredHeight))
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
                    Console.WriteLine($"An error occurred: {0}", ex.Message);
                }
            }
        }
    }

    // This implementation defines a very simple comparison  
    // between two FileInfo objects. It only compares the name  
    // of the files being compared  
    class FileCompare : System.Collections.Generic.IEqualityComparer<System.IO.FileInfo>
    {
        public FileCompare() { }

        public bool Equals(System.IO.FileInfo f1, System.IO.FileInfo f2)
        {
            return (f1.Name == f2.Name);
        }

        // Return a hash that reflects the comparison criteria. According to the
        // rules for IEqualityComparer<T>, if Equals is true, then the hash codes must  
        // also be equal. Because equality as defined here is a simple value equality, not  
        // reference identity, it is possible that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(System.IO.FileInfo fi)
        {
            string s = $"{fi.Name}";
            return s.GetHashCode();
        }
    }
}
