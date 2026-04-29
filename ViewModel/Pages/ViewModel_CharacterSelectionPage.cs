using MyriaLib.Entities.Players;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaRPG.Model;
using MyriaRPG.Services;
using MyriaRPG.Utils;
using MyriaRPG.View.Pages;
using MyriaRPG.View.Pages.Game;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyriaRPG.ViewModel.Pages
{
    public class ViewModel_CharacterSelectionPage : BaseViewModel
    {
        private List<Player> characters = new List<Player>();
        private Player _selectedPlayer;
        private string _btnJoin;
        private string _btnCreate;
        private string _btnDelete;
        private string _btnBack;
        private Visibility _visibility1 = Visibility.Hidden;
        private Visibility _visibility2 = Visibility.Hidden;
        private Visibility _visibility3 = Visibility.Hidden;
        private Visibility _visibility4 = Visibility.Hidden;
        private Visibility _visibility5 = Visibility.Hidden;

        [LocalizedKey("pg.character.select.btn.join")]
        public string btnJoin 
        {
            get { return _btnJoin; }
            set
            {
                _btnJoin = value;
                OnPropertyChanged(nameof(btnJoin));
            }

        }

        [LocalizedKey("app.general.UI.create")]
        public string btnCreate 
        {
            get { return _btnCreate; }
            set
            {
                _btnCreate = value;
                OnPropertyChanged(nameof(btnCreate));
            }
            
        }

        [LocalizedKey("app.general.UI.delete")]
        public string btnDelete 
        {
            get { return _btnDelete; }
            set
            {
                _btnDelete = value;
                OnPropertyChanged(nameof(btnDelete));
            }

        }

            [LocalizedKey("app.general.UI.back")]
        public string btnBack 
        { 
            get { return _btnBack; }
            set
            {
                _btnBack = value;
                OnPropertyChanged(nameof(btnBack));
            }

        }
        public Visibility Visibility1 
        {
            get { return _visibility1; }
            set
            {
                _visibility1 = value; 
                OnPropertyChanged(nameof(Visibility1));
            }

        }
        public Visibility Visibility2 
        { 
            get { return _visibility2; }
            set
            {
                _visibility2 = value;
                OnPropertyChanged(nameof(Visibility2));
            }

        }
        public Visibility Visibility3 
        { 
            get { return _visibility3; }
            set
            {
                _visibility3 = value;
                OnPropertyChanged(nameof(Visibility3));
            }

        }
        public Visibility Visibility4 
        { 
            get { return _visibility4; }
            set
            {
                _visibility4 = value;
                OnPropertyChanged(nameof(Visibility4));
            }
            
        }
        public Visibility Visibility5 
        { 
            get { return _visibility5; }
            set
            {
                _visibility5 = value;
                OnPropertyChanged(nameof(Visibility5));
            }

        }
        public string btnCharacter1 { get; set; }
        public string btnCharacter2 { get; set; }
        public string btnCharacter3 { get; set; }
        public string btnCharacter4 { get; set; }
        public string btnCharacter5 { get; set; }

        public RelayCommand Join { get; }
        public ICommand Create { get; }
        public RelayCommand Delete { get; }
        public ICommand Back { get; }
        public ICommand SelectFirst { get; }
        public ICommand SelectSecond { get; }
        public ICommand SelectThird { get; }
        public ICommand SelectFourth { get; }
        public ICommand SelectFifth { get; }

        public Player SelectedPlayer 
        { 
            get { return _selectedPlayer; } 
            set
            {
                _selectedPlayer = value;
                OnPropertyChanged(nameof(SelectedPlayer));
                Join.RaiseCanExecuteChanged();
                Delete.RaiseCanExecuteChanged();
                UserAccoundService.CurrentCharacter = value;
            }

        }

        public ViewModel_CharacterSelectionPage()
        {
            Join = new RelayCommand(JoinAction, IsSelected);
            Create = new RelayCommand(CreateAction);
            Delete = new RelayCommand(DeleteAction, IsSelected);
            Back = new RelayCommand(BackAction);
            SelectFirst = new RelayCommand(SelectFirstAction);
            SelectSecond = new RelayCommand(SelectSecondAction);
            SelectThird = new RelayCommand(SelectThirdAction);
            SelectFourth = new RelayCommand(SelectFourthAction);
            SelectFifth = new RelayCommand(SelectFifthAction);

            LocalizationAutoWire.Wire(this);

            if (ServerApiService.Token is null)
                characters = CharacterService.LoadCharacters(UserAccoundService.CurrentUser);

            var names = UserAccoundService.CurrentUser?.CharacterNames ?? [];
            for (int count = 0; count < names.Count; count++)
            {
                switch (count)
                {
                    case 0: btnCharacter1 = names[count]; Visibility1 = Visibility.Visible; break;
                    case 1: btnCharacter2 = names[count]; Visibility2 = Visibility.Visible; break;
                    case 2: btnCharacter3 = names[count]; Visibility3 = Visibility.Visible; break;
                    case 3: btnCharacter4 = names[count]; Visibility4 = Visibility.Visible; break;
                    case 4: btnCharacter5 = names[count]; Visibility5 = Visibility.Visible; break;
                }
            }
        }

        private async void SelectFirstAction()  => SelectedPlayer = await LoadCharacterAsync(0);
        private async void SelectSecondAction() => SelectedPlayer = await LoadCharacterAsync(1);
        private async void SelectThirdAction()  => SelectedPlayer = await LoadCharacterAsync(2);
        private async void SelectFourthAction() => SelectedPlayer = await LoadCharacterAsync(3);
        private async void SelectFifthAction()  => SelectedPlayer = await LoadCharacterAsync(4);

        private async Task<Player?> LoadCharacterAsync(int index)
        {
            var names = UserAccoundService.CurrentUser?.CharacterNames ?? [];
            if (index >= names.Count) return null;

            if (ServerApiService.Token is not null)
                return await ServerApiService.LoadCharacterAsync(names[index]);

            return characters.Count > index ? characters[index] : null;
        }
        private void JoinAction()
        {
            SkillFactory.UpdateSkills(UserAccoundService.CurrentCharacter);
            GameService.StartSession(UserAccoundService.CurrentCharacter);
            Navigation.NavigateMain(new Page_Game());
        }
        private bool IsSelected()
        {
            return SelectedPlayer != null;
        }
        private void CreateAction()
        {
            Navigation.NavigateMain(new Page_CharacterCreation());
        }
        private async void DeleteAction()
        {
            if (SelectedPlayer is null) return;
            var name = SelectedPlayer.Name;

            if (ServerApiService.Token is not null)
                await ServerApiService.DeleteCharacterAsync(name);

            UserAccoundService.CurrentUser?.CharacterNames.Remove(name);
            SelectedPlayer = null;
            UserAccoundService.CurrentCharacter = null;
        }
        private void BackAction()
        {
            Navigation.NavigateMain(new Page_StartupMenue());
        }

    }

}
