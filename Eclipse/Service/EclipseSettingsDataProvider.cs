using Eclipse.Models;

namespace Eclipse.Service
{
    /// <summary>
    /// The settings this process is running on.
    ///
    /// Read once, on first access, and shared by everything after that - twenty-six call sites
    /// across sixteen files, from <c>GameList</c> to the key strategies. Nothing writes it, and
    /// nothing reloads it: a change saved in the LaunchBox settings window reaches Big Box at its
    /// next start, because they are different processes. See "When settings take effect" in
    /// docs/features/configuration.md.
    ///
    /// This type used to also carry the editor's read and write methods - three forwarders to
    /// <see cref="EclipseSettingsDataService"/> with one caller each - so the twenty-six readers
    /// of the property below had to look past an API they never used. The editor talks to the
    /// data service directly now, and this is left doing the one job its name describes.
    /// </summary>
    public class EclipseSettingsDataProvider
    {
        private EclipseSettings eclipseSettings;

        public EclipseSettings EclipseSettings
        {
            get
            {
                if (eclipseSettings == null)
                {
                    eclipseSettings = EclipseSettingsDataService.Instance.GetEclipseSettings();
                }
                return eclipseSettings;
            }
        }

        #region singleton implementation
        public static EclipseSettingsDataProvider Instance => instance;

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
