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

        // The prefix on this file's timestamped backups. Unchanged from what installations
        // already have on disk, so existing backups stay recognisable.
        private const string BackupPrefix = "EclipseSettings_";

        private void SaveToFile(EclipseSettings eclipseSettings)
        {
            JsonFileStore.Write(EclipseSettingsFile, eclipseSettings, BackupPrefix);
        }

        private EclipseSettings ReadFromFile()
        {
            // A settings file that cannot be parsed no longer throws out of here. It is reached
            // from field initialisers and constructors, where an exception is both unrecoverable
            // and impossible to attribute - so the store falls back to the newest backup that
            // reads, and then to defaults.
            EclipseSettings eclipseSettings =
                JsonFileStore.Read(EclipseSettingsFile, BackupPrefix, GetDefaultSettings);

            // first run - there was no file, so write the defaults out
            if (!File.Exists(EclipseSettingsFile))
            {
                DirectoryInfoHelper.CreateFolders();
                SaveToFile(eclipseSettings);
            }

            return eclipseSettings;
        }

        private EclipseSettings GetDefaultSettings()
        {
            // Used only when no settings file exists yet. Every property on EclipseSettings
            // declares its default as a [DefaultValue] attribute with
            // DefaultValueHandling.Populate, which Json.NET already applies when a property
            // is missing from an existing file. Deserialising an empty object runs that same
            // path, so defaults have one home instead of two - a hand-maintained list here
            // silently drifts every time a setting is added, and a missing entry hands a new
            // installation a zero instead of the intended value.
            return JsonConvert.DeserializeObject<EclipseSettings>("{}");
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
