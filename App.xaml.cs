using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Models.Settings;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Services;
using Myria.Wpf.Services.Mods;
using Myria.Lib.Core.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using Myria.Wpf.View.Windows;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Myria.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            InstallErrorHandlers();

            try
            {
                RunStartupStep("Testing initialization error", () => ThrowTestInitializationErrorIfRequested(e.Args));
                RegisterViews();

                ModLoader.RegisterExtender(new WpfAssetModExtender());
                ModLoader.RegisterExtender(new GameDataModExtender());
                RunStartupStep("Loading mods", () => ModLoader.Load("Data/Mods"));
                RunStartupStep("Loading settings", SettingsService.Load);
                RunStartupStep("Applying server address", () =>
                {
                    if (!string.IsNullOrWhiteSpace(Settings.Current.ServerAddress))
                        ServerApiService.AuthBaseUrl = ServerApiService.NormalizeAddress(Settings.Current.ServerAddress);
                });
                RunStartupStep("Loading localization", () =>
                {
                    // Wpf-only UI strings (page/panel copy) live separately from the shared
                    // game-content strings in Myria.Lib.Core/Data/locales — registered once,
                    // reapplied automatically on every language (re)load.
                    Myria.Lib.Core.Systems.Localization.RegisterAdditionalLocaleDirectory("Data/locales-wpf");
                    Myria.Lib.Core.Systems.Localization.Load(Settings.Current.LanguageSettings.EffectiveLanguageId);
                });
                RunStartupStep("Applying theme", () =>
                    ThemeManager.Apply(Settings.Current.VisualSettings.DarkMode));
                RunStartupStep("Loading application resources", WpfAssetModExtender.EnsureApplicationAssetDictionaries);

                // Runs before the game itself launches - settings/localization/theme are already
                // loaded above so the window can show localized, themed text. Coordinates across
                // multiple Myria.Wpf.exe instances via UpdateCoordinator so overlapping launches
                // never race the same download (see Data/Misc/update.log history).
                if (Settings.Current.AutoUpdateEnabled)
                {
                    // Default ShutdownMode is OnLastWindowClose - without this, closing the
                    // update window below (before MainWindow exists) would shut the whole app
                    // down instead of continuing startup. Restored right after MainWindow shows.
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    bool isLeader = UpdateCoordinator.TryBecomeLeader();
                    var updateWindow = new UpdateCheckWindow();
                    updateWindow.Show();
                    try
                    {
                        if (isLeader)
                        {
                            var progress = new Progress<UpdateProgress>(updateWindow.Apply);
                            bool launchingInstaller = await UpdateService.CheckForUpdatesAsync(progress);
                            if (launchingInstaller)
                                return; // UpdateService already requested Shutdown()
                        }
                        else
                        {
                            await UpdateCoordinator.WaitForLeaderAsync();
                        }
                    }
                    finally
                    {
                        if (isLeader) UpdateCoordinator.Release();
                        updateWindow.Close();
                    }
                }

                RunStartupStep("Loading race profiles", () => RaceProfile.Load());
                RunStartupStep("Loading class profiles", () => ClassProfile.Load());
                RunStartupStep("Loading rune words", () => RuneWordService.Load());
                RunStartupStep("Loading base runes", () => BaseRuneService.Load());
                RunStartupStep("Loading jobs", () => JobManager.LoadJobs());
                RegisterSessionHooks();

                MainWindow = new MainWindow();
                MainWindow.Show();
                ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            catch (StartupStepException ex)
            {
                ApplicationErrorService.ShowInitializationError(ex.Step, ex);
                Shutdown(-1);
            }
            catch (Exception ex)
            {
                ApplicationErrorService.ShowInitializationError("Application startup", ex);
                Shutdown(-1);
            }
        }

        private static void RunStartupStep(string step, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new StartupStepException(step, ex);
            }
        }

        private static void ThrowTestInitializationErrorIfRequested(string[] args)
        {
            if (args.Any(arg => string.Equals(arg, "--test-init-error", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("This is a test initialization error.");
        }

        private static void RegisterSessionHooks()
        {
            GameService.SessionStarted += _ => DayCycleManager.StartInactivityTimer();
            GameService.SessionStarted += character => ClassManager.ApplyDailyPenalty(character);
            GameService.SessionStarted += character =>
            {
                int bonus = JobManager.GetGatherKnowledgeBonus(character);
                if (bonus > 0)
                    foreach (var room in RoomService.AllRooms.Where(r => r.GatheringSpots.Count > 0))
                        room.AddGatherBonus(bonus);

                DayCycleManager.DayAdvanced += day =>
                {
                    JobManager.ApplyDailyTicks(character, day);
                    int b = JobManager.GetGatherKnowledgeBonus(character);
                    if (b > 0)
                        foreach (var r in RoomService.AllRooms.Where(r => r.GatheringSpots.Count > 0))
                            r.AddGatherBonus(b);
                };
            };
        }

        private void InstallErrorHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ApplicationErrorService.ShowUnhandledError("Unhandled UI error", e.Exception);

            if (!ApplicationErrorService.InitializationComplete)
                Shutdown(-1);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                ApplicationErrorService.ShowUnhandledError("Unhandled application error", ex);
            else
                ApplicationErrorService.ShowUnhandledError(
                    "Unhandled application error",
                    new Exception(e.ExceptionObject?.ToString() ?? "Unknown error"));
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ApplicationErrorService.ShowUnhandledError("Unhandled background task error", e.Exception);
            e.SetObserved();
        }

        private static void RegisterViews()
        {
            var nav = Navigation.Current;

            nav.RegisterView(Nav.Startup,            NavigationFrameType.Main,     () => new Page_StartupMenu());
            nav.RegisterView(Nav.CharacterSelection, NavigationFrameType.Main,     () => new Page_CharacterSelection());
            nav.RegisterView(Nav.Game,               NavigationFrameType.Main,     () => new Page_Game());

            nav.RegisterView(Nav.Login,    NavigationFrameType.Startup, () => new Page_Login());
            nav.RegisterView(Nav.Settings, NavigationFrameType.Startup, () => new Page_Settings());

            nav.RegisterView(Nav.SettingsVisuals,     NavigationFrameType.Settings, () => new Page_SettingsVisuals());
            nav.RegisterView(Nav.SettingsLanguage,    NavigationFrameType.Settings, () => new Page_SettingsLanguage());
            nav.RegisterView(Nav.SettingsKeybindings, NavigationFrameType.Settings, () => new Page_Keybindings());
            nav.RegisterView(Nav.SettingsMods,        NavigationFrameType.Settings, () => new Page_SettingsMods(allowToggle: false));

            nav.RegisterView(Nav.Room, NavigationFrameType.Game, () => new Page_Room());
        }

        private sealed class StartupStepException : Exception
        {
            public StartupStepException(string step, Exception innerException)
                : base($"{step} failed.", innerException)
            {
                Step = step;
            }

            public string Step { get; }
        }
    }

}
