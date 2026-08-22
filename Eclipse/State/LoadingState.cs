using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

                EclipseStateContext.MainWindowViewModel.gameCatalog = GameCatalog.Instance;
                EclipseStateContext.MainWindowViewModel.gameFilesBag = GameCatalog.Instance.MediaEntries;

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += EclipseStateContext.MainWindowViewModel.SetupFiles;
                worker.RunWorkerAsync();

                // the phrase index and the recogniser build in the background - they are
                // only needed if the user actually starts a voice search, so they no longer
                // hold up the first screen. Voice search reports itself as still preparing
                // until this finishes.
                SpeechRecognizerService.Instance.PrepareInBackground();

                // prepare lists of games by different categories
                EclipseStateContext.MainWindowViewModel.GameListSets = new List<GameListSet>();

                // populate the lists
                EclipseStateContext.MainWindowViewModel.CreateGameLists();

                // GameFieldEnum and the accessor table are two hand-written lists of the same
                // fields, so they can drift apart. A field with no accessor throws the moment a
                // user's custom list happens to use it, which could be long after the mistake -
                // say so at startup instead. Silent unless something is actually missing.
                List<GameFieldEnum> unmappedFields = GameFields.UnmappedFields().ToList();
                if (unmappedFields.Count > 0)
                {
                    LogHelper.Log($"Custom lists cannot filter or sort on these fields - no accessor is defined: {string.Join(", ", unmappedFields)}");
                }

                // record what was built, if the dump setting is on - the baseline that the
                // list construction refactor is measured against. Does nothing by default.
                GameListDump.WriteIfEnabled(EclipseStateContext.MainWindowViewModel.GameListSets, "startup");

                // and every field/operator combination a custom list can use, which this user's
                // own CustomLists.json will only cover a corner of
                CustomListQueryProbe.WriteIfEnabled(EclipseStateContext.MainWindowViewModel.gameCatalog);

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
