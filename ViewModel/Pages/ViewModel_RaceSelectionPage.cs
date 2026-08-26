using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class RaceOptionVm : BaseViewModel
    {
        public string Race { get; }
        public string Name { get; }
        public string Description { get; }
        public string StatGrowthLabel { get; }

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

        public RaceOptionVm(string race, Action<RaceOptionVm> onSelect)
        {
            Race = race;
            Name = Localization.T($"race.{race}");
            Description = Localization.T($"race.{race}.desc");

            RaceProfile.All.TryGetValue(race, out var profile);
            if (profile == null) { SelectCommand = new RelayCommand(() => onSelect(this)); return; }
            StatGrowthLabel = string.Join("  ",
                $"STR +{profile.StatGrowth["STR"]}",
                $"DEX +{profile.StatGrowth["DEX"]}",
                $"END +{profile.StatGrowth["END"]}",
                $"INT +{profile.StatGrowth["INT"]}",
                $"SPR +{profile.StatGrowth["SPR"]}",
                $"HP +{profile.HpPerLevel}",
                $"MP +{profile.ManaPerLevel}");

            SelectCommand = new RelayCommand(() => onSelect(this));
        }
    }

    public class ViewModel_RaceSelectionPage : BaseViewModel
    {
        private readonly Character _character;

        public string Title    => Localization.T("pg.race_select.title");
        public string Subtitle => Localization.T("pg.race_select.subtitle");
        public string BtnBack  => Localization.T("app.general.UI.back");
        public string BtnConfirm => Localization.T("pg.race_select.confirm");
        public string GrowthHeader => Localization.T("pg.race_select.stat_growth");

        private string _validationText = "";
        public string ValidationText
        {
            get => _validationText;
            private set { _validationText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RaceOptionVm> Races { get; } = new();

        private RaceOptionVm? _selectedRace;
        public RaceOptionVm? SelectedRace
        {
            get => _selectedRace;
            private set
            {
                _selectedRace = value;
                OnPropertyChanged();
                ValidationText = "";
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand BackCommand { get; }

        public ViewModel_RaceSelectionPage(Character character, Action goBack)
        {
            _character = character;

            foreach (var race in RaceProfile.All.Keys)
                Races.Add(new RaceOptionVm(race, OnRaceSelected));

            ConfirmCommand = new RelayCommand(ConfirmAsync, () => SelectedRace != null);
            BackCommand    = new RelayCommand(goBack);
        }

        private void OnRaceSelected(RaceOptionVm chosen)
        {
            foreach (var opt in Races)
                opt.IsSelected = opt == chosen;
            SelectedRace = chosen;
        }

        private async void ConfirmAsync()
        {
            if (SelectedRace is null)
            {
                ValidationText = Localization.T("pg.race_select.validation");
                return;
            }

            var oldRace = _character.Race;
            _character.Race = SelectedRace.Race;
            _character.RaceSelected = true;

            ApplyRaceMigration(_character, oldRace, SelectedRace.Race);

            if (ServerApiService.Token is not null)
                await ServerApiService.SaveCharacterAsync(_character);

            UserAccoundService.CurrentCharacter = _character;
            SkillFactory.UpdateSkills(_character);
            GameService.StartSession(_character);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_Game());
        }

        /// <summary>
        /// Swaps the one-time base-stat bonus from <paramref name="from"/> to <paramref name="to"/>.
        /// Called only once during the migration of characters that never had a race explicitly set.
        /// </summary>
        private static void ApplyRaceMigration(Character character, string from, string to)
        {
            if (from.Equals(to, StringComparison.OrdinalIgnoreCase)) return;

            if (!RaceProfile.All.TryGetValue(from, out var oldProfile)) return;
            if (!RaceProfile.All.TryGetValue(to,   out var newProfile)) return;

            character.Stats.Strength     += newProfile.BaseStatBonus["STR"] - oldProfile.BaseStatBonus["STR"];
            character.Stats.Dexterity    += newProfile.BaseStatBonus["DEX"] - oldProfile.BaseStatBonus["DEX"];
            character.Stats.Endurance    += newProfile.BaseStatBonus["END"] - oldProfile.BaseStatBonus["END"];
            character.Stats.Intelligence += newProfile.BaseStatBonus["INT"] - oldProfile.BaseStatBonus["INT"];
            character.Stats.Spirit       += newProfile.BaseStatBonus["SPR"] - oldProfile.BaseStatBonus["SPR"];

            character.Stats.BaseHealth   += newProfile.BaseHpBonus   - oldProfile.BaseHpBonus;
            character.Stats.BaseMana     += newProfile.BaseManaBonus - oldProfile.BaseManaBonus;
        }
    }
}
