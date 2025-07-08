# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

⚠️ **IMPORTANT**: After completing any feature requests, always review the "Refactoring Roadmap" section below and suggest the next most important refactor to work on for continued code quality improvement.

## Project Overview

**Eclipse** is a C# WPF plugin and theme for LaunchBox/BigBox that provides a Netflix-style interface for game browsing. It features voice search, random game selection, and media-rich game presentation.

## Build Commands

```bash
# Build the solution
msbuild Eclipse.sln /p:Configuration=Release

# Or use Visual Studio
devenv Eclipse.sln /build Release

# Debug build
msbuild Eclipse.sln /p:Configuration=Debug
```

**Note**: The post-build event automatically copies the compiled plugin to the LaunchBox installation directory at `C:\Users\Adam\Documents\LaunchBox\`.

## Project Structure

- **Eclipse.sln** - Visual Studio solution file
- **Eclipse/Eclipse.csproj** - Main project targeting .NET Framework 4.8
- **Eclipse/Models/** - Data models including `EclipseSettings.cs` (main configuration)
- **Eclipse/Views/** - WPF views and view models (MVVM pattern)
- **Eclipse/Services/** - Business logic services for data access and game management
- **Eclipse/State/** - State machine implementation for UI navigation
- **Eclipse/Converters/** - WPF value converters for data binding

## Architecture

### MVVM + State Machine Pattern
- **Models**: `EclipseSettings`, `GameMatch`, `GameList`, `Option`
- **Views**: `MainWindowView.xaml` (primary UI), `EclipseSettingsView.xaml`
- **ViewModels**: Refactored MVVM structure with separated concerns:
  - `MainWindowViewModel` (~650 lines, down from 1,397 - significantly improved)
  - `VideoControlViewModel` - Video volume control
  - `GameDetailsViewModel` - UI visibility settings for game details
  - `UIStateViewModel` - All UI state flags (IsDisplaying*, IsPlaying*, etc.)
  - `GameOperationsViewModel` - Game operations (play, favorite, rating)
- **Services**: Clean separation of business logic:
  - `GameListManagementService` - Complete list management and operations
- **State Management**: `EclipseStateContext` with states like `LoadingState`, `SelectingGameState`, `VoiceRecognitionState`

### Key Services
- **DataService**: LaunchBox API integration
- **GameBagService**: Game collection management
- **SpeechRecognizer**: Voice search functionality
- **BezelService**: Game bezel/overlay management
- **AttractModeService**: Screensaver mode

### Plugin Architecture
- Implements `IBigBoxThemeElementPlugin` for LaunchBox integration
- Settings accessible via LaunchBox Tools menu
- Outputs to `Eclipse.dll` class library

## Key Dependencies

- **Unbroken.LaunchBox.Plugins.dll** - LaunchBox plugin API (referenced locally)
- **Prism.Core 8.1.97** - MVVM framework and event aggregation
- **Newtonsoft.Json 13.0.1** - JSON serialization for settings
- **WpfAnimatedGif 2.0.0** - Animated GIF support
- **System.Speech** - Voice recognition

## Common Development Patterns

### Settings Management
Settings are stored in `EclipseSettings.cs` and serialized to JSON. Access via:
```csharp
var settings = EclipseSettings.Instance;
```

### State Machine Usage
All UI states inherit from `EclipseState` and are managed by `EclipseStateContext`:
```csharp
context.SetState(new SelectingGameState(context));
```

### Event Aggregation
Uses Prism's event aggregator for decoupled communication:
```csharp
EventAggregatorHelper.Instance.GetEvent<SomeEvent>().Publish(data);
```

### Logging
Errors are logged to `Eclipse.txt` in the LaunchBox directory:
```csharp
LogHelper.LogException(ex, "MethodName");
```

### Modern Property Change Notification
All new ViewModels use modern C# patterns:
```csharp
protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

## Recent Architecture Improvements (2025)

### Major Refactoring Completed
**Massive MainWindowViewModel Cleanup**: Reduced from 1,397 → ~550 lines (847 lines removed, 61% reduction)

**Separated ViewModels Created:**
1. **VideoControlViewModel** (40 lines)
   - Handles video volume control and adjustment
   - Access via: `mainWindowViewModel.VideoControl.VideoVolume`

2. **GameDetailsViewModel** (78 lines)
   - Manages UI visibility settings for game details
   - Properties: `ShowMatchPercent`, `ShowPlatformLogo`, `ShowPlayMode`, etc.
   - Access via: `mainWindowViewModel.GameDetails.ShowStarRating`

3. **UIStateViewModel** (154 lines)  
   - Contains all UI state boolean flags
   - Properties: `IsDisplayingFeature`, `IsPlayingGame`, `IsInitializing`, etc.
   - Access via: `mainWindowViewModel.UIState.IsDisplayingFeature`

