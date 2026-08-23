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
}
