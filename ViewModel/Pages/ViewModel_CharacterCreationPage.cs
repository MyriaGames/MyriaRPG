using Myria.Lib.Core.Entities;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    class ViewModel_CharacterCreationPage : BaseViewModel
    {
        private string _tblName = string.Empty;
        private string _tblSubtitle = string.Empty;
        private string _tblRace = string.Empty;
        private string _tblClass = string.Empty;
        private string _tblShowName = string.Empty;
        private string _tblShowRace = string.Empty;
        private string _tblShowClass = string.Empty;
        private string _tblPreview = string.Empty;
        private string _tblTitle = string.Empty;
        private string _btnBack = string.Empty;
        private string _btnCreate = string.Empty;

        [LocalizedKey("app.general.UI.name")]
        public string TblName
        {
            get => _tblName;
            private set { _tblName = value; OnPropertyChanged(nameof(TblName)); }
        }
        [LocalizedKey("app.general.UI.name")]
        public string TblShowName
        {
            get => _tblShowName;
            private set { _tblShowName = value + ": "; OnPropertyChanged(nameof(TblShowName)); }
        }

        [LocalizedKey("pg.character.create.race")]
        public string TblRace
        {
            get => _tblRace;
            private set { _tblRace = value; OnPropertyChanged(nameof(TblRace)); }
        }

        [LocalizedKey("pg.character.create.race")]
        public string TblShowRace
        {
            get => _tblShowRace;
            private set { _tblShowRace = value + ": "; OnPropertyChanged(nameof(TblShowRace)); }
        }

        [LocalizedKey("pg.character.create.class")]
        public string TblClass
        {
            get => _tblClass;
            private set { _tblClass = value; OnPropertyChanged(nameof(TblClass)); }
        }

        [LocalizedKey("pg.character.create.class")]
        public string TblShowClass
        {
            get => _tblShowClass;
            private set { _tblShowClass = value + ": "; OnPropertyChanged(nameof(TblShowClass)); }
        }

        [LocalizedKey("pg.character.create.preview")]
        public string TblPreview
        {
            get => _tblPreview;
            private set { _tblPreview = value; OnPropertyChanged(nameof(TblPreview)); }
        }

        [LocalizedKey("pg.character.create.title")]
        public string TblTitle
        {
            get => _tblTitle;
            private set { _tblTitle = value; OnPropertyChanged(nameof(TblTitle)); }
        }

        [LocalizedKey("pg.character.create.subtitle")]
        public string TblSubtitle
        {
            get => _tblSubtitle;
            set { _tblSubtitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.back")]
        public string BtnBack
        {
            get => _btnBack;
            private set { _btnBack = value; OnPropertyChanged(nameof(BtnBack)); }
        }

        [LocalizedKey("app.general.UI.create")]
        public string BtnCreate
        {
            get => _btnCreate;
            private set { _btnCreate = value; OnPropertyChanged(nameof(BtnCreate)); }
        }

        private string _characterName = "";
        public string CharacterName
        {
            get => _characterName;
            set { _characterName = value; OnPropertyChanged(); Revalidate(); }
        }

        private string _validationText = "";
        public string ValidationText
        {
            get => _validationText;
            set { _validationText = value; OnPropertyChanged(); }
        }

        private bool _canCreate;
        public bool CanCreate
        {
            get => _canCreate;
            set { _canCreate = value; OnPropertyChanged(); }
        }

        // ── Multiplayer / race picker ─────────────────────────────────────────────

        public bool IsMultiplayer => ServerApiService.Token is not null;

        public ObservableCollection<RaceOptionVm> Races { get; } = new();

        private RaceOptionVm? _selectedRace;
        public RaceOptionVm? SelectedRace
        {
            get => _selectedRace;
            private set
            {
                _selectedRace = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewRaceName));
                OnPropertyChanged(nameof(ShowStartingStats));
                OnPropertyChanged(nameof(PreviewStatLine1));
                OnPropertyChanged(nameof(PreviewStatLine2));
                OnPropertyChanged(nameof(PreviewHpMpLine));
                Revalidate();
            }
        }

        private string EffectiveRace => _selectedRace?.Race ?? CharacterRace.Myralu;
        private RaceProfile? EffectiveProfile => RaceProfile.All.TryGetValue(EffectiveRace, out var p) ? p : null;

        public string PreviewRaceName => !IsMultiplayer || _selectedRace != null ? EffectiveRace : "";

        // ── Class picker ──────────────────────────────────────────────────────────

        public ObservableCollection<ClassOptionVm> Classes { get; } = new();

        private ClassOptionVm? _selectedClass;
        public ClassOptionVm? SelectedClass
        {
            get => _selectedClass;
            private set
            {
                _selectedClass = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewClassName));
                OnPropertyChanged(nameof(PreviewClassGrowthLine));
                OnPropertyChanged(nameof(PreviewStatLine1));
                OnPropertyChanged(nameof(PreviewStatLine2));
                OnPropertyChanged(nameof(PreviewHpMpLine));
                Revalidate();
            }
        }

        private string EffectiveClass => _selectedClass?.Class ?? CharacterClass.Fighter;
        public string PreviewClassName => _selectedClass != null ? Localization.T($"class.{EffectiveClass}") : "";
        public string PreviewClassGrowthLine
        {
            get
            {
                if (_selectedClass == null || !ClassProfile.All.TryGetValue(EffectiveClass, out var p)) return "";
                return $"STR +{p.StatGrowth["STR"]}  DEX +{p.StatGrowth["DEX"]}  " +
                       $"END +{p.StatGrowth["END"]}  INT +{p.StatGrowth["INT"]}  " +
                       $"SPR +{p.StatGrowth["SPR"]}  HP +{p.HpPerLevel}  MP +{p.ManaPerLevel}  /lv";
            }
        }

        // True once there is something meaningful to show in the stats block.
        // In multiplayer we wait until a race is chosen; singleplayer always shows defaults.
        public bool ShowStartingStats => !IsMultiplayer || _selectedRace != null;

        public string PreviewStatLine1
        {
            get
            {
                var rp  = EffectiveProfile;
                int str = 10 + rp.BaseStatBonus["STR"];
                int dex = 10 + rp.BaseStatBonus["DEX"];
                int end = 10 + rp.BaseStatBonus["END"];
                if (_selectedClass != null && ClassProfile.All.TryGetValue(EffectiveClass, out var cp))
                {
                    str += cp.StatGrowth["STR"];
                    dex += cp.StatGrowth["DEX"];
                    end += cp.StatGrowth["END"];
                }
                return $"STR: {str}  DEX: {dex}  END: {end}";
            }
        }

        public string PreviewStatLine2
        {
            get
            {
                var rp   = EffectiveProfile;
                int @int = 10 + rp.BaseStatBonus["INT"];
                int spr  = 10 + rp.BaseStatBonus["SPR"];
                if (_selectedClass != null && ClassProfile.All.TryGetValue(EffectiveClass, out var cp))
                {
                    @int += cp.StatGrowth["INT"];
                    spr  += cp.StatGrowth["SPR"];
                }
                return $"INT: {@int}  SPR: {spr}";
            }
        }

        public string PreviewHpMpLine
        {
            get
            {
                var rp = EffectiveProfile;
                int hp = 30 + rp.BaseHpBonus;
                int mp = 30 + rp.BaseManaBonus;
                if (_selectedClass != null && ClassProfile.All.TryGetValue(EffectiveClass, out var cp))
                {
                    hp += cp.HpPerLevel;
                    mp += cp.ManaPerLevel;
                }
                return $"HP: {hp}  MP: {mp}";
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────────

        public ICommand CreateCommand { get; }
        public ICommand BackCommand { get; }

        private readonly UserAccount _user;

        public ViewModel_CharacterCreationPage()
        {
            _user = UserAccountService.CurrentUser;
            CreateCommand = new RelayCommand(Create);
            BackCommand = new RelayCommand(() => Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection()));

            if (IsMultiplayer)
            {
                foreach (var race in RaceProfile.All.Keys)
                    Races.Add(new RaceOptionVm(race, OnRaceSelected));
            }
            else
            {
                RebuildClasses();
            }

            Revalidate();
        }

        private void OnRaceSelected(RaceOptionVm chosen)
        {
            foreach (var opt in Races)
                opt.IsSelected = opt == chosen;
            SelectedRace = chosen;
            RebuildClasses();
        }

        private void RebuildClasses()
        {
            Classes.Clear();
            _selectedClass = null;
            foreach (var cls in ClassManager.GetAllowedClasses(EffectiveRace))
                Classes.Add(new ClassOptionVm(cls, OnClassSelected));
            OnPropertyChanged(nameof(SelectedClass));
            OnPropertyChanged(nameof(PreviewClassName));
            OnPropertyChanged(nameof(PreviewClassGrowthLine));
            Revalidate();
        }

        private void OnClassSelected(ClassOptionVm chosen)
        {
            if (!chosen.IsAvailable) return;

            foreach (var opt in Classes)
                opt.IsSelected = opt == chosen;
            SelectedClass = chosen;
        }

        private void Revalidate()
        {
            ValidationText = "";
            var name = (CharacterName ?? "").Trim();

            // Checked first so the whole rest of the creation flow doesn't waste the player's
            // time on race/class/name choices they won't be able to submit anyway - see
            // UserAccount.MaxCharacters for why this cap exists (only 5 fixed slots in
            // Page_CharacterSelection's current design). Re-checked defensively here (not just
            // gating the "Create" button back on Page_CharacterSelection) in case this page was
            // reached with stale state.
            int existingCount = UserAccountService.CurrentUser?.CharacterNames.Count ?? 0;
            if (existingCount >= UserAccount.MaxCharacters)
                ValidationText = Localization.T("pg.character.create.validation.max_reached", UserAccount.MaxCharacters);
            else if (name.Length < 2)
                ValidationText = Localization.T("pg.character.create.validation.name.short");
            else if (name.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
                ValidationText = Localization.T("pg.character.create.validation.name.invalid");
            else if (IsMultiplayer && _selectedRace == null)
                ValidationText = Localization.T("pg.race_select.validation");
            else if (_selectedClass == null)
                ValidationText = Localization.T("pg.character.create.validation.class.missing");

            CanCreate = string.IsNullOrWhiteSpace(ValidationText);
        }

        private async void Create()
        {
            if (!CanCreate) return;

            var name       = CharacterName.Trim();
            var race       = EffectiveRace;
            var raceProfile = EffectiveProfile;

            var stats = new Stats
            {
                Strength     = 10 + raceProfile.BaseStatBonus["STR"],
                Dexterity    = 10 + raceProfile.BaseStatBonus["DEX"],
                Endurance    = 10 + raceProfile.BaseStatBonus["END"],
                Intelligence = 10 + raceProfile.BaseStatBonus["INT"],
                Spirit       = 10 + raceProfile.BaseStatBonus["SPR"],
                BaseHealth   = 30 + raceProfile.BaseHpBonus,
                BaseMana     = 30 + raceProfile.BaseManaBonus,
            };

            var character = new Character(name, stats) { Race = race, RaceSelected = true, Class = EffectiveClass };

            StartingEquipmentService.GrantStartingEquipment(character);
            SkillFactory.UpdateSkills(character);

            // Every load path (ServerApiService.LoadCharacterAsync, SqlCharacterRepository.LoadAsync
            // used by the multiplayer hub, CharacterService.LoadCharacter) calls this to auto-fill
            // SkillSlots from an empty list - this creation path never did. For multiplayer that's
            // not just a cosmetic gap: the server independently loads/migrates this same character
            // the moment it attaches a session (GameHub.LoadCharacter), so without this the two
            // sides pick different starting slot state from the start - the server ends up with
            // slot 1 already filled (from its own migration) while the client shows 0 slots used,
            // so the very next skill the player tries to slot gets silently rejected (server is
            // already at cap) and reverts client-side, looking like "it assigned then un-assigned
            // itself" for every attempt until a relog re-syncs both sides to the same migrated state.
            SkillSlotService.MigrateIfEmpty(character);

            // Character's constructor snapshots CurrentHealth/CurrentMana from MaxHealth/MaxMana
            // before this object initializer's Class assignment above has run - Class is still its
            // default (Fighter) at that point, so the snapshot used the wrong class's HP/MP growth
            // instead of EffectiveClass's. Re-synced here, after Class/gear are all final, so
            // Current correctly starts at the character's real Max (same fix Myria.Mono already
            // has in its own CharacterCreationScreen for this exact issue).
            character.CurrentHealth = character.MaxHealth;
            character.CurrentMana   = character.MaxMana;

            character.CurrentRoom = RoomService.GetRoomById(1);
            character.CurrentRoomId = character.CurrentRoom.Id;

            if (ServerApiService.Token is not null)
            {
                bool saved = await ServerApiService.SaveCharacterAsync(character);
                if (!saved)
                {
                    ValidationText = $"Could not save character to server: {ServerApiService.LastError}";
                    return;
                }

                // Unlike the offline branch below, CurrentUser.CharacterNames here was fetched
                // once from the Auth server at login and never refreshed since - without this,
                // the new character exists server-side but is invisible on Page_CharacterSelection
                // (which reads this same in-memory list) until a full relogin re-fetches it.
                UserAccountService.CurrentUser.CharacterNames.Add(character.Name);
            }
            else
            {
                CharacterService.SaveCharacter(_user, character);
                UserAccountService.CurrentUser.CharacterNames.Add(character.Name);
                UserAccountService.SaveUser();
            }

            UserAccountService.CurrentCharacter = character;
            GameService.StartSession(character);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_Game());
        }
    }

    public class ClassOptionVm : BaseViewModel
    {
        public string Class { get; }
        public string Name { get; }
        public string StatGrowthLabel { get; }

        /// <summary>False for classes that exist in the data but are not selectable in this build (e.g. RunicMage — see Pflichtenheft/Herausforderungen: rune magic scope cut).</summary>
        public bool IsAvailable { get; }
        public string? UnavailableHint { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ButtonLabel)); }
        }

        public string ButtonLabel => IsSelected
            ? Localization.T("pg.race_select.selected")
            : Localization.T("pg.race_select.pick");

        public ICommand SelectCommand { get; }

        public ClassOptionVm(string cls, Action<ClassOptionVm> onSelect)
        {
            Class = cls;
            Name  = Localization.T($"class.{cls}");
            ClassProfile.All.TryGetValue(cls, out var p);
            StatGrowthLabel = p != null
                ? $"STR +{p.StatGrowth["STR"]}  DEX +{p.StatGrowth["DEX"]}  END +{p.StatGrowth["END"]}  " +
                  $"INT +{p.StatGrowth["INT"]}  SPR +{p.StatGrowth["SPR"]}  HP +{p.HpPerLevel}  MP +{p.ManaPerLevel}"
                : "";

            IsAvailable     = !cls.Equals(CharacterClass.RunicMage, StringComparison.OrdinalIgnoreCase);
            UnavailableHint = IsAvailable ? null : Localization.T("pg.character.create.class.unavailable");

            SelectCommand = new RelayCommand(() => onSelect(this));
        }
    }
}
