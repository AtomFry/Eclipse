using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Helpers
{
    public sealed class DirectoryInfoHelper
    {
        private static readonly DirectoryInfoHelper instance = new DirectoryInfoHelper();

        // path to images will incorporate the screen resolution because images will be pre-scaled
        // based on the screen resolution
        private static readonly DisplayInfoHelper displayInfoHelper = DisplayInfoHelper.Instance;

        // path to the big box application directory
        public static string ApplicationPath
        {
            get 
            {
                // todo: determine how to identify if this is 11.3? and later where the switch to .net core then the folder is one level deeper
                // new version 
                return Directory.GetParent(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)).FullName;

                // old version
                // return Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
        }

        // path to launchbox images folder 
        public static string LaunchboxImagesPath
        {
            get
            {
                return $"{ApplicationPath}\\Images";
            }
        }

        public static string LaunchboxImagesPlatformsPath
        {
            get
            {
                return $"{LaunchboxImagesPath}\\Platforms";
            }
        }

        public static string EclipseFolder
        {
            get
            {
                return $"{ApplicationPath}\\Plugins\\Eclipse";
            }
        }

        public static string MediaFolder
        {
            get
            {
                return $"{EclipseFolder}\\Media";
            }
        }

        public static string MediaResolutionSpecificFolder
        {
            get
            {
                return $"{MediaFolder}\\{displayInfoHelper.displayWidth}x{displayInfoHelper.displayHeight}";
            }
        }


        public static string PluginImagesPath
        {
            get
            {
                return $"{MediaResolutionSpecificFolder}\\Images";
            }
        }

        public static string DefaultBoxFrontImageFilePath
        {
            get
            {
                return $"{MediaResolutionSpecificFolder}\\DefaultFrontImage";
            }
        }

        public static string DefaultBoxFrontImageFileName
        {
            get
            {
                return "DefaultFrontImage.png";
            }
        }

        public static string DefaultBoxFrontImageFullPath
        {
            get
            {
                return Path.Combine(DefaultBoxFrontImageFilePath, DefaultBoxFrontImageFileName);
            }
        }

        public static string ClearLogoFolder
        {
            get { return "Clear logo"; }
        }


        public static string BigBoxSettingsFile
        {
            get
            {
                return $"{DirectoryInfoHelper.ApplicationPath}\\Data\\BigBoxSettings.xml";
            }
        }

        public static string LaunchBoxSettingsFile
        {
            get
            {
                return $"{DirectoryInfoHelper.ApplicationPath}\\Data\\Settings.xml";
            }
        }


        public static void CreateFolders()
        {
            LogHelper.Log("CreateFolders");

            CreateFolder(EclipseFolder);
            CreateFolder(MediaFolder);
            CreateFolder(MediaResolutionSpecificFolder);
            CreateFolder(PluginImagesPath);
        }

        public static void CreateFolder(string path)
        {
            LogHelper.Log($"CreateFolder: {path}");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string ResourceFolder
        {
            get
            {
                return "pack://application:,,,/Eclipse;component/resources";
            }
        }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static DirectoryInfoHelper()
        {
        }

        private DirectoryInfoHelper()
        {

        }

        public static DirectoryInfoHelper Instance
        {
            get
            {
                return instance;
            }
        }
    }

}
