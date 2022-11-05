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
        private string applicationPath;
        public string ApplicationPath
        {
            get 
            {
                if(string.IsNullOrWhiteSpace(applicationPath))
                {
                    // original application root folder
                    applicationPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                    string folder = new DirectoryInfo(applicationPath).Name;

                    if(folder.Equals("core", StringComparison.InvariantCultureIgnoreCase))
                    {
                        applicationPath = Directory.GetParent(applicationPath).FullName;
                    }
                }

                return applicationPath;
            }
        }

        // path to launchbox images folder 
        private string launchboxImagesPath;
        public string LaunchboxImagesPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(launchboxImagesPath))
                {
                    launchboxImagesPath = $"{ApplicationPath}\\Images";
                }

                return launchboxImagesPath;
            }
        }

        private string launchboxImagesPlatformsPath;
        public string LaunchboxImagesPlatformsPath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(launchboxImagesPlatformsPath))
                {
                    launchboxImagesPlatformsPath = $"{LaunchboxImagesPath}\\Platforms";
                }
                return launchboxImagesPlatformsPath;
            }
        }

        private string eclipseFolder;
        public string EclipseFolder
        {
            get
            {
                if(string.IsNullOrWhiteSpace(eclipseFolder))
                {
                    eclipseFolder = $"{ApplicationPath}\\Plugins\\Eclipse";
                }
                return eclipseFolder;
            }
        }

        private string settingsFolder;
        public string SettingsFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(settingsFolder))
                {
                    settingsFolder = $"{EclipseFolder}\\Settings";
                }
                return settingsFolder;
            }
        }

        private string eclipseSettingsFile;
        public string EclipseSettingsFile
        {
            get
            {
                if (string.IsNullOrWhiteSpace(eclipseSettingsFile))
                {
                    eclipseSettingsFile = $"{SettingsFolder}\\EclipseSettings.json";
                }
                return eclipseSettingsFile;
            }
        }

        private string customListsFile;
        public string CustomListsFile
        {
            get
            {
                if (string.IsNullOrWhiteSpace(customListsFile))
                {
                    customListsFile = $"{SettingsFolder}\\CustomLists.json";
                }
                return customListsFile;
            }
        }

        private string settingsBackupPath;
        public string SettingsBackupPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(settingsBackupPath))
                {
                    settingsBackupPath = $"{SettingsFolder}\\DataBackup";
                }
                return settingsBackupPath;
            }
        }

        private string mediaFolder;
        public string MediaFolder
        {
            get
            {
                if(string.IsNullOrWhiteSpace(mediaFolder))
                {
                    mediaFolder = $"{EclipseFolder}\\Media";
                }
                return mediaFolder; 
            }
        }

        private string bezelFolder;
        public string BezelFolder
        {
            get
            {
                if(string.IsNullOrWhiteSpace(bezelFolder))
                {
                    bezelFolder = $"{MediaFolder}\\Bezels";
                }
                return bezelFolder;
            }
        }

        private string defaultBackgroundImagePath;
        public string DefaultBackgroundImagePath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(defaultBackgroundImagePath))
                {
                    defaultBackgroundImagePath = $"{MediaFolder}\\DefaultBackground\\DefaultBackground.jpg";
                }
                return defaultBackgroundImagePath;
            }
        }

        private string mediaResolutionSpecificFolder;
        public string MediaResolutionSpecificFolder
        {
            get
            {
                if(string.IsNullOrWhiteSpace(mediaResolutionSpecificFolder))
                {
                    mediaResolutionSpecificFolder = $"{MediaFolder}\\{displayInfoHelper.displayWidth}x{displayInfoHelper.displayHeight}";
                }
                return mediaResolutionSpecificFolder;             
            }
        }

        private string pluginImagesPath;
        public string PluginImagesPath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(pluginImagesPath))
                {
                    pluginImagesPath = $"{MediaResolutionSpecificFolder}\\Images";
                }
                return pluginImagesPath;
            }
        }

        private string defaultBoxFrontImageFilePath;
        public string DefaultBoxFrontImageFilePath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(defaultBoxFrontImageFilePath))
                {
                    defaultBoxFrontImageFilePath = $"{MediaResolutionSpecificFolder}\\DefaultFrontImage";
                }
                return defaultBoxFrontImageFilePath; 
            }
        }

        private string defaultBoxFrontImageFileName;
        public string DefaultBoxFrontImageFileName
        {
            get
            {
                if(string.IsNullOrWhiteSpace(defaultBoxFrontImageFileName))
                {
                    defaultBoxFrontImageFileName = "DefaultFrontImage.png";
                }
                return defaultBoxFrontImageFileName; 
            }
        }

        public string defaultBoxFrontImageFullPath;
        public string DefaultBoxFrontImageFullPath
        {
            get
            {
                if(string.IsNullOrWhiteSpace(defaultBoxFrontImageFullPath))
                {
                    defaultBoxFrontImageFullPath = Path.Combine(DefaultBoxFrontImageFilePath, DefaultBoxFrontImageFileName);
                }
                return defaultBoxFrontImageFullPath; 
            }
        }

        private string clearLogoFolder;
        public string ClearLogoFolder
        {
            get 
            {
                if(string.IsNullOrWhiteSpace(clearLogoFolder))
                {
                    clearLogoFolder = "Clear logo";
                }
                return clearLogoFolder; 
            }
        }

        private string bigBoxSettingsFile;
        public string BigBoxSettingsFile
        {
            get
            {
                if(string.IsNullOrWhiteSpace(BigBoxSettingsFile))
                {
                    bigBoxSettingsFile = $"{ApplicationPath}\\Data\\BigBoxSettings.xml";
                }
                return bigBoxSettingsFile;
            }
        }

        private string launchBoxSettingsFile;
        public string LaunchBoxSettingsFile
        {
            get
            {
                if(string.IsNullOrWhiteSpace(launchBoxSettingsFile))
                {
                    launchBoxSettingsFile = $"{ApplicationPath}\\Data\\Settings.xml";
                }
                return launchBoxSettingsFile; 
            }
        }

        public static void CreateFolders()
        {
            CreateFolder(Instance.SettingsFolder);
            CreateFolder(Instance.EclipseFolder);
            CreateFolder(Instance.MediaFolder);
            CreateFolder(Instance.MediaResolutionSpecificFolder);
            CreateFolder(Instance.PluginImagesPath);
        }

        public static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void CreateDirectoryIfNotExists(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
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

        public static DirectoryInfoHelper Instance => instance;
    }
}