4. **GameOperationsViewModel** (86 lines)
   - Handles all game operations and actions
   - Methods: `PlayCurrentGame()`, `FavoriteCurrentGame()`, `RateCurrentGame()`, `SaveRatingCurrentGame()`
   - Access via: `mainWindowViewModel.GameOperations.PlayCurrentGame()`

**Separated Services Created:**
5. **GameListManagementService** (350+ lines)
   - Complete list management and creation functionality  
   - Methods: `CreateGameLists()`, `DoMoreLikeCurrentGame()`, `CycleListForward()`, `CycleListBackward()`
   - Complex filtering and sorting with custom list definitions
   - Access via: `mainWindowViewModel.GameListManagement.CreateGameLists()`

6. **GameListExtensions** (130+ lines)
   - Dynamic LINQ extension methods for filtering and sorting
   - Methods: `OrderBy()`, `OrderByDescending()`, `ThenBy()`, `ApplyDynamicFilter()`
   - Moved from MainWindowViewModel to proper Helpers namespace

7. **FileProcessingService** (100+ lines)
   - Complete async file processing and background setup functionality
   - Methods: `SetupFiles()`, `SetupNextGameFiles()`, `GetProcessingProgress()`, `GetRemainingFileCount()`
   - Maintains priority-based file processing (current list → next list → any remaining files)
   - Access via: `mainWindowViewModel.FileProcessing.SetupFiles(sender, e)`

**PropertyChanged Modernization** (Cross-cutting improvement)
   - Modernized all 55 hardcoded `PropertyChanged("PropertyName")` calls to use `[CallerMemberName]`
   - Added `OnPropertyChanged([CallerMemberName] string propertyName = null)` to all INotifyPropertyChanged classes
   - Files updated: MainWindowViewModel, GameFiles, GameList, GameMatch, Option, GameDetailOptionList
   - Benefits: Compile-time safety, refactoring-friendly property notifications

### XAML Binding Updates Required
**Critical**: When adding new UI state properties, always use the nested ViewModel pattern:
```xml
<!-- CORRECT -->
<Grid Visibility="{Binding UIState.IsDisplayingFeature, Converter={StaticResource BoolToVis}}"/>

<!-- INCORRECT (old pattern) -->
<Grid Visibility="{Binding IsDisplayingFeature, Converter={StaticResource BoolToVis}}"/>
```

### State Machine Integration
All State classes have been updated to use the new ViewModel structure:
```csharp
// CORRECT
eclipseStateContext.MainWindowViewModel.UIState.IsDisplayingFeature = true;
eclipseStateContext.MainWindowViewModel.GameOperations.PlayCurrentGame();
eclipseStateContext.MainWindowViewModel.GameListManagement.DoMoreLikeCurrentGame();
eclipseStateContext.MainWindowViewModel.FileProcessing.SetupFiles(sender, e);

// INCORRECT (old pattern)  
eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = true;
eclipseStateContext.MainWindowViewModel.PlayCurrentGame();
eclipseStateContext.MainWindowViewModel.DoMoreLikeCurrentGame();
```

### Converter Compatibility
MultiValue converters work correctly with the nested ViewModels:
```xml
<MultiBinding Converter="{StaticResource FeatureVideoOffsetConverter}">
    <Binding Path="UIState.IsDisplayingFeature"/>
    <Binding Path="UIState.IsDisplayingMoreInfo"/>
    <Binding ElementName="CurrentListGrid" Path="ActualHeight"/>
</MultiBinding>
```

## File Locations

### Development
- Source code: `Eclipse/` directory
- Output: `Eclipse/bin/Release/Eclipse.dll`

### Deployment (Post-build)
- Plugin: `LaunchBox/Plugins/Eclipse.dll`
- Media assets: `LaunchBox/Plugins/Eclipse/Media/`
- Theme files: `LaunchBox/Themes/Eclipse/`
- Startup themes: `LaunchBox/StartupThemes/Eclipse/`

## Testing

No automated tests are currently present. Manual testing requires:
1. Building the solution
2. Copying to LaunchBox installation
3. Starting BigBox with Eclipse theme selected

## Common Issues

### Legacy Project Issues
- **Null reference exceptions**: Check `Eclipse.txt` log file
- **Missing game data**: Verify LaunchBox database connection
- **Voice recognition not working**: Ensure System.Speech is properly referenced
- **Post-build copy failures**: Check LaunchBox installation path in project settings

### Post-Refactoring Issues (2025)
- **"MS.Internal.NamedObject cannot be cast to Boolean"**: XAML binding is using old direct property path instead of nested ViewModel (e.g., use `UIState.IsDisplayingFeature` not `IsDisplayingFeature`)
- **Video positioning issues**: Check that Grid positioning converters use `UIState.IsDisplayingFeature` bindings
- **Button visibility problems**: Verify feature-related visibility bindings use `UIState.` prefix
- **State machine errors**: Ensure all State classes reference properties via nested ViewModels (e.g., `UIState.PropertyName`)

