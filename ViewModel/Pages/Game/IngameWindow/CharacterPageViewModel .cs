using Myria.Lib.Core.Entities;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class CharacterPageViewModel : BaseViewModel
    {
        private string tbl_Level;
        private string tbl_Base;
        private string tbl_STR;
        private string tbl_DEX;
        private string tbl_END;
        private string tbl_INT;
        private string tbl_SPR;
        private string tblUnspent;
        private string tbl_Derived;
        private string tbl_HP;
        private string tbl_MP;
        private string tbl_ATK;
        private string tbl_DEF;
        private string tbl_MATK;
        private string tbl_MDEF;
        private string tbl_Aim;
        private string tbl_Crit;
        private string tbl_Evasion;
        private string tbl_Block;
        private string _strTooltip;
        private string _dexTooltip;
        private string _endTooltip;
        private string _intTooltip;
        private string _sprTooltip;
        private string _charClass;
        private Character _character;

        [LocalizedKey("pg.character.info.level")]
        public string TblLevel
        {
            get { return tbl_Level; }
            private set
            {
                tbl_Level = value + " ";
                OnPropertyChanged(nameof(TblLevel));
            }

        }
        [LocalizedKey("pg.character.info.base")]
        public string TblBase
        {
            get { return tbl_Base; }
            private set
            {
                tbl_Base = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.str")]
        public string TblSTR
        {
            get { return tbl_STR; }
            private set
            {
                tbl_STR = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.dex")]
        public string TblDEX
        {
            get { return tbl_DEX; }
            private set
            {
                tbl_DEX = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.end")]
        public string TblEND
        {
            get { return tbl_END; }
            private set
            {
                tbl_END = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.int")]
        public string TblINT
        {
            get { return tbl_INT; }
            private set
            {
                tbl_INT = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.spr")]
        public string TblSPR
        {
            get { return tbl_SPR; }
            private set
            {
                tbl_SPR = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.hp")]
        public string TblHP
        {
            get { return tbl_HP; }
            private set
            {
                tbl_HP = value + " ";
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.mp")]
        public string TblMP
        {
            get { return tbl_MP; }
            private set
            {
                tbl_MP = value + " ";
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.atk")]
        public string TblATK
        {
            get { return tbl_ATK; }
            private set
            {
                tbl_ATK = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.def")]
        public string TblDEF
        {
            get { return tbl_DEF; }
            private set
            {
                tbl_DEF = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.matk")]
        public string TblMATK
        {
            get { return tbl_MATK; }
            private set
            {
                tbl_MATK = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.mdef")]
        public string TblMDEF
        {
            get { return tbl_MDEF; }
            private set
            {
                tbl_MDEF = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.aim")]
        public string TblAim
        {
            get { return tbl_Aim; }
            private set
            {
                tbl_Aim = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.eva")]
        public string TblEvasion
        {
            get { return tbl_Evasion; }
            private set
            {
                tbl_Evasion = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.crit")]
        public string TblCrit
        {
            get { return tbl_Crit; }
            private set
            {
                tbl_Crit = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.block")]
        public string TblBlock
        {
            get { return tbl_Block; }
            private set
            {
                tbl_Block = value;
                OnPropertyChanged();
            }

        }

        [LocalizedKey("pg.character.tooltip.str")]
        public string StrTooltip
        {
            get { return _strTooltip; }
            private set { _strTooltip = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.character.tooltip.dex")]
        public string DexTooltip
        {
            get { return _dexTooltip; }
            private set { _dexTooltip = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.character.tooltip.end")]
        public string EndTooltip
        {
            get { return _endTooltip; }
            private set { _endTooltip = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.character.tooltip.int")]
        public string IntTooltip
        {
            get { return _intTooltip; }
            private set { _intTooltip = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.character.tooltip.spr")]
        public string SprTooltip
        {
            get { return _sprTooltip; }
            private set { _sprTooltip = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.character.info.unspent")]
        public string TblUnspent
        {
            get { return tblUnspent; }
            private set
            {
                tblUnspent = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.derived")]
        public string TblDerived
        {
            get { return tbl_Derived; }
            private set
            {
                tbl_Derived = value;
                OnPropertyChanged();
            }

        }
        public string CharClass
        {
            get => _charClass;
            set
            {
                _charClass = value;
                OnPropertyChanged();
            }

        }
        public string CharacterName { get; set; }
        public int Level { get; set; }
        public long XpCurrent { get; set; }
        public long XpToNextLevel { get; set; }  // total needed for current->next

        public string XpProgressText => $"{XpCurrent:N0} / {XpToNextLevel:N0}  ({XpPercent:0}%)";
        public double XpPercent => Math.Clamp(XpToNextLevel > 0 ? (XpCurrent * 100.0) / XpToNextLevel : 0, 0, 100);

        public string ClassLevelLabel => Localization.T("pg.class.level", ClassManager.GetClassLevel(_character, _character.Class).ToString());
        public double ClassXpFraction => ClassXpService.GetProgressFraction(ClassManager.GetClassXp(_character, _character.Class));
        public string ClassXpText => ClassXpService.FormatProgress(ClassManager.GetClassXp(_character, _character.Class));

        public BaseStatsVm Base { get; private set; }
        public DerivedStatsVm Derived { get; private set; }
        public ICommand IncreaseStatCommand { get; }
        private readonly RelayCommand<string> _decreaseStatCommand;
        public ICommand DecreaseStatCommand => _decreaseStatCommand;

        // -- Tab navigation ----------------------------------------------------

        public enum CharacterTab { Overview, Skills, Jobs, Quests }

        private CharacterTab _selectedTab = CharacterTab.Overview;
        public CharacterTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOverviewTab));
                OnPropertyChanged(nameof(IsSkillsTab));
                OnPropertyChanged(nameof(IsJobsTab));
                OnPropertyChanged(nameof(IsQuestsTab));
                OnPropertyChanged(nameof(IsSubPageTab));
            }
        }

        public bool IsOverviewTab  => SelectedTab == CharacterTab.Overview;
        public bool IsSkillsTab    => SelectedTab == CharacterTab.Skills;
        public bool IsJobsTab      => SelectedTab == CharacterTab.Jobs;
        public bool IsQuestsTab    => SelectedTab == CharacterTab.Quests;
        public bool IsSubPageTab   => SelectedTab != CharacterTab.Overview;

        public ICommand SelectOverviewTabCommand { get; }
        public ICommand SelectSkillsTabCommand   { get; }
        public ICommand SelectJobsTabCommand     { get; }
        public ICommand SelectQuestsTabCommand   { get; }

        // -- Window title ------------------------------------------------------

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }
        private string _windowTitle = Localization.T("app.general.UI.character");

        public CharacterPageViewModel(Character character)
        {
            _character = character;
            CharacterName = character.Name;
            Level = character.Level;
            XpCurrent = character.Experience;
            XpToNextLevel = character.ExpForNextLvl;
            CharClass = Localization.T($"class.{character.Class}");

            RefreshStats();

            IncreaseStatCommand      = new RelayCommand<string>(IncreaseStat);
            _decreaseStatCommand     = new RelayCommand<string>(DecreaseStat, CanDecreaseStat);
            SelectOverviewTabCommand = new RelayCommand(() => SelectedTab = CharacterTab.Overview);
            SelectSkillsTabCommand   = new RelayCommand(() => SelectedTab = CharacterTab.Skills);
            SelectJobsTabCommand     = new RelayCommand(() => SelectedTab = CharacterTab.Jobs);
            SelectQuestsTabCommand   = new RelayCommand(() => SelectedTab = CharacterTab.Quests);

            // If a stat point got allocated while briefly disconnected, SyncStatsToServer's
            // IsConnected guard silently drops that push - catch it up as soon as the connection
            // comes back, instead of leaving the server's session character (and therefore
            // MaxHealth/MaxMana) stale until the player happens to touch stat allocation again.
            GameHubService.HubConnected += SyncStatsToServer;
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            CharClass = Localization.T($"class.{_character.Class}");
        }

        private void RefreshStats()
        {
            Base = new BaseStatsVm(
                _character.Stats.Strength, _character.Stats.StrengthAdded,
                _character.Stats.Dexterity, _character.Stats.DexterityAdded,
                _character.Stats.Endurance, _character.Stats.EnduranceAdded,
                _character.Stats.Intelligence, _character.Stats.IntelligenceAdded,
                _character.Stats.Spirit, _character.Stats.SpiritAdded,
                _character.Stats.UnusedPoints);

            Derived = new DerivedStatsVm(_character);

            OnPropertyChanged(nameof(Base));
            OnPropertyChanged(nameof(Derived));
            _decreaseStatCommand?.RaiseCanExecuteChanged();
        }

        private bool CanDecreaseStat(string? statKey) => statKey switch
        {
            "STR" => _character.Stats.StrengthAdded > 0,
            "DEX" => _character.Stats.DexterityAdded > 0,
            "END" => _character.Stats.EnduranceAdded > 0,
            "INT" => _character.Stats.IntelligenceAdded > 0,
            "SPR" => _character.Stats.SpiritAdded > 0,
            _     => false
        };

        private void IncreaseStat(string? statKey)
        {
            if (_character.Stats.UnusedPoints <= 0 || statKey is null) return;

            switch (statKey)
            {
                case "STR": _character.Stats.StrengthAdded++; break;
                case "DEX": _character.Stats.DexterityAdded++; break;
                case "END": _character.Stats.EnduranceAdded++; break;
                case "INT": _character.Stats.IntelligenceAdded++; break;
                case "SPR": _character.Stats.SpiritAdded++; break;
                default: return;
            }
            _character.Stats.UnusedPoints--;
            RefreshStats();
            SyncStatsToServer();
        }

        private void DecreaseStat(string? statKey)
        {
            if (statKey is null) return;

            switch (statKey)
            {
                case "STR":
                    if (_character.Stats.StrengthAdded > 0) { _character.Stats.StrengthAdded--; _character.Stats.UnusedPoints++; }
                    break;
                case "DEX":
                    if (_character.Stats.DexterityAdded > 0) { _character.Stats.DexterityAdded--; _character.Stats.UnusedPoints++; }
                    break;
                case "END":
                    if (_character.Stats.EnduranceAdded > 0) { _character.Stats.EnduranceAdded--; _character.Stats.UnusedPoints++; }
                    break;
                case "INT":
                    if (_character.Stats.IntelligenceAdded > 0) { _character.Stats.IntelligenceAdded--; _character.Stats.UnusedPoints++; }
                    break;
                case "SPR":
                    if (_character.Stats.SpiritAdded > 0) { _character.Stats.SpiritAdded--; _character.Stats.UnusedPoints++; }
                    break;
            }
            RefreshStats();
            SyncStatsToServer();
        }

        // Keeps the server's session character (used by Heal, combat, etc. to compute
        // MaxHealth/MaxMana) from silently going stale relative to whatever the player has
        // allocated since logging in - see GameHub.SyncStatAllocation.
        private void SyncStatsToServer()
        {
            if (!GameHubService.IsConnected) return;
            _ = GameHubService.SyncStatAllocationAsync(
                _character.Stats.StrengthAdded, _character.Stats.DexterityAdded, _character.Stats.EnduranceAdded,
                _character.Stats.IntelligenceAdded, _character.Stats.SpiritAdded, _character.Stats.UnusedPoints);
        }
    }

    public record BaseStatsVm(
        int STR, int STR_Added,
        int DEX, int DEX_Added,
        int END, int END_Added,
        int INT, int INT_Added,
        int SPR, int SPR_Added,
        int Unspent)
    {
        public string STR_Display => Fmt(STR, STR_Added);
        public string DEX_Display => Fmt(DEX, DEX_Added);
        public string END_Display => Fmt(END, END_Added);
        public string INT_Display => Fmt(INT, INT_Added);
        public string SPR_Display => Fmt(SPR, SPR_Added);
        public bool HasUnspent => Unspent > 0;

        private static string Fmt(int @base, int added)
        {
            int total = @base + added;
            if (added > 0) return $"{total} (+{added})";
            return $"{@base}";
        }
    }

    public class DerivedStatsVm
    {
        public string HP { get; }
        public string MaxHP { get; }
        public string MP { get; }
        public string MaxMP { get; }

        public string ATK { get; }
        public string DEF { get; }
        public string MATK { get; }
        public string MDEF { get; }
        public string Aim { get; }
        public string Evasion { get; }

        public double Crit { get; }
        public double Block { get; }

        private readonly int _gearHP,  _gearMP;
        private readonly int _gearATK, _gearDEF, _gearMATK, _gearMDEF, _gearAim, _gearEva;
        private readonly int _statHP,  _statMP;
        private readonly int _statATK, _statDEF, _statMATK, _statMDEF, _statAim, _statEva;

        public string CritPercent  => $"{Crit  * 100:0.#}%";
        public string BlockPercent => $"{Block * 100:0.#}%";

        public string Crit_Hint  => Crit  > 0 ? "(from gear)" : "";
        public string Block_Hint => Block > 0 ? "(from gear)" : "";

        public string HP_Hint   => Hint(_statHP,   _gearHP);
        public string MP_Hint   => Hint(_statMP,   _gearMP);
        public string ATK_Hint  => Hint(_statATK,  _gearATK);
        public string DEF_Hint  => Hint(_statDEF,  _gearDEF);
        public string MATK_Hint => Hint(_statMATK, _gearMATK);
        public string MDEF_Hint => Hint(_statMDEF, _gearMDEF);
        public string Aim_Hint  => Hint(_statAim,  _gearAim);
        public string Eva_Hint  => Hint(_statEva,  _gearEva);

        public int HPValue  { get; }
        public int MaxHPValue { get; }
        public int MPValue  { get; }
        public int MaxMPValue { get; }

        public DerivedStatsVm(Character character)
        {
            HP    = character.CurrentHealth.ToString();
            MaxHP = character.MaxHealth.ToString();
            MP    = character.CurrentMana.ToString();
            MaxMP = character.MaxMana.ToString();

            HPValue    = character.CurrentHealth;
            MaxHPValue = character.MaxHealth;
            MPValue    = character.CurrentMana;
            MaxMPValue = character.MaxMana;

            ATK     = character.TotalPhysicalAttack.ToString();
            DEF     = character.TotalPhysicalDefense.ToString();
            MATK    = character.TotalMagicAttack.ToString();
            MDEF    = character.TotalMagicDefense.ToString();
            Aim     = character.TotalAim.ToString();
            Evasion = character.TotalEvasion.ToString();

            Crit  = character.CritChance;
            Block = character.BlockChance;

            _gearHP   = character.GetBonusFromGear(g => g.BonusHP);
            _gearMP   = character.GetBonusFromGear(g => g.BonusMP);
            _gearATK  = character.GetBonusFromGear(g => g.BonusATK);
            _gearDEF  = character.GetBonusFromGear(g => g.BonusDEF);
            _gearMATK = character.GetBonusFromGear(g => g.BonusMATK);
            _gearMDEF = character.GetBonusFromGear(g => g.BonusMDEF);
            _gearAim  = character.GetBonusFromGear(g => g.BonusAim);
            _gearEva  = character.GetBonusFromGear(g => g.BonusEvasion);

            _statHP   = character.Stats.GetAddedStatBonus(DerivedStatType.MaxHealth);
            _statMP   = character.Stats.GetAddedStatBonus(DerivedStatType.MaxMana);
            _statATK  = character.Stats.GetAddedStatBonus(DerivedStatType.PhysicalAttack);
            _statDEF  = character.Stats.GetAddedStatBonus(DerivedStatType.PhysicalDefense);
            _statMATK = character.Stats.GetAddedStatBonus(DerivedStatType.MagicAttack);
            _statMDEF = character.Stats.GetAddedStatBonus(DerivedStatType.MagicDefense);
            _statAim  = character.Stats.GetAddedStatBonus(DerivedStatType.HitChance);
            _statEva  = character.Stats.GetAddedStatBonus(DerivedStatType.DodgeChance);
        }

        private static string Hint(int fromStats, int fromGear)
        {
            if (fromStats > 0 && fromGear > 0) return $"(+{fromStats} stats, +{fromGear} gear)";
            if (fromStats > 0) return $"(+{fromStats} stats)";
            if (fromGear  > 0) return $"(+{fromGear} gear)";
            return "";
        }
    }
}
