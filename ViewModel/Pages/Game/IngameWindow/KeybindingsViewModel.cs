using Myria.Lib.Core.Models.Settings;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class KeybindingsViewModel : BaseViewModel
    {
        private string _windowTitle = string.Empty;
        private string _resetAllLabel = string.Empty;
        private string _tblDescription = string.Empty;
        private string _tblColumnAction = string.Empty;
        private string _tblColumnPrimary = string.Empty;
        private string _tblColumnSecondary = string.Empty;
        [LocalizedKey("pg.settings.keybindings")]
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.keybindings.reset_all")]
        public string ResetAllLabel
        {
            get => _resetAllLabel;
            set { _resetAllLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.keybindings.description")]
        public string TblDescription
        {
            get => _tblDescription;
            set { _tblDescription = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.keybindings.column.action")]
        public string TblColumnAction
        {
            get => _tblColumnAction;
            set { _tblColumnAction = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.keybindings.column.primary")]
        public string TblColumnPrimary
        {
            get => _tblColumnPrimary;
            set { _tblColumnPrimary = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.keybindings.column.secondary")]
        public string TblColumnSecondary
        {
            get => _tblColumnSecondary;
            set { _tblColumnSecondary = value; OnPropertyChanged(); }
        }

        public ObservableCollection<KeybindingRowVm> Rows { get; } = new();

        public ICommand ResetAllCommand { get; }

        public KeybindingsViewModel()
        {
            LocalizationAutoWire.Wire(this);
            var kb = Settings.Current.Keybindings;
            BuildRows(kb);
            ResetAllCommand = new RelayCommand(ResetAll);
        }

        private void BuildRows(KeybindingSettings kb)
        {
            // Movement
            Rows.Add(new KeybindingRowVm("pg.keybindings.move_north",  "W",        () => kb.MoveNorth,  v => kb.MoveNorth  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.move_south",  "S",        () => kb.MoveSouth,  v => kb.MoveSouth  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.move_west",   "A",        () => kb.MoveWest,   v => kb.MoveWest   = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.move_east",   "D",        () => kb.MoveEast,   v => kb.MoveEast   = v));
            // Navigation
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_inventory", "R",      () => kb.OpenInventory, v => kb.OpenInventory = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_character", "C",      () => kb.OpenCharacter, v => kb.OpenCharacter = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_skills",    "T",      () => kb.OpenSkills,    v => kb.OpenSkills    = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_quests",    "J",      () => kb.OpenQuests,    v => kb.OpenQuests    = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_map",       "M",      () => kb.OpenMap,       v => kb.OpenMap       = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.open_settings",  "Escape", () => kb.OpenSettings,  v => kb.OpenSettings  = v));
            // Room actions
            Rows.Add(new KeybindingRowVm("pg.keybindings.start_fight",   "F",        () => kb.StartFight,   v => kb.StartFight   = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.start_gather",  "G",        () => kb.StartGather,  v => kb.StartGather  = v));
            // Combat
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_attack",  "OemCaret", () => kb.FightAttack,  v => kb.FightAttack  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill1",  "D1",       () => kb.FightSkill1,  v => kb.FightSkill1  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill2",  "D2",       () => kb.FightSkill2,  v => kb.FightSkill2  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill3",  "D3",       () => kb.FightSkill3,  v => kb.FightSkill3  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill4",  "D4",       () => kb.FightSkill4,  v => kb.FightSkill4  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill5",  "D5",       () => kb.FightSkill5,  v => kb.FightSkill5  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill6",  "D6",       () => kb.FightSkill6,  v => kb.FightSkill6  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill7",  "D7",       () => kb.FightSkill7,  v => kb.FightSkill7  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill8",  "D8",       () => kb.FightSkill8,  v => kb.FightSkill8  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill9",  "D9",       () => kb.FightSkill9,  v => kb.FightSkill9  = v));
            Rows.Add(new KeybindingRowVm("pg.keybindings.fight_skill10", "D0",       () => kb.FightSkill10, v => kb.FightSkill10 = v));
        }

        private void ResetAll()
        {
            var defaults = new KeybindingSettings();
            Settings.Current.Keybindings = defaults;
            Rows.Clear();
            BuildRows(defaults);
            SettingsService.Save();
        }
    }

    /// <summary>
    /// One row in the keybindings list. Holds current key name and enter-listen mode.
    /// </summary>
    public class KeybindingRowVm : BaseViewModel
    {
        private readonly string _defaultKey;
        private readonly string _labelKey;
        private readonly Func<string> _get;
        private readonly Action<string> _set;

        private bool _isListening;

        public string Label => Localization.T(_labelKey);

        [LocalizedKey("pg.keybindings.listening")]
        public string TblListening { get; set; } = string.Empty;

        [LocalizedKey("pg.keybindings.reset_default")]
        public string TblResetDefault { get; set; } = string.Empty;

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(Label));
        }

        public string KeyName
        {
            get => _get();
            set { _set(value); OnPropertyChanged(); SettingsService.Save(); }
        }

        public bool IsListening
        {
            get => _isListening;
            set { _isListening = value; OnPropertyChanged(); }
        }

        public ICommand ListenCommand { get; }
        public ICommand ResetCommand { get; }

        public KeybindingRowVm(string labelKey, string defaultKey, Func<string> get, Action<string> set)
        {
            _labelKey = labelKey;
            _defaultKey = defaultKey;
            _get = get;
            _set = set;
            ListenCommand = new RelayCommand(() => IsListening = true);
            ResetCommand  = new RelayCommand(() => { KeyName = _defaultKey; IsListening = false; });
        }

        /// <summary>Called by Page_Keybindings when a key is pressed while this row is listening.</summary>
        public void ApplyKey(Key key)
        {
            IsListening = false;
            KeyName = key.ToString();
        }
    }
}
