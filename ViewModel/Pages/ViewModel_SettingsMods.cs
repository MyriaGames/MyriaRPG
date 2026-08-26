using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Model;
using Myria.Wpf.Services.Mods;
using Myria.Wpf.Utils;
using GameLocalization = Myria.Lib.Core.Systems.Localization;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsMods : BaseViewModel
    {
        private const string ModsDirectory = "Data/Mods";

        /// <summary>
        /// When false the enable/disable toggle on each row is read-only.
        /// Pass false when opening the mod list from within an active game session.
        /// </summary>
        public bool AllowToggle { get; }

        private ModSortMode _sortMode = ModSortMode.LoadOrder;
        private string _statusText = string.Empty;
        private string _windowTitle = string.Empty;
        private string _tblDescription = string.Empty;
        private string _tblSort = string.Empty;
        private string _tblSortLoadOrder = string.Empty;
        private string _tblSortName = string.Empty;
        private string _tblColumnPosition = string.Empty;
        private string _tblColumnLoadOrder = string.Empty;
        private string _tblColumnMod = string.Empty;
        private string _tblColumnType = string.Empty;
        private string _tblColumnActive = string.Empty;
        private string _tblEmpty = string.Empty;
        private string _tblRefresh = string.Empty;
        private string _tblSave = string.Empty;
        private string _tblReset = string.Empty;
        private string _tblMoveUp = string.Empty;
        private string _tblMoveDown = string.Empty;
        public ObservableCollection<ModSettingsRowVm> Mods { get; } = new();

        [LocalizedKey("pg.settings.mods")]
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.description")]
        public string TblDescription
        {
            get => _tblDescription;
            set { _tblDescription = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.sort")]
        public string TblSort
        {
            get => _tblSort;
            set { _tblSort = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.sort.load_order")]
        public string TblSortLoadOrder
        {
            get => _tblSortLoadOrder;
            set { _tblSortLoadOrder = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.sort.name")]
        public string TblSortName
        {
            get => _tblSortName;
            set { _tblSortName = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.column.position")]
        public string TblColumnPosition
        {
            get => _tblColumnPosition;
            set { _tblColumnPosition = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.column.load_order")]
        public string TblColumnLoadOrder
        {
            get => _tblColumnLoadOrder;
            set { _tblColumnLoadOrder = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.column.mod")]
        public string TblColumnMod
        {
            get => _tblColumnMod;
            set { _tblColumnMod = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.column.type")]
        public string TblColumnType
        {
            get => _tblColumnType;
            set { _tblColumnType = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.column.active")]
        public string TblColumnActive
        {
            get => _tblColumnActive;
            set { _tblColumnActive = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.empty")]
        public string TblEmpty
        {
            get => _tblEmpty;
            set { _tblEmpty = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.refresh")]
        public string TblRefresh
        {
            get => _tblRefresh;
            set { _tblRefresh = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.save")]
        public string TblSave
        {
            get => _tblSave;
            set { _tblSave = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.reset")]
        public string TblReset
        {
            get => _tblReset;
            set { _tblReset = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.move_up")]
        public string TblMoveUp
        {
            get => _tblMoveUp;
            set { _tblMoveUp = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.move_down")]
        public string TblMoveDown
        {
            get => _tblMoveDown;
            set { _tblMoveDown = value; OnPropertyChanged(); }
        }

        public bool SortByLoadOrder
        {
            get => _sortMode == ModSortMode.LoadOrder;
            set
            {
                if (!value || _sortMode == ModSortMode.LoadOrder) return;
                _sortMode = ModSortMode.LoadOrder;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SortByName));
                RefreshRows();
            }
        }

        public bool SortByName
        {
            get => _sortMode == ModSortMode.Name;
            set
            {
                if (!value || _sortMode == ModSortMode.Name) return;
                _sortMode = ModSortMode.Name;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SortByLoadOrder));
                RefreshRows();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public Visibility EmptyVisibility => Mods.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ModsVisibility => Mods.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        private ModDetailsVm? _selectedDetails;
        public ModDetailsVm? SelectedDetails
        {
            get => _selectedDetails;
            private set
            {
                SetProperty(ref _selectedDetails, value);
                OnPropertyChanged(nameof(HasSelectedDetails));
            }
        }
        public bool HasSelectedDetails => _selectedDetails != null;

        public ICommand RefreshCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand OpenModDetailsCommand { get; }

        public ViewModel_SettingsMods(bool allowToggle = true)
        {
            AllowToggle = allowToggle;
            LocalizationAutoWire.Wire(this);
            RefreshCommand = new RelayCommand(RefreshMods);
            MoveUpCommand = new RelayCommand<ModSettingsRowVm>(row => Move(row, -1));
            MoveDownCommand = new RelayCommand<ModSettingsRowVm>(row => Move(row, 1));
            OpenModDetailsCommand = new RelayCommand<ModSettingsRowVm?>(OpenDetails);
            RefreshRows();
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            UpdateStatusText();
            foreach (var mod in Mods)
                mod.RefreshLanguage();
        }

        private void RefreshMods()
        {
            ModLoader.Load(ModsDirectory);
            RefreshRows();
        }

        private void RefreshRows()
        {
            var indexedMods = ModLoader.AllMods
                .Select((mod, index) => new IndexedMod(mod, index))
                .ToList();

            IEnumerable<IndexedMod> ordered = _sortMode == ModSortMode.Name
                ? indexedMods
                    .OrderBy(m => m.Mod.Manifest.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(m => m.Mod.Manifest.Id, StringComparer.CurrentCultureIgnoreCase)
                : indexedMods;

            Mods.Clear();
            foreach (var item in ordered)
            {
                Mods.Add(new ModSettingsRowVm(
                    item.Mod,
                    item.LoadPosition + 1,
                    _sortMode == ModSortMode.LoadOrder && item.LoadPosition > 0,
                    _sortMode == ModSortMode.LoadOrder && item.LoadPosition < indexedMods.Count - 1,
                    SaveManualLoadOrder,
                    SaveEnabled,
                    AllowToggle));
            }

            OnPropertyChanged(nameof(EmptyVisibility));
            OnPropertyChanged(nameof(ModsVisibility));
            UpdateStatusText();
        }

        private void Move(ModSettingsRowVm? row, int offset)
        {
            if (row == null || _sortMode != ModSortMode.LoadOrder) return;

            int index = Mods.IndexOf(row);
            int target = index + offset;
            if (index < 0 || target < 0 || target >= Mods.Count) return;

            Mods.Move(index, target);
            PersistVisibleOrder();
            RefreshMods();
        }

        private void PersistVisibleOrder()
        {
            for (int i = 0; i < Mods.Count; i++)
            {
                int loadOrder = (i + 1) * 10;
                Mods[i].SetLoadOrderFromOwner(loadOrder);
                ModManifestStore.WriteProperty(Mods[i].LoadedMod, "loadOrder", JsonValue.Create(loadOrder));
            }
        }

        private void SaveManualLoadOrder(ModSettingsRowVm row, int loadOrder)
        {
            row.LoadedMod.Manifest.LoadOrder = loadOrder;
            ModManifestStore.WriteProperty(row.LoadedMod, "loadOrder", JsonValue.Create(loadOrder));
            RefreshMods();
        }

        private void SaveEnabled(ModSettingsRowVm row, bool enabled)
        {
            row.LoadedMod.Manifest.Enabled = enabled;
            ModManifestStore.WriteProperty(row.LoadedMod, "enabled", JsonValue.Create(enabled));
            RefreshMods();
        }

        private void OpenDetails(ModSettingsRowVm? row)
        {
            if (row == null) return;
            SelectedDetails = new ModDetailsVm(
                row.LoadedMod,
                closeCallback:  () => SelectedDetails = null,
                reloadCallback: RefreshMods);
        }

        private void UpdateStatusText()
        {
            StatusText = string.Format(GameLocalization.T("pg.mods.status.count"), Mods.Count);
        }

        private enum ModSortMode
        {
            LoadOrder,
            Name
        }

        private sealed record IndexedMod(LoadedMod Mod, int LoadPosition);
    }

    public class ModSettingsRowVm : BaseViewModel
    {
        private readonly Action<ModSettingsRowVm, int> _loadOrderChanged;
        private readonly Action<ModSettingsRowVm, bool> _enabledChanged;
        private readonly bool _allowToggle;
        private bool _suppressLoadOrderSave;
        private bool _suppressEnabledSave;
        private int _loadOrder;
        private bool _isEnabled;

        internal LoadedMod LoadedMod { get; }

        public int LoadPosition { get; }
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string Description { get; }
        public string FolderName { get; }
        public int OverriddenFileCount { get; }
        public bool IsVisualOnly { get; }
        public bool CanMoveUp { get; }
        public bool CanMoveDown { get; }

        /// <summary>True when the mod has at least one warning that is NOT marked as optional.</summary>
        public bool HasBlockingWarnings => LoadedMod.Warnings.Any(w =>
            !w.StartsWith("[Optional]", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// False when either the page is in read-only mode (e.g. opened in-game) OR the mod
        /// has blocking validation failures and cannot be activated.
        /// </summary>
        public bool CanToggle => _allowToggle && !HasBlockingWarnings;

        public bool HasWarnings => LoadedMod.Warnings.Count > 0;
        public string WarningText => string.Join("\n", LoadedMod.Warnings);

        public int LoadOrder
        {
            get => _loadOrder;
            set
            {
                if (!SetProperty(ref _loadOrder, value)) return;
                if (!_suppressLoadOrderSave)
                    _loadOrderChanged(this, value);
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetProperty(ref _isEnabled, value)) return;
                OnPropertyChanged(nameof(ActiveLabel));
                if (!_suppressEnabledSave)
                    _enabledChanged(this, value);
            }
        }

        public string TypeLabel => GameLocalization.T(IsVisualOnly ? "pg.mods.type.visual" : "pg.mods.type.gameplay");
        public string ActiveLabel => GameLocalization.T(IsEnabled ? "pg.mods.active.on" : "pg.mods.active.off");
        public string VersionText => string.IsNullOrWhiteSpace(Version) ? "" : $"v{Version}";
        public string AuthorText => string.IsNullOrWhiteSpace(Author) ? FolderName : Author;

        public ModSettingsRowVm(
            LoadedMod mod,
            int loadPosition,
            bool canMoveUp,
            bool canMoveDown,
            Action<ModSettingsRowVm, int> loadOrderChanged,
            Action<ModSettingsRowVm, bool> enabledChanged,
            bool allowToggle = true)
        {
            LoadedMod = mod;
            LoadPosition = loadPosition;
            Id = mod.Manifest.Id;
            Name = string.IsNullOrWhiteSpace(mod.Manifest.Name) ? mod.Manifest.Id : mod.Manifest.Name;
            Version = mod.Manifest.Version;
            Author = mod.Manifest.Author;
            Description = mod.Manifest.Description;
            FolderName = Path.GetFileName(mod.Directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            OverriddenFileCount = mod.OverriddenFiles.Count;
            IsVisualOnly = mod.IsVisualOnly;
            CanMoveUp = canMoveUp;
            CanMoveDown = canMoveDown;
            _allowToggle = allowToggle;
            _loadOrder = mod.Manifest.LoadOrder;
            _isEnabled = mod.IsEnabled;
            _loadOrderChanged = loadOrderChanged;
            _enabledChanged = enabledChanged;
        }

        public void SetLoadOrderFromOwner(int loadOrder)
        {
            _suppressLoadOrderSave = true;
            LoadOrder = loadOrder;
            _suppressLoadOrderSave = false;
        }

        public void SetEnabledFromOwner(bool enabled)
        {
            _suppressEnabledSave = true;
            IsEnabled = enabled;
            _suppressEnabledSave = false;
        }

        public void RefreshLanguage()
        {
            OnPropertyChanged(nameof(TypeLabel));
            OnPropertyChanged(nameof(ActiveLabel));
        }
    }
}
