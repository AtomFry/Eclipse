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
    public sealed class CustomListDefinitionDataService
    {
        private readonly string CustomListsFile = DirectoryInfoHelper.Instance.CustomListsFile;

        // Per-item add, update and delete used to live here, each reading the whole file,
        // editing the list and writing it back - an unsynchronised read-modify-write, and the
        // update pair threw on an id that was not in the file. The settings window now owns the
        // list while it is open and writes it whole, so there is one write path and no read step
        // to race.

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

        // The prefix on this file's timestamped backups. Deliberately not derived from the file
        // name - it does not match it, and existing installations already have backups under it.
        private const string BackupPrefix = "CustomGameLists_";

        private void SaveToFile(List<CustomListDefinition> customListDefinitionList)
        {
            JsonFileStore.Write(CustomListsFile, customListDefinitionList, BackupPrefix);
        }

        private List<CustomListDefinition> ReadFromFile()
        {
            // Same treatment as the settings file: a custom-list file that will not parse falls
            // back to the newest backup that does, and then to the default lists, rather than
            // throwing from wherever it happened to be read.
            List<CustomListDefinition> customListDefinitions =
                JsonFileStore.Read(CustomListsFile, BackupPrefix, GetDefaultCustomLists);

            // first run - there was no file, so write the defaults out
            if (!File.Exists(CustomListsFile))
            {
                DirectoryInfoHelper.CreateFolders();
                SaveToFile(customListDefinitions);
            }

            return customListDefinitions;
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
}
