# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build entire solution
dotnet build Myria.Wpf.sln

# Run the application
dotnet run

# Release build
dotnet build -c Release
```

No test projects or linting configuration exist in this repo.

## Project Structure

Two projects make up this solution:

- **Myria.Wpf** — WPF desktop app (net8.0-windows, WinExe). Contains all UI, ViewModels, and app services.
- **MyriaLib** — Class library (net8.0) referenced by Myria.Wpf. Contains all game entities, core game logic, and data models.

Game data (JSON) lives in `Data/common/` (monsters, items, NPCs, quests, rooms, cities, etc.) and locale strings in `Data/locales/` (EN/DE).

## Architecture

### MVVM

The app uses standard MVVM. Key base classes:

- `ViewModel/BaseLocalizedViewModel.cs` — subscribes to `Localization.LanguageChanged`, auto-refreshes all properties decorated with `[LocalizedKey("key")]` via reflection.
- `ViewModel/BaseViewModel.cs` — inherits from above, adds `SetProperty<T>()` and `OnPropertyChanged()` helpers.
- `Utils/RelayCommand.cs` — `RelayCommand` and `RelayCommand<T>` ICommand implementations with optional `canExecute` predicate.

### Navigation

`Services/Navigation.cs` is a static service managing multiple named `Frame` controls registered from `MainWindow`. Navigation is enum-driven:

- `NavigationFrameType` — identifies which frame to target (Main, Startup, IngameWindow, Game, IngameMenu, etc.)
- `GamePageType` — identifies which in-game page to show (Room, Combat, etc.)
- Key methods: `NavigateMain()`, `NavigateStartup()`, `NavigateIngameWindow()`, `NavigateGamePage()`

### Localization

Properties on ViewModels are auto-populated via `[LocalizedKey("key")]` attribute + `Utils/LocalizationAutoWire.cs`. When `Localization.LanguageChanged` fires, all attributed properties refresh through `INotifyPropertyChanged`. Translations are JSON files under `Data/locales/`.

### Theming

`Services/ThemeManager.cs` switches between light/dark mode by swapping WPF resource dictionaries (`Assets/Light.xaml`, `Assets/Layout.xaml`, `Assets/Icons.xaml`). Applied at startup in `App.xaml.cs` from persisted settings.

### MyriaLib

Contains game entities under `Entities/` (Items, Maps, Monsters, NPCs, Character) and services under `Services/` (builders, formatters, managers). Settings persistence is also handled here via `SettingsService`.

## Current Work (branch: Npcs)

NPC interaction panels are being refactored from `View/UserControls/IngameWindow/` (flat UserControls) into dedicated pages under `View/Pages/Game/IngameWindow/NpcInteraction/` (CraftPanel, DialogPanel, ShopPanel, UpgradePanel). The old UserControl files have been deleted; the new page files are untracked.
