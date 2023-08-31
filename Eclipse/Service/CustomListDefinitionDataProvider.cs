using Eclipse.Helpers;
using Eclipse.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    public class CustomListDefinitionDataProvider
    {
        public CustomListDefinitionDataProvider()
        {
        }

        public async Task<CustomListDefinition> GetCustomListDefinitionByIdAsync(string id)
        {
            return await CustomListDefinitionDataService.Instance.GetCustomListDefinitionByIdAsync(id);
        }

        public async Task SaveCustomListDefinitionAsync(CustomListDefinition customListDefinition)
        {
            await CustomListDefinitionDataService.Instance.SaveCustomListDefinitionAsync(customListDefinition);
        }

        public async Task DeleteCustomListDefinition(string id)
        {
            await CustomListDefinitionDataService.Instance.DeleteCustomListDefinitionAsync(id);
        }

        public async Task<IEnumerable<CustomListDefinition>> GetAllCustomListDefinitionsAsync()
        {
            return await Task.Run(() =>
            {
                return GetAllCustomListDefinitions();
            });
        }

        public IEnumerable<CustomListDefinition> GetAllCustomListDefinitions()
        {
            return CustomListDefinitionDataService.Instance.GetAllCustomListDefinitions();
        }

        public async Task SaveCustomListDefinitionsAsync(List<CustomListDefinition> customListDefinitions)
        {
            await CustomListDefinitionDataService.Instance.SaveCustomListDefinitionsAsync(customListDefinitions);
        }
    }

    public sealed class CustomListDefinitionDataService
    {
        private readonly string CustomListsFile = DirectoryInfoHelper.Instance.CustomListsFile;

        public async Task<CustomListDefinition> GetCustomListDefinitionByIdAsync(string id)
        {
            List<CustomListDefinition> customListDefinitions = await ReadFromFileAsync();
            return customListDefinitions.Single(f => f.Id == id);
        }

        public async Task SaveCustomListDefinitionAsync(CustomListDefinition customListDefinition)
        {
            if (string.IsNullOrWhiteSpace(customListDefinition.Id))
            {
                await InsertCustomListDefinitionAsync(customListDefinition);
            }
            else
            {
                await UpdateCustomListDefinitionAsync(customListDefinition);
            }
        }

        public async Task DeleteCustomListDefinitionAsync(string id)
        {
            List<CustomListDefinition> customListDefinitions = await ReadFromFileAsync();
            CustomListDefinition existing = customListDefinitions.Single(f => f.Id == id);
            customListDefinitions.Remove(existing);
            await SaveToFileAsync(customListDefinitions);
        }

        private async Task UpdateCustomListDefinitionAsync(CustomListDefinition customListDefinition)
        {
            List<CustomListDefinition> customListDefinitions = await ReadFromFileAsync();
            CustomListDefinition existing = customListDefinitions.SingleOrDefault(f => f.Id == customListDefinition.Id);
            int indexOfExisting = customListDefinitions.IndexOf(existing);
            customListDefinitions.Insert(indexOfExisting, customListDefinition);
            customListDefinitions.Remove(existing);
            await SaveToFileAsync(customListDefinitions);
        }

        private async Task InsertCustomListDefinitionAsync(CustomListDefinition customListDefinition)
        {
            List<CustomListDefinition> customListDefinitions = await ReadFromFileAsync();
            customListDefinition.Id = Guid.NewGuid().ToString();
            customListDefinitions.Add(customListDefinition);
            await SaveToFileAsync(customListDefinitions);
        }

        public IEnumerable<CustomListDefinition> GetAllCustomListDefinitions()
        {
            return ReadFromFile();
        }


        public async Task<IEnumerable<CustomListDefinition>> GetAllCustomListDefinitionsAsync()
        {
            return await Task.Run(() =>
            {
                return GetAllCustomListDefinitions();
            });
        }

        public async Task SaveCustomListDefinitionsAsync(List<CustomListDefinition> customListDefinitions)
        {
            await SaveToFileAsync(customListDefinitions);
        }

        private async Task SaveToFileAsync(List<CustomListDefinition> customListDefinitionList)
        {
            await Task.Run(() =>
            {
                SaveToFile(customListDefinitionList);
            });
        }

        private void SaveToFile(List<CustomListDefinition> customListDefinitionList)
        {
            BackupDataFile();

            string json = JsonConvert.SerializeObject(customListDefinitionList, Formatting.Indented);
            File.WriteAllText(CustomListsFile, json);
        }

        private List<CustomListDefinition> ReadFromFile()
        {
            // make sure the data file exists 
            if (!File.Exists(CustomListsFile))
            {
                // make sure the folders exist 
                DirectoryInfoHelper.CreateFolders();

                // create a sample list 
                List<CustomListDefinition> customListDefinitions = GetDefaultCustomLists();

                // save the file 
                SaveToFile(customListDefinitions);

                return customListDefinitions;
            }

            // read and deserialize the file
            string json = File.ReadAllText(CustomListsFile);
            return JsonConvert.DeserializeObject<List<CustomListDefinition>>(json);
        }


        private async Task<List<CustomListDefinition>> ReadFromFileAsync()
        {
            return await Task.Run(() =>
            {
                return ReadFromFile();
            });
        }

        private List<CustomListDefinition> GetDefaultCustomLists()
        {
            List<CustomListDefinition> customListDefinitions = new List<CustomListDefinition>();

            CustomListDefinition favoriteGamesListDefinition = new CustomListDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Favorites",
                MaxGamesInList = 0
            };
            favoriteGamesListDefinition.FilterExpressions.Add(new FilterExpression()
            {
                GameFieldEnum = GameFieldEnum.Favorite,
                FilterFieldOperator = FilterFieldOperator.Equal,
                FilterFieldValue = true
            });
            favoriteGamesListDefinition.SortExpressions.Add(new SortExpression()
            {
                GameFieldEnum = GameFieldEnum.SortTitleOrTitle,
                SortDirection = SortDirection.Ascending
            });
            favoriteGamesListDefinition.ListCategoryTypes.Add(ListCategoryType.Platform);
            favoriteGamesListDefinition.ListCategoryTypes.Add(ListCategoryType.Playlist);

            CustomListDefinition historyListDefinition = new CustomListDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Description = "History"
            };
            historyListDefinition.FilterExpressions.Add(new FilterExpression()
            {
                GameFieldEnum = GameFieldEnum.LastPlayedDate,
                FilterFieldOperator = FilterFieldOperator.IsNotNull,
                FilterFieldValue = null
            });
            historyListDefinition.SortExpressions.Add(new SortExpression()
            {
                GameFieldEnum = GameFieldEnum.LastPlayedDate,
                SortDirection = SortDirection.Descending
            });
            historyListDefinition.ListCategoryTypes.Add(ListCategoryType.Platform);
            historyListDefinition.ListCategoryTypes.Add(ListCategoryType.Playlist);

            customListDefinitions.Add(historyListDefinition);
            customListDefinitions.Add(favoriteGamesListDefinition);

            return customListDefinitions;
        }

        private void BackupDataFile()
        {
            // copy the file to backup folder before writing
            if (File.Exists(CustomListsFile))
            {
                string currentTimeString = DateTime.Now.ToString("yyyyMMdd_H_mm_ss");
                string newFilePath = $"{DirectoryInfoHelper.Instance.SettingsBackupPath}\\CustomGameLists_{currentTimeString}.json";
                DirectoryInfoHelper.CreateDirectoryIfNotExists(newFilePath);
                File.Copy(CustomListsFile, newFilePath);
            }
        }

        #region singleton implementation 
        public static CustomListDefinitionDataService Instance
        {
            get
            {
                return instance;
            }
        }

        private static readonly CustomListDefinitionDataService instance = new CustomListDefinitionDataService();

        static CustomListDefinitionDataService()
        {
        }

        private CustomListDefinitionDataService()
        {
        }
        #endregion
    }

    public class EclipseSettingsDataProvider
    {
        public EclipseSettings GetEclipseSettings()
        {
            return EclipseSettingsDataService.Instance.GetEclipseSettings();
        }

        public async Task<EclipseSettings> GetEclipseSettingsAsync()
        {
            return await Task.Run(() =>
            {
                return GetEclipseSettings();
            });
        }

        public async Task SaveEclipseSettingsAsync(EclipseSettings eclipseSettings)
        {
            await EclipseSettingsDataService.Instance.SaveEclipseSettingsAsync(eclipseSettings);
        }

        private EclipseSettings eclipseSettings;
        public EclipseSettings EclipseSettings
        {
            get
            {
                if (eclipseSettings == null)
                {
                    eclipseSettings = instance.GetEclipseSettings();
                }
                return eclipseSettings;
            }

            set => eclipseSettings = value;
        }

        #region singleton implementation 
        public static EclipseSettingsDataProvider Instance
        {
            get
            {
                return instance;
            }
        }

        private static readonly EclipseSettingsDataProvider instance = new EclipseSettingsDataProvider();

        static EclipseSettingsDataProvider()
        {
        }

        private EclipseSettingsDataProvider()
        {
        }
        #endregion
    }

    public sealed class EclipseSettingsDataService
    {
        private readonly string EclipseSettingsFile = DirectoryInfoHelper.Instance.EclipseSettingsFile;

        public EclipseSettings GetEclipseSettings()
        {
            return ReadFromFile();
        }

        public async Task<EclipseSettings> GetEclipseSettingsAsync()
        {
            return await Task.Run(() =>
            {
                return GetEclipseSettings();
            });
        }

        public async Task SaveEclipseSettingsAsync(EclipseSettings eclipseSettings)
        {
            await SaveToFileAsync(eclipseSettings);
        }

        private async Task SaveToFileAsync(EclipseSettings eclipseSettings)
        {
            await Task.Run(() =>
            {
                SaveToFile(eclipseSettings);
            });
        }

        private void SaveToFile(EclipseSettings eclipseSettings)
        {
            BackupDataFile();

            string json = JsonConvert.SerializeObject(eclipseSettings, Formatting.Indented);
            File.WriteAllText(EclipseSettingsFile, json);
        }

        private EclipseSettings ReadFromFile()
        {
            // make sure the data file exists 
            if (!File.Exists(EclipseSettingsFile))
            {
                // make sure the folders exist 
                DirectoryInfoHelper.CreateFolders();

                // create a sample list 
                EclipseSettings defaultEclipseSettings = GetDefaultSettings();

                // save the file 
                SaveToFile(defaultEclipseSettings);

                return defaultEclipseSettings;
            }

            // read and deserialize the file
            string json = File.ReadAllText(EclipseSettingsFile);

            return JsonConvert.DeserializeObject<EclipseSettings>(json);
        }

        private async Task<EclipseSettings> ReadFromFileAsync()
        {
            return await Task.Run(() =>
            {
                return ReadFromFile();
            });
        }

        private EclipseSettings GetDefaultSettings()
        {
            EclipseSettings eclipseSettings = new EclipseSettings
            {
                DefaultListCategoryType = ListCategoryType.Platform,

                DisableVideos = false,
                EnableScreenSaver = true,
                EnableVoiceSearch = true,
                ShowGameCountInList = true,
                IncludeBrokenGames = false,
                IncludeHiddenGames = false,
                OpenSettingsPaneOnLeft = true,
                AdditionalVersionsEnable = true,
                AdditionalVersionsExcludeRunBefore = true,
                AdditionalVersionsExcludeRunAfter = true,
                AdditionalVersionsOnlyEmulatorOrDosBox = true,
                AdditionalApplicationDisplayField = AdditionalApplicationDisplayField.Name,
                AdditionalVersionsRemovePlayPrefix = true,
                AdditionalVersionsRemoveVersionPostfix = true,

                PageDownFunction = PageFunction.VoiceSearch,
                PageUpFunction = PageFunction.RandomGame,

                ScreensaverDelayInSeconds = 90,
                VideoDelayInMilliseconds = 2000,
                DefaultVideoVolume = 0.5,
                BypassDetails = false,
                RepeatGamesToFillScreen = true,
                ShowMatchPercent = true,
                ShowPlatformLogo = true,
                ShowPlayMode = true,
                ShowReleaseYear = true,
                ShowStarRating = true,
                ShowOptionsIcon = true, 

                BoxFrontMarginBottom = 2.0,
                BoxFrontMarginLeft = 2.0,
                BoxFrontMarginRight = 2.0,
                BoxFrontMarginTop = 2.0,

                DisplayFeaturedGame = false,
                DisplayOptionsOnEscape = true
            };

            return eclipseSettings;
        }

        private void BackupDataFile()
        {
            // copy the file to backup folder before writing
            if (File.Exists(EclipseSettingsFile))
            {
                string currentTimeString = DateTime.Now.ToString("yyyyMMdd_H_mm_ss");
                string newFilePath = $"{DirectoryInfoHelper.Instance.SettingsBackupPath}\\EclipseSettings_{currentTimeString}.json";
                DirectoryInfoHelper.CreateDirectoryIfNotExists(newFilePath);
                File.Copy(EclipseSettingsFile, newFilePath);
            }
        }

        #region singleton implementation 
        public static EclipseSettingsDataService Instance
        {
            get
            {
                return instance;
            }
        }

        private static readonly EclipseSettingsDataService instance = new EclipseSettingsDataService();

        static EclipseSettingsDataService()
        {
        }

        private EclipseSettingsDataService()
        {
        }
        #endregion
    }
}