### Adding New Files to Legacy Project
- **Compilation errors for new classes**: Must manually add `<Compile Include="..."/>` entries to Eclipse.csproj for any new .cs files
- **Modern SDK-style migration recommended**: Current legacy .csproj format requires manual file management

## Refactoring Roadmap

⚠️ **IMPORTANT FOR FUTURE CLAUDE SESSIONS**: Always read this CLAUDE.md file first, review the refactoring todo list below, and suggest the next most important refactor to work on after completing any requested features.

### Priority Refactoring Todo List

#### **High Priority (Major Impact)**

1. **Extract Random Game Selection Logic** 🎯 NEXT PRIORITY
   - **Impact**: ~50 lines removed from MainWindowViewModel (bringing total to ~500 lines, 64% reduction)
   - **Scope**: Extract random game selection algorithms and attract mode logic
   - **Methods to extract**:
     - `DoRandomGame(int randomIndex = -1)` (~30 lines) - Complex algorithm to find game list containing random index
     - `NextAttractModeGame()` (~4 lines) - Random selection from gameBag for attract mode  
     - `AttractModeGame` property (~10 lines) - Property with change notification
     - `private static readonly Random random` field (~1 line)
   - **Dependencies analysis**:
     - **Reads**: `CurrentGameListSet`, `gameBag`, `listCycle` 
     - **Writes**: `CurrentGameList`, `NextGameList`, `AttractModeGame`
     - **Calls**: `CallGameChangeFunction()`, `listCycle.SetCurrentIndex()`, `CurrentGameList.SetGameIndex()`
   - **Usage patterns**:
     - `DoRandomGame()` - Called for truly random selection
     - `DoRandomGame(specificIndex)` - Called 5x in `ResetListsAfterChange()` for targeted navigation
     - `NextAttractModeGame()` - Called by AttractModeService
   - **Benefits**: Testable random logic, cleaner separation, algorithm encapsulation
   - **Estimated effort**: 3-4 hours

#### **Medium Priority (Quality Improvements)**

3. **Refactor State Pattern Implementation**
   - **Impact**: Improved testability, reduced coupling, cleaner architecture
   - **Scope**: Decouple states from UI/ViewModel, implement commands/events
   - **Benefits**: Unit testable, maintainable, extensible state management
   - **Estimated effort**: 3-4 days

4. **Extract Layout/UI Constants ViewModel**
   - **Impact**: ~30 lines removed from MainWindowViewModel
   - **Scope**: `FrontImageMargin`, `SelectedGameDetailsPadding`, button images, star constants
   - **Benefits**: Centralized UI configuration
   - **Estimated effort**: 3-4 hours

5. **Enable Nullable Reference Types**
   - **Impact**: Prevention of null reference exceptions
   - **Scope**: Add `<Nullable>enable</Nullable>` and annotate all nullable properties
   - **Benefits**: Compile-time null safety, better code quality
   - **Estimated effort**: 2-3 days

6. **Extract Navigation/State Coordination**
   - **Impact**: ~40 lines removed from MainWindowViewModel
   - **Scope**: Navigation methods like `DoUp()`, `DoDown()`, `DoLeft()`, `DoRight()` input delegation
   - **Benefits**: Cleaner state management integration
   - **Estimated effort**: 4-5 hours

7. **Improve Error Handling with ILogger**
   - **Impact**: Modern logging throughout codebase
   - **Scope**: Replace `LogHelper` with structured logging using `ILogger<T>`
   - **Benefits**: Better debugging, structured logs, performance
   - **Estimated effort**: 1-2 days

8. **Migrate to SDK-Style Project Format**
    - **Impact**: Modernized build system, automatic file inclusion
    - **Benefits**: Better tooling, simplified project management
    - **Estimated effort**: 3-4 hours

#### **Low Priority (Future Considerations)**

9. **Add Unit Testing Framework**
    - **Impact**: Testable codebase foundation
    - **Benefits**: Regression protection, documentation through tests
    - **Estimated effort**: 1-2 days setup + ongoing

### Refactoring Guidelines

1. **Always create separate ViewModel files** following the established pattern
2. **Update all XAML bindings** to use nested ViewModel paths
3. **Update all State class references** to use new nested properties
4. **Add new files to Eclipse.csproj** manually (legacy project format)
5. **Use modern C# patterns** (`[CallerMemberName]`, nullable reference types)
6. **Test thoroughly** in BigBox after each refactor
7. **Update this todo list** as items are completed

### Current Status
- ✅ **VideoControlViewModel**: Completed
- ✅ **GameDetailsViewModel**: Completed  
- ✅ **UIStateViewModel**: Completed
- ✅ **GameOperationsViewModel**: Completed
- ✅ **GameListManagementService**: Completed
- ✅ **FileProcessingService**: Completed
- ✅ **PropertyChanged Modernization**: Completed (55 hardcoded calls → [CallerMemberName])
- 📋 **MainWindowViewModel**: ~550 lines (down from 1,397) - ~25% reduction potential remaining