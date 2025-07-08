using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eclipse.Models;
using Eclipse.Helpers;
using Eclipse.View;
using System.Runtime.CompilerServices;

namespace Eclipse.Service
{
    public class FileProcessingService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Reference to MainWindowViewModel for accessing game data and state
        private readonly MainWindowViewModel mainWindowViewModel;

        public FileProcessingService(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Main entry point for background file processing. Sets up files for all games in a background thread.
        /// </summary>
        public async void SetupFiles(object sender, DoWorkEventArgs e)
        {
            int? GameFilesCount = mainWindowViewModel.gameFilesBag?.Count;
            int processedCount = 0;

            await Task.Run(async () =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                while (await SetupNextGameFiles())
                {
                    // just to be safe and avoid an infinite loop
                    // check how many times we've been through the loop and stop after we have
                    // processed enough to go through all game files
                    processedCount++;
                    if (processedCount > GameFilesCount)
                    {
                        break;
                    }
                }
            });
        }

        /// <summary>
        /// Processes the next set of game files that need setup. Prioritizes current list, then next list, then any remaining files.
        /// </summary>
        /// <returns>True if more files need processing, false if all files are setup</returns>
        private async Task<bool> SetupNextGameFiles()
        {
            bool moreGameFiles = false;

            try
            {
                await Task.Run(async () =>
                {
                    // setup a game in the current list 
                    if (mainWindowViewModel.CurrentGameList != null && mainWindowViewModel.CurrentGameList.MatchingGames != null)
                    {
                        var currentListQuery = mainWindowViewModel.CurrentGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup);
                        if (currentListQuery.Any())
                        {
                            GameMatch gameMatchCurrentList = currentListQuery.FirstOrDefault();
                            if (gameMatchCurrentList?.GameFiles != null)
                            {
                                moreGameFiles = true;
                                await gameMatchCurrentList.GameFiles.SetupFiles();

                                // If this is the currently displayed game, trigger a UI refresh
                                if (gameMatchCurrentList?.GameFiles?.Game?.Id == mainWindowViewModel.CurrentGameList?.Game1?.Game?.Id)
                                {
                                    mainWindowViewModel.CallGameChangeFunction();
                                }
                            }
                        }
                    }

                    // setup a game in the next list
                    if (mainWindowViewModel.NextGameList != null && mainWindowViewModel.NextGameList.MatchingGames != null)
                    {
                        var nextListQuery = mainWindowViewModel.NextGameList.MatchingGames.Where(g => !g.GameFiles.IsSetup);
                        if (nextListQuery.Any())
                        {
                            GameMatch gameMatchNextList = nextListQuery.FirstOrDefault();
                            if (gameMatchNextList?.GameFiles != null)
                            {
                                moreGameFiles = true;
                                await gameMatchNextList.GameFiles.SetupFiles();
                            }
                        }
                    }

                    // setup any game that still needs to be setup
                    if (mainWindowViewModel.gameFilesBag != null)
                    {
                        var anyGameQuery = mainWindowViewModel.gameFilesBag.Where(gf => !gf.IsSetup);
                        if (anyGameQuery.Any())
                        {
                            GameFiles anyGameFiles = anyGameQuery.FirstOrDefault();
                            if (anyGameFiles != null)
                            {
                                moreGameFiles = true;
                                await anyGameFiles.SetupFiles();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "SetupNextGameFiles");
            }

            return moreGameFiles;
        }

        /// <summary>
        /// Gets the current progress of file processing as a percentage
        /// </summary>
        public double GetProcessingProgress()
        {
            if (mainWindowViewModel.gameFilesBag == null || mainWindowViewModel.gameFilesBag.Count == 0)
                return 100.0;

            int totalFiles = mainWindowViewModel.gameFilesBag.Count;
            int setupFiles = mainWindowViewModel.gameFilesBag.Count(gf => gf.IsSetup);

            return (double)setupFiles / totalFiles * 100.0;
        }

        /// <summary>
        /// Gets the number of files still needing setup
        /// </summary>
        public int GetRemainingFileCount()
        {
            if (mainWindowViewModel.gameFilesBag == null)
                return 0;

            return mainWindowViewModel.gameFilesBag.Count(gf => !gf.IsSetup);
        }
    }
}