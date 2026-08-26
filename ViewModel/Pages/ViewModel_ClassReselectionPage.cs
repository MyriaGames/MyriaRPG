using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    /// <summary>
    /// Shown instead of Page_Game when a character's current class is no longer selectable
    /// (RunicMage — rune magic was cut from this build's scope, see Herausforderungen 8.1).
    /// Mirrors ViewModel_RaceSelectionPage's "must resolve before entering the game" pattern.
    /// </summary>
    public class ViewModel_ClassReselectionPage : BaseViewModel
    {
        private readonly Character _character;

        public string Title      => Localization.T("pg.class_reselect.title");
        public string Subtitle   => Localization.T("pg.class_reselect.subtitle");
        public string BtnBack    => Localization.T("app.general.UI.back");
        public string BtnConfirm => Localization.T("pg.class_reselect.confirm");

        private string _validationText = "";
        public string ValidationText
        {
            get => _validationText;
            private set { _validationText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ClassOptionVm> Classes { get; } = new();

        private ClassOptionVm? _selectedClass;
        public ClassOptionVm? SelectedClass
        {
            get => _selectedClass;
            private set
            {
                _selectedClass = value;
                OnPropertyChanged();
                ValidationText = "";
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand BackCommand { get; }

        public ViewModel_ClassReselectionPage(Character character, Action goBack)
        {
            _character = character;

            // RunicMage is the reason we are here, so it is left out entirely rather than
            // shown disabled (unlike the creation-page picker, which still shows it greyed out).
            foreach (var cls in ClassManager.GetAllowedClasses(character.Race))
            {
                if (cls.Equals(CharacterClass.RunicMage, StringComparison.OrdinalIgnoreCase)) continue;
                Classes.Add(new ClassOptionVm(cls, OnClassSelected));
            }

            ConfirmCommand = new RelayCommand(ConfirmAsync, () => SelectedClass != null);
            BackCommand    = new RelayCommand(goBack);
        }

        private void OnClassSelected(ClassOptionVm chosen)
        {
            foreach (var opt in Classes)
                opt.IsSelected = opt == chosen;
            SelectedClass = chosen;
        }

        private async void ConfirmAsync()
        {
            if (SelectedClass is null)
            {
                ValidationText = Localization.T("pg.class_reselect.validation");
                return;
            }

            // Forced, one-time migration off a class that is no longer selectable — bypass the
            // regular class-change cooldown that would otherwise gate ClassManager.SetClass.
            _character.LastClassChanged = DateTime.MinValue;
            ClassManager.SetClass(_character, SelectedClass.Class);

            if (ServerApiService.Token is not null)
            {
                bool saved = await ServerApiService.SaveCharacterAsync(_character);
                if (!saved)
                {
                    ValidationText = $"Could not save character to server: {ServerApiService.LastError}";
                    return;
                }
            }

            UserAccoundService.CurrentCharacter = _character;
            SkillFactory.UpdateSkills(_character);
            GameService.StartSession(_character);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_Game());
        }
    }
}
