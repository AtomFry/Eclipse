using System;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Tests.Fakes
{
    /// <summary>
    /// A game, as a plain settable object.
    ///
    /// IGame is Eclipse's domain model - that is `S-2`, and the reason most of the product could
    /// not be tested. It carries 88 properties and 34 methods, so nothing can implement it in
    /// passing; this exists once so that everything else can take a game without taking Big Box.
    ///
    /// Every property is settable, including the ones the interface exposes read-only. A fake is
    /// more useful when a test can state exactly the game it means, and nothing here is pretending
    /// to reproduce LaunchBox's own derivations - where a test needs one (SortTitleOrTitle, say)
    /// it sets it.
    ///
    /// The methods all throw. Nothing under test should be calling behaviour on a data object,
    /// and if something does, the exception names it rather than a null quietly doing nothing.
    ///
    /// The member list was generated from the interface by reflection rather than typed out, so
    /// it is complete and in a stable order. Regenerating it is the right move if the contract
    /// assembly is ever updated.
    /// </summary>
    public sealed class FakeGame : IGame
    {
        /// <summary>A game with the fields the tests here actually read.</summary>
        public static FakeGame With(string id, string title, string platform = "Test Platform")
        {
            return new FakeGame
            {
                Id = id,
                Title = title,
                SortTitleOrTitle = title,
                Platform = platform
            };
        }

        public bool AggressiveWindowHiding { get; set; }
        public string ApplicationPath { get; set; }
        public string BackgroundImagePath { get; set; }
        public string BackImagePath { get; set; }
        public string Box3DImagePath { get; set; }
        public bool Broken { get; set; }
        public string Cart3DImagePath { get; set; }
        public string CartBackImagePath { get; set; }
        public string CartFrontImagePath { get; set; }
        public string ClearLogoImagePath { get; set; }
        public string CloneOf { get; set; }
        public string CommandLine { get; set; }
        public float CommunityOrLocalStarRating { get; set; }
        public float CommunityStarRating { get; set; }
        public int CommunityStarRatingTotalVotes { get; set; }
        public bool Completed { get; set; }
        public string ConfigurationCommandLine { get; set; }
        public string ConfigurationPath { get; set; }
        public System.DateTime DateAdded { get; set; }
        public System.DateTime DateModified { get; set; }
        public string DetailsWithoutPlatform { get; set; }
        public string DetailsWithPlatform { get; set; }
        public string Developer { get; set; }
        public string[] Developers { get; set; }
        public bool DisableShutdownScreen { get; set; }
        public string DosBoxConfigurationPath { get; set; }
        public string EmulatorId { get; set; }
        public bool Favorite { get; set; }
        public string FrontImagePath { get; set; }
        public System.Collections.Concurrent.BlockingCollection<string> Genres { get; set; }
        public string GenresString { get; set; }
        public bool Hide { get; set; }
        public bool HideAllNonExclusiveFullscreenWindows { get; set; }
        public bool HideMouseCursorInGame { get; set; }
        public string Id { get; set; }
        public bool? Installed { get; set; }
        public System.DateTime? LastPlayedDate { get; set; }
        public int? LaunchBoxDbId { get; set; }
        public string ManualPath { get; set; }
        public string MarqueeImagePath { get; set; }
        public int? MaxPlayers { get; set; }
        public bool MonitorStartupShutdownWithProcess { get; set; }
        public string MusicPath { get; set; }
        public string Notes { get; set; }
        public bool OverrideDefaultStartupScreenSettings { get; set; }
        public string Platform { get; set; }
        public string PlatformClearLogoImagePath { get; set; }
        public int PlayCount { get; set; }
        public string PlayMode { get; set; }
        public string[] PlayModes { get; set; }
        public int PlayTime { get; set; }
        public bool Portable { get; set; }
        public string Progress { get; set; }
        public string Publisher { get; set; }
        public string[] Publishers { get; set; }
        public string Rating { get; set; }
        public System.Drawing.Image RatingImage { get; set; }
        public string Region { get; set; }
        public System.DateTime? ReleaseDate { get; set; }
        public string ReleaseType { get; set; }
        public int? ReleaseYear { get; set; }
        public string RootFolder { get; set; }
        public string ScreenshotImagePath { get; set; }
        public bool ScummVmAspectCorrection { get; set; }
        public bool ScummVmFullscreen { get; set; }
        public string ScummVmGameDataFolderPath { get; set; }
        public string ScummVmGameType { get; set; }
        public string Series { get; set; }
        public string[] SeriesValues { get; set; }
        public bool ShowBack { get; set; }
        public string SortTitle { get; set; }
        public string SortTitleOrTitle { get; set; }
        public string Source { get; set; }
        public int StarRating { get; set; }
        public float StarRatingFloat { get; set; }
        public int StartupLoadDelay { get; set; }
        public int StartupScreenPostLaunchDisplayTime { get; set; }
        public string Status { get; set; }
        public string ThemeVideoPath { get; set; }
        public string Title { get; set; }
        public bool UseDosBox { get; set; }
        public bool UseScummVm { get; set; }
        public bool UseStartupScreen { get; set; }
        public string Version { get; set; }
        public string VideoPath { get; set; }
        public string VideoUrl { get; set; }
        public int? WikipediaId { get; set; }
        public string WikipediaUrl { get; set; }

        public bool AddControllerSupport(string controllerName, string controllerCategory, int? supportLevel) => throw new NotSupportedException("AddControllerSupport");
        public Unbroken.LaunchBox.Plugins.Data.IAdditionalApplication AddNewAdditionalApplication() => throw new NotSupportedException("AddNewAdditionalApplication");
        public Unbroken.LaunchBox.Plugins.Data.IAlternateName AddNewAlternateName() => throw new NotSupportedException("AddNewAlternateName");
        public Unbroken.LaunchBox.Plugins.Data.ICustomField AddNewCustomField() => throw new NotSupportedException("AddNewCustomField");
        public Unbroken.LaunchBox.Plugins.Data.IMount AddNewMount() => throw new NotSupportedException("AddNewMount");
        public string Configure() => throw new NotSupportedException("Configure");
        public Unbroken.LaunchBox.Plugins.Data.IAdditionalApplication[] GetAllAdditionalApplications() => throw new NotSupportedException("GetAllAdditionalApplications");
        public Unbroken.LaunchBox.Plugins.Data.IAlternateName[] GetAllAlternateNames() => throw new NotSupportedException("GetAllAlternateNames");
        public Unbroken.LaunchBox.Plugins.Data.ICustomField[] GetAllCustomFields() => throw new NotSupportedException("GetAllCustomFields");
        public Unbroken.LaunchBox.Plugins.Data.ImageDetails[] GetAllImagesWithDetails() => throw new NotSupportedException("GetAllImagesWithDetails");
        public Unbroken.LaunchBox.Plugins.Data.ImageDetails[] GetAllImagesWithDetails(string imageType) => throw new NotSupportedException("GetAllImagesWithDetails");
        public Unbroken.LaunchBox.Plugins.Data.IMount[] GetAllMounts() => throw new NotSupportedException("GetAllMounts");
        public string GetBigBoxDetails(bool showPlatform) => throw new NotSupportedException("GetBigBoxDetails");
        public System.Collections.Generic.KeyValuePair<Unbroken.LaunchBox.Plugins.Data.IGameController, int?>[] GetControllerSupport() => throw new NotSupportedException("GetControllerSupport");
        public string GetEffectiveCommandLine() => throw new NotSupportedException("GetEffectiveCommandLine");
        public string GetManualPath() => throw new NotSupportedException("GetManualPath");
        public string GetMusicPath() => throw new NotSupportedException("GetMusicPath");
        public string GetNewManualFilePath(string extension) => throw new NotSupportedException("GetNewManualFilePath");
        public string GetNewMusicFilePath(string extension) => throw new NotSupportedException("GetNewMusicFilePath");
        public string GetNewThemeVideoFilePath(string extension) => throw new NotSupportedException("GetNewThemeVideoFilePath");
        public string GetNewVideoFilePath(string extension) => throw new NotSupportedException("GetNewVideoFilePath");
        public string GetNextAvailableImageFilePath(string extension, string imageType, string region) => throw new NotSupportedException("GetNextAvailableImageFilePath");
        public string GetNextVideoFilePath(string videoType, string extension) => throw new NotSupportedException("GetNextVideoFilePath");
        public string GetThemeVideoPath() => throw new NotSupportedException("GetThemeVideoPath");
        public string GetVideoPath(bool prioritizeThemeVideos) => throw new NotSupportedException("GetVideoPath");
        public string GetVideoPath(string videoType) => throw new NotSupportedException("GetVideoPath");
        public string OpenFolder() => throw new NotSupportedException("OpenFolder");
        public string OpenManual() => throw new NotSupportedException("OpenManual");
        public string Play() => throw new NotSupportedException("Play");
        public bool TryRemoveAdditionalApplication(Unbroken.LaunchBox.Plugins.Data.IAdditionalApplication additionalApplication) => throw new NotSupportedException("TryRemoveAdditionalApplication");
        public bool TryRemoveAlternateNames(Unbroken.LaunchBox.Plugins.Data.IAlternateName alternateName) => throw new NotSupportedException("TryRemoveAlternateNames");
        public bool TryRemoveCustomField(Unbroken.LaunchBox.Plugins.Data.ICustomField customField) => throw new NotSupportedException("TryRemoveCustomField");
        public bool TryRemoveMount(Unbroken.LaunchBox.Plugins.Data.IMount mount) => throw new NotSupportedException("TryRemoveMount");
        public void UpdateTitleAndMigrateMedia(string newTitle) => throw new NotSupportedException("UpdateTitleAndMigrateMedia");
    }
}
