using Eclipse.Diagnostics;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.State
{
    public class LoadingState : EclipseState
    {
        EclipseStateContext EclipseStateContext;

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            EclipseStateContext = eclipseStateContext;

            eclipseStateContext.MainWindowViewModel.IsInitializing = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Initialization_LoadData;
            worker.RunWorkerCompleted += InitializationCompleted;
            worker.RunWorkerAsync();
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            return false;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            return true;
        }

        private void Initialization_LoadData(object sender, DoWorkEventArgs e)
        {
            try
            {
                // create folders that are required by the plugin
                DirectoryInfoHelper.CreateFolders();

                // setup the list of options
                EclipseStateContext.MainWindowViewModel.OptionList = OptionListService.Instance.OptionList;

                // reading these is what triggers the catalog build, so time it here
                Stopwatch catalogStopwatch = Stopwatch.StartNew();
                EclipseStateContext.MainWindowViewModel.gameCatalog = GameCatalog.Instance;
                EclipseStateContext.MainWindowViewModel.gameFilesBag = GameCatalog.Instance.MediaEntries;
                catalogStopwatch.Stop();

                // build the voice index at the same point the voice clones used to be built
                Stopwatch voiceIndexStopwatch = Stopwatch.StartNew();
                VoiceSearchIndex.Instance.Prepare();
                voiceIndexStopwatch.Stop();

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += EclipseStateContext.MainWindowViewModel.SetupFiles;
                worker.RunWorkerAsync();

                // create the voice recognition
                if (EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
                {
                    _ = SpeechRecognizerService.Instance.GetRecognizer();
                }

                // prepare lists of games by different categories
                EclipseStateContext.MainWindowViewModel.GameListSets = new List<GameListSet>();

                // populate the lists
                Stopwatch createGameListsStopwatch = Stopwatch.StartNew();
                EclipseStateContext.MainWindowViewModel.CreateGameLists();
                createGameListsStopwatch.Stop();

                // capture the shape of the index so the refactor stages that replace it can
                // be proved behaviour-preserving by diff - does nothing unless the marker
                // file is present. Remove along with GameIndexDiagnostics when done.
                GameIndexDiagnostics.Capture(EclipseStateContext.MainWindowViewModel,
                                             catalogStopwatch.ElapsedMilliseconds,
                                             voiceIndexStopwatch.ElapsedMilliseconds,
                                             createGameListsStopwatch.ElapsedMilliseconds);

                // get settings and setup default list category type
                EclipseSettings eclipseSettings = EclipseSettingsDataProvider.Instance.EclipseSettings;

                EclipseStateContext.MainWindowViewModel.ResetGameLists(eclipseSettings.DefaultListCategoryType);

                e.Result = EclipseStateContext;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Initializion_loadData");

                DisplayingErrorState displayingErrorState = EclipseStateContext.GetState(typeof(DisplayingErrorState)) as DisplayingErrorState;
                displayingErrorState.ErrorMessage = ex.Message;

                EclipseStateContext.TransitionToState(displayingErrorState);
            }
        }

        private void InitializationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EclipseStateContext.MainWindowViewModel.IsInitializing = false;
            EclipseStateContext.TransitionToState(new SelectingGameState());
        }
    }
}
