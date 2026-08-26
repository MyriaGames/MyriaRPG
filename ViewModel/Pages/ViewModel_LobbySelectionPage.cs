using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_LobbySelectionPage : BaseViewModel
    {
        private List<LobbyInfo> _lobbies = [];
        private LobbyInfo?      _selectedLobby;
        private bool            _isLoading = true;

        public List<LobbyInfo> Lobbies
        {
            get => _lobbies;
            set { _lobbies = value; OnPropertyChanged(nameof(Lobbies)); }
        }

        public LobbyInfo? SelectedLobby
        {
            get => _selectedLobby;
            set
            {
                _selectedLobby = value;
                OnPropertyChanged(nameof(SelectedLobby));
                OnPropertyChanged(nameof(IsJoinEnabled));
                _joinCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsNotLoading));
                OnPropertyChanged(nameof(IsJoinEnabled));
                _joinCommand.RaiseCanExecuteChanged();
                _backCommand.RaiseCanExecuteChanged();
                _reloadCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsNotLoading => !_isLoading;

        // Guards against double-clicking Join/Back/Reload while a lobby list fetch or the
        // realm-join request itself is in flight.
        public bool IsJoinEnabled => !_isLoading && _selectedLobby != null && _selectedLobby.IsOnline;

        private readonly RelayCommand _joinCommand;
        private readonly RelayCommand _backCommand;
        private readonly RelayCommand _reloadCommand;
        public ICommand Join   => _joinCommand;
        public ICommand Back   => _backCommand;
        public ICommand Reload => _reloadCommand;

        public ViewModel_LobbySelectionPage()
        {
            _joinCommand   = new RelayCommand(JoinAction, () => IsJoinEnabled);
            _backCommand   = new RelayCommand(BackAction, () => !IsLoading);
            _reloadCommand = new RelayCommand(async () => await LoadLobbiesAsync(), () => !IsLoading);

            _ = LoadLobbiesAsync();
        }

        private async Task LoadLobbiesAsync()
        {
            IsLoading = true;
            SelectedLobby = null;
            Lobbies = await ServerApiService.GetLobbiesAsync();
            IsLoading = false;
        }

        private async void JoinAction()
        {
            if (_selectedLobby is null || !_selectedLobby.IsOnline) return;
            LobbySelectionService.SelectedLobbyId   = _selectedLobby.Id;
            LobbySelectionService.SelectedRealmName = _selectedLobby.Name;
            ServerApiService.BaseUrl                = _selectedLobby.Url;

            // Character names are per-realm, so only fetch them now that BaseUrl actually points
            // at the chosen realm - fetching any earlier (e.g. right after login) would hit
            // whatever BaseUrl still defaulted to and silently cache an empty list.
            IsLoading = true;
            try
            {
                var names = await ServerApiService.GetCharacterNamesAsync();
                if (UserAccountService.CurrentUser != null)
                    UserAccountService.CurrentUser.CharacterNames = names;

                Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection());
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BackAction()
        {
            // Page_Login is hosted as a sub-view inside Page_StartupMenu's own frame — clear
            // back to that frame instead of pushing a new full-screen Page_Login onto Main.
            Navigation.Current.ClearFrame(NavigationFrameType.Startup);
        }
    }
}
