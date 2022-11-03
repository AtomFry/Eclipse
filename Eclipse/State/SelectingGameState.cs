﻿using Eclipse.Models;
using Eclipse.Service;
using System;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.State
{
    public class SelectingGameState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public SelectingGameState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.IsDisplayingResults = true;
            eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CycleListForward();
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionPlayState)));
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingOptionsState)));
            return true;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            if (eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex == 0 && !held)
            {
                // first game and going left and not held down, open the options 
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingOptionsState)));
                return true;
            }

            if (eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex != 0)
            {
                eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleBackward();
                eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
                return true;
            }

            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            // todo: testing page to next letter - this works better on page down or after being held so many times
            int currentIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex +
                eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex;

            int nextIndex = currentIndex + 7;

            if (nextIndex > eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetEndIndex)
            {
                nextIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex;
            }

            eclipseStateContext.MainWindowViewModel.DoRandomGame(nextIndex);

            // todo: testing replacing voice search with page down
            // eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(VoiceRecognitionState)));

            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            int currentIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex +
                eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex;

            int nextIndex = currentIndex - 7;

            if (nextIndex < eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex)
            {
                nextIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetEndIndex;
            }

            eclipseStateContext.MainWindowViewModel.DoRandomGame(nextIndex);

            // todo: testing replacing random game with page up
            // eclipseStateContext.MainWindowViewModel.DoRandomGame();
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleForward();

            eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            // if displaying first list 
            if (eclipseStateContext.MainWindowViewModel.listCycle.GetIndexValue(0) == 0)
            {
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(FeatureOptionPlayState))); 
                return true;
            }

            eclipseStateContext.MainWindowViewModel.CycleListBackward();
            return true;
        }
    }
}