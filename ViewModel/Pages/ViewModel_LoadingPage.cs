using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Myria.Wpf.ViewModel.Pages
{
    public class LoadingStepVm : INotifyPropertyChanged
    {
        private string _status = "...";
        private Brush _statusColor = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x99));

        public string Label { get; set; } = "";

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static readonly Brush ColorPending = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x99));
        public static readonly Brush ColorDone    = new SolidColorBrush(Color.FromRgb(0x55, 0xCC, 0x88));
        public static readonly Brush ColorFailed  = new SolidColorBrush(Color.FromRgb(0xE0, 0x55, 0x55));
    }

    public class ViewModel_LoadingPage : BaseViewModel
    {
        private static readonly Dictionary<string, string> StepLabels = new()
        {
            ["game_status"]   = "Loading game status",
            ["profiles"]      = "Loading character profiles",
            ["items"]         = "Loading items",
            ["monsters"]      = "Loading monsters",
            ["npcs"]          = "Loading NPCs",
            ["rooms"]         = "Loading rooms",
            ["connections"]   = "Connecting world",
            ["day_cycle"]     = "Initializing day cycle",
            ["recipes"]       = "Loading recipes",
            ["jobs"]          = "Loading jobs",
            ["quests"]        = "Loading quests",
            ["skills"]        = "Loading skills",
            ["registries"]    = "Loading registries",
            ["skill_systems"] = "Loading skill systems",
            ["finalize"]      = "Finalizing initialization",
        };

        public ObservableCollection<LoadingStepVm> Steps { get; } = new();

        // ── Dynamic status ────────────────────────────────────────────────────

        private string _statusMessage = "Initializing...";
        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _okButtonVisible;
        public bool OkButtonVisible
        {
            get => _okButtonVisible;
            private set { _okButtonVisible = value; OnPropertyChanged(); }
        }

        private bool _hasWarnings;
        public bool HasWarnings
        {
            get => _hasWarnings;
            private set { _hasWarnings = value; OnPropertyChanged(); }
        }

        private string _warningsSummary = "";
        public string WarningsSummary
        {
            get => _warningsSummary;
            private set { _warningsSummary = value; OnPropertyChanged(); }
        }

        public ICommand OkCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public ViewModel_LoadingPage()
        {
            foreach (var label in StepLabels.Values)
                Steps.Add(new LoadingStepVm { Label = label });

            OkCommand = new RelayCommand(NavigateToStartup);

            _ = RunInitAsync();
        }

        // ── Initialization flow ───────────────────────────────────────────────

        private async Task RunInitAsync()
        {
            var stepKeys = StepLabels.Keys.ToList();
            int nextIndex = 0;

            var progress = new Progress<string>(key =>
            {
                int i = stepKeys.IndexOf(key);
                if (i >= 0 && i < Steps.Count)
                {
                    Steps[i].Status = "Done";
                    Steps[i].StatusColor = LoadingStepVm.ColorDone;
                }
                nextIndex = i + 1;
            });

            try
            {
                await Task.Run(() => GameService.InitializeGame(progress));

                ThrowTestEndInitializationErrorIfRequested();
                ((IProgress<string>)progress).Report("finalize");

                var warnings = CollectWarnings();

                if (warnings.Count == 0)
                {
                    // No warnings — proceed automatically.
                    NavigateToStartup();
                }
                else
                {
                    // Show warnings and wait for the user to acknowledge.
                    StatusMessage = "Initialization complete — some mods have warnings.";
                    WarningsSummary = string.Join(
                        Environment.NewLine + Environment.NewLine,
                        warnings);
                    HasWarnings   = true;
                    OkButtonVisible = true;
                }
            }
            catch (Exception ex)
            {
                int failedIndex = nextIndex < Steps.Count ? nextIndex : Steps.Count - 1;
                Steps[failedIndex].Status      = "Failed";
                Steps[failedIndex].StatusColor = LoadingStepVm.ColorFailed;

                string failedLabel = Steps[failedIndex].Label;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorWindow = new Window_InitError(
                        failedLabel,
                        ApplicationErrorService.FormatException(ex));
                    errorWindow.Show();
                });
            }
        }

        private void NavigateToStartup()
        {
            if (!Navigation.Current.Navigate(Nav.Startup))
                return; // navigation already in progress or failed silently

            ApplicationErrorService.MarkInitializationComplete();
            OkButtonVisible = false;
        }

        // ── Warning collection ────────────────────────────────────────────────

        /// <summary>
        /// Gathers warnings from two sources:
        /// (1) Mods that were enabled but blocked by dependency/version validation.
        /// (2) Extender errors that unloaded a mod during a hook.
        /// Returns one formatted string per problem mod/error.
        /// </summary>
        private static List<string> CollectWarnings()
        {
            var lines = new List<string>();

            // Validation-blocked mods (enabled but not active).
            foreach (var mod in ModLoader.AllMods.Where(m => m.IsEnabled && m.Warnings.Count > 0))
            {
                var bullets = string.Join(
                    Environment.NewLine,
                    mod.Warnings.Select(w => $"  • {w}"));
                lines.Add($"⚠ {mod.Manifest.Name} ({mod.Manifest.Id}):{Environment.NewLine}{bullets}");
            }

            // Extender errors that caused a mod to be unloaded mid-hook.
            foreach (var error in ModLoader.ExtenderErrors.Where(e => e.ModUnloaded))
            {
                var modName = error.Mod?.Manifest.Name
                           ?? error.Mod?.Manifest.Id
                           ?? "Unknown mod";
                lines.Add(
                    $"⚠ {modName} was unloaded during {error.Phase}:{Environment.NewLine}" +
                    $"  {error.Exception?.Message ?? "Unknown error"}");
            }

            return lines;
        }

        // ── Test helpers ──────────────────────────────────────────────────────

        private static void ThrowTestEndInitializationErrorIfRequested()
        {
            if (Environment.GetCommandLineArgs().Any(arg =>
                    string.Equals(arg, "--test-end-init-error", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("This is a test error at the end of initialization.");
        }
    }
}
