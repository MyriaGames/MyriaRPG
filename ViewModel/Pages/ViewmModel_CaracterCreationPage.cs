using MyriaLib.Entities;
using MyriaLib.Entities.Players;
using MyriaLib.Models;
using MyriaLib.Services;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using MyriaLib.Entities.Players;
using MyriaRPG.Model;
using MyriaRPG.Services;
using MyriaRPG.Utils;
using MyriaRPG.View.Pages;
using MyriaRPG.View.Pages.Game;
using System.Windows.Input;

namespace MyriaRPG.ViewModel.Pages
{
    class ViewmModel_CaracterCreationPage : BaseViewModel
    {
        private string tbl_Name;
        private string tbl_Race;
        private string tbl_ShowName;
        private string tbl_ShowRace;
        private string tbl_Preview;
        private string tbl_Title;
        private string btn_Back;
        private string btn_Create;

        [LocalizedKey("app.general.UI.name")]
        public string TblName
        {
            get { return tbl_Name; }
            private set { tbl_Name = value; OnPropertyChanged(nameof(TblName)); }
        }

        [LocalizedKey("app.general.UI.name")]
        public string TblShowName
        {
            get { return tbl_ShowName; }
            private set { tbl_ShowName = value + ": "; OnPropertyChanged(nameof(TblShowName)); }
        }

        [LocalizedKey("pg.character.create.race")]
        public string TblRace
        {
            get { return tbl_Race; }
            private set { tbl_Race = value; OnPropertyChanged(nameof(TblRace)); }
        }

        [LocalizedKey("pg.character.create.race")]
        public string TblShowRace
        {
            get { return tbl_ShowRace; }
            private set { tbl_ShowRace = value + ": "; OnPropertyChanged(nameof(TblShowRace)); }
        }

        [LocalizedKey("pg.character.create.preview")]
        public string TblPreview
        {
            get { return tbl_Preview; }
            private set { tbl_Preview = value; OnPropertyChanged(nameof(TblPreview)); }
        }

        [LocalizedKey("pg.character.create.title")]
        public string TblTitle
        {
            get { return tbl_Title; }
            private set { tbl_Title = value; OnPropertyChanged(nameof(TblTitle)); }
        }

        [LocalizedKey("app.general.UI.back")]
        public string BtnBack
        {
            get { return btn_Back; }
            private set { btn_Back = value; OnPropertyChanged(nameof(BtnBack)); }
        }

        [LocalizedKey("app.general.UI.create")]
        public string BtnCreate
        {
            get { return btn_Create; }
            private set { btn_Create = value; OnPropertyChanged(nameof(BtnCreate)); }
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

        public ICommand CreateCommand { get; }
        public ICommand BackCommand { get; }

        private readonly UserAccount _user;

        public ViewmModel_CaracterCreationPage()
        {
            _user = UserAccoundService.CurrentUser;
            CreateCommand = new RelayCommand(Create);
            BackCommand = new RelayCommand(() => Navigation.NavigateMain(new Page_CharacterSelection()));
            Revalidate();
        }

        private void Revalidate()
        {
            ValidationText = "";
            var name = (CharacterName ?? "").Trim();

            if (name.Length < 2)
                ValidationText = Localization.T("pg.character.create.validation.name.short");
            else if (name.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
                ValidationText = Localization.T("pg.character.create.validation.name.invalid");

            CanCreate = string.IsNullOrWhiteSpace(ValidationText);
        }

        private async void Create()
        {
            if (!CanCreate) return;

            var name = CharacterName.Trim();
            var raceProfile = RaceProfile.All[PlayerRace.Myralu];

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

            var player = new Player(name, stats) { Race = PlayerRace.Myralu };

            player.CurrentRoom = RoomService.GetRoomById(11);
            player.CurrentRoomId = player.CurrentRoom.Id;

            if (ServerApiService.Token is not null)
                await ServerApiService.SaveCharacterAsync(player);
            else
            {
                CharacterService.SaveCharacter(_user, player);
                UserAccoundService.CurrentUser.CharacterNames.Add(player.Name);
                UserAccoundService.SaveUser();
            }

            UserAccoundService.CurrentCharacter = player;
            GameService.StartSession(player);
            Navigation.NavigateMain(new Page_Game());
        }
    }
}
