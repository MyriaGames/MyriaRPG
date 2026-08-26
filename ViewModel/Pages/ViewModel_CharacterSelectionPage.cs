using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game;
using Myria.Wpf.View.Windows;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_CharacterSelectionPage : BaseViewModel
    {
        private string? _selectedCharacterName;
        private int _selectedCharacterIndex;
        private Task<Character?>? _characterLoadTask;
        private bool _isConfirmingDelete;
        private bool _isBusy;
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
            get => _btnJoin;
            set { _btnJoin = value; OnPropertyChanged(nameof(btnJoin)); }
        }

        [LocalizedKey("app.general.UI.create")]
        public string btnCreate 
        {
            get => _btnCreate;
            set { _btnCreate = value; OnPropertyChanged(nameof(btnCreate)); }
        }

        [LocalizedKey("app.general.UI.delete")]
        public string btnDelete 
        {
            get => _btnDelete;
            set { _btnDelete = value; OnPropertyChanged(nameof(btnDelete)); }
        }

            [LocalizedKey("app.general.UI.back")]
        public string btnBack 
        { 
            get => _btnBack;
            set { _btnBack = value; OnPropertyChanged(nameof(btnBack)); }
        }
        public Visibility Visibility1 
        {
            get => _visibility1;
            set { _visibility1 = value; OnPropertyChanged(nameof(Visibility1)); }
        }
        public Visibility Visibility2 
        { 
            get => _visibility2;
            set { _visibility2 = value; OnPropertyChanged(nameof(Visibility2)); }
        }
        public Visibility Visibility3 
        { 
            get => _visibility3;
            set { _visibility3 = value; OnPropertyChanged(nameof(Visibility3)); }
        }
        public Visibility Visibility4 
        { 
            get => _visibility4;
            set { _visibility4 = value; OnPropertyChanged(nameof(Visibility4)); }
        }
        public Visibility Visibility5 
        { 
            get => _visibility5;
            set { _visibility5 = value; OnPropertyChanged(nameof(Visibility5)); }
        }
        public string btnCharacter1 { get; set; }
        public string btnCharacter2 { get; set; }
        public string btnCharacter3 { get; set; }
        public string btnCharacter4 { get; set; }
        public string btnCharacter5 { get; set; }

        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set { _isConfirmingDelete = value; OnPropertyChanged(nameof(IsConfirmingDelete)); }
        }

        public string ConfirmDeleteMessage => _selectedCharacterName is null
            ? string.Empty
            : $"Are you sure you want to permanently delete \"{_selectedCharacterName}\"?\nThis action cannot be undone.";

        // Guards against double-clicking Join/Delete/Back while their server call (session
        // start, character delete, or disconnect) is in flight.
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                Join.RaiseCanExecuteChanged();
                Delete.RaiseCanExecuteChanged();
                (Back as RelayCommand)?.RaiseCanExecuteChanged();
                (ConfirmDelete as RelayCommand)?.RaiseCanExecuteChanged();
                (Create as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand Join { get; }
        public ICommand Create { get; }
        public RelayCommand Delete { get; }
        public ICommand Back { get; }
        public ICommand ConfirmDelete { get; }
        public ICommand CancelDelete  { get; }
        public ICommand SelectFirst { get; }
        public ICommand SelectSecond { get; }
        public ICommand SelectThird { get; }
        public ICommand SelectFourth { get; }
        public ICommand SelectFifth { get; }

        private void BeginSelect(int index)
        {
            var names = UserAccoundService.CurrentUser?.CharacterNames ?? [];
            if (index >= names.Count) return;
            _selectedCharacterName  = names[index];
            _selectedCharacterIndex = index;
            _characterLoadTask      = LoadCharacterAsync(index);
            Join.RaiseCanExecuteChanged();
            Delete.RaiseCanExecuteChanged();
        }

        public ViewModel_CharacterSelectionPage()
        {
            Join          = new RelayCommand(JoinAction, () => IsSelected() && !IsBusy);
            Create        = new RelayCommand(CreateAction, () => !IsBusy);
            Delete        = new RelayCommand(DeleteAction, () => IsSelected() && !IsBusy);
            Back          = new RelayCommand(BackAction, () => !IsBusy);
            ConfirmDelete = new RelayCommand(ConfirmDeleteAction, () => !IsBusy);
            CancelDelete  = new RelayCommand(() => IsConfirmingDelete = false);
            SelectFirst = new RelayCommand(SelectFirstAction);
            SelectSecond = new RelayCommand(SelectSecondAction);
            SelectThird = new RelayCommand(SelectThirdAction);
            SelectFourth = new RelayCommand(SelectFourthAction);
            SelectFifth = new RelayCommand(SelectFifthAction);

            LocalizationAutoWire.Wire(this);

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

        private void SelectFirstAction()  => BeginSelect(0);
        private void SelectSecondAction() => BeginSelect(1);
        private void SelectThirdAction()  => BeginSelect(2);
        private void SelectFourthAction() => BeginSelect(3);
        private void SelectFifthAction()  => BeginSelect(4);

        private async Task<Character?> LoadCharacterAsync(int index)
        {
            var names = UserAccoundService.CurrentUser?.CharacterNames ?? [];
            if (index >= names.Count) return null;

            if (ServerApiService.Token is not null)
            {
                LoadWarnings.Consume();
                var character = await ServerApiService.LoadCharacterAsync(names[index]);
                LoadWarnings.Consume();
                character?.Inventory.Items.RemoveAll(i => i == null);

                return character;
            }

            // ── Mod snapshot check ───────────────────────────────────────────────
            var savedSnapshot   = CharacterService.ReadModSnapshot(names[index], UserAccoundService.CurrentUser!);
            var currentSnapshot = ModLoader.GetCurrentSnapshot();

            if (savedSnapshot == null && currentSnapshot.Mods.Count > 0)
            {
                var dialog = new Window_ModWarning(
                    title:        "Unknown Mod Configuration",
                    subtitle:     "This character was saved before mod tracking was introduced. " +
                                  "It is unknown which mods were active at that time.\n\n" +
                                  "If you have gameplay mods active now, some content may be " +
                                  "missing or behave differently.",
                    detailsLabel: "",
                    details:      null);
                if (dialog.ShowDialog() != true) return null;
            }
            else if (savedSnapshot != null && !savedSnapshot.Matches(currentSnapshot))
            {
                var diff = savedSnapshot.GetChanges(currentSnapshot);
                var lines = new System.Text.StringBuilder();

                if (diff.Added.Count > 0)
                    lines.AppendLine("Added:    " + string.Join(", ", diff.Added));
                if (diff.Removed.Count > 0)
                    lines.AppendLine("Removed:  " + string.Join(", ", diff.Removed));
                if (diff.Changed.Count > 0)
                    lines.AppendLine("Changed:  " + string.Join(", ", diff.Changed.Select(c => $"{c.Id} ({c.Was} → {c.Now})")));

                var dialog = new Window_ModWarning(
                    title:        "Mod Configuration Changed",
                    subtitle:     "This character was last saved with a different mod configuration. " +
                                  "Some items, skills, or content may be missing or behave differently.",
                    detailsLabel: "Changes since last save",
                    details:      lines.ToString().TrimEnd());
                if (dialog.ShowDialog() != true) return null;
            }

            // ── Load character ───────────────────────────────────────────────────
            LoadWarnings.Consume();
            var sp = CharacterService.LoadCharacter(names[index], UserAccoundService.CurrentUser!);
            var skipped = LoadWarnings.Consume();

            if (skipped.Count > 0)
            {
                string itemList = string.Join(", ", skipped);
                var result = MessageBox.Show(
                    $"The following items no longer exist in the game and were removed from your inventory:\n\n{itemList}\n\nDo you want to continue?",
                    "Removed Items",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return null;
            }

            if (sp is not null && !sp.RaceSelected)
            {
                sp.Race = CharacterRace.Myralu;
                sp.RaceSelected = true;
                CharacterService.SaveCharacter(UserAccoundService.CurrentUser!, sp);
            }

            return sp;
        }
        private async void JoinAction()
        {
            if (_selectedCharacterName is null) return;

            IsBusy = true;
            try
            {
                var character = await (_characterLoadTask ?? LoadCharacterAsync(_selectedCharacterIndex));
                if (character is null) return;

                if (!character.RaceSelected)
                {
                    Navigation.Current.Navigate(NavigationFrameType.Main, new Page_RaceSelection(character));
                    return;
                }

                // Rune magic was cut from this build's scope (see Herausforderungen 8.1); characters
                // that are still RunicMage from before that decision must pick a new class first.
                if (character.Class.Equals(CharacterClass.RunicMage, StringComparison.OrdinalIgnoreCase))
                {
                    Navigation.Current.Navigate(NavigationFrameType.Main, new Page_ClassReselection(character));
                    return;
                }

                UserAccoundService.CurrentCharacter = character;
                SkillFactory.UpdateSkills(character);
                GameService.StartSession(character);
                Navigation.Current.Navigate(NavigationFrameType.Main, new Page_Game());
            }
            finally
            {
                IsBusy = false;
            }
        }
        private bool IsSelected() => _selectedCharacterName != null;
        private void CreateAction()
        {
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterCreation());
        }
        private void DeleteAction()
        {
            if (_selectedCharacterName is null) return;
            OnPropertyChanged(nameof(ConfirmDeleteMessage));
            IsConfirmingDelete = true;
        }

        private async void ConfirmDeleteAction()
        {
            IsConfirmingDelete = false;
            if (_selectedCharacterName is null) return;
            var name = _selectedCharacterName;

            IsBusy = true;
            try
            {
                if (ServerApiService.Token is not null)
                    await ServerApiService.DeleteCharacterAsync(name);
                else if (UserAccoundService.CurrentUser is not null)
                    CharacterService.DeleteCharacter(name, UserAccoundService.CurrentUser);

                UserAccoundService.CurrentUser?.CharacterNames.Remove(name);
                UserAccoundService.SaveUser();
                _selectedCharacterName = null;
                _characterLoadTask     = null;
                UserAccoundService.CurrentCharacter = null;
                Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection());
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async void BackAction()
        {
            IsBusy = true;
            try
            {
                if (ServerApiService.Token is not null)
                {
                    await GameHubService.DisconnectAsync();
                    ServerApiService.ClearToken();
                }
                Navigation.Current.Navigate(Nav.Startup);
            }
            finally
            {
                IsBusy = false;
            }
        }

    }

}
