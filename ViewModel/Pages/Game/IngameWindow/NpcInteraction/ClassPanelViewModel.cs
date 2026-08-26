using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class ClassOptionVm : BaseViewModel
    {
        private readonly Character _character;
        private readonly string _class;
        private readonly Action _onChanged;
        private readonly Action<string> _executeChange;

        public string Name { get; }
        public string GroupLabel { get; }
        public bool IsAllowed { get; }
        public string ForbiddenLabel => Localization.T("pg.class.forbidden");

        public int ClassLevel => ClassManager.GetClassLevel(_character, _class);
        public double ProgressFraction => ClassXpService.GetProgressFraction(ClassManager.GetClassXp(_character, _class));
        public string ProgressText => ClassXpService.FormatProgress(ClassManager.GetClassXp(_character, _class));

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(ButtonLabel)); }
        }

        public string ButtonLabel => IsActive
            ? Localization.T("pg.class.active_class")
            : Localization.T("pg.class.change");

        public ICommand SelectCommand { get; }

        public ClassOptionVm(Character character, string cls, Action onChanged, Action<string> executeChange)
        {
            _character     = character;
            _class         = cls;
            _onChanged     = onChanged;
            _executeChange = executeChange;
            Name           = Localization.T($"class.{cls}");

            var group = ClassManager.GetClassGroup(cls);
            GroupLabel = group switch
            {
                "Physical"    => Localization.T("pg.class.group.physical"),
                "RangerRogue" => Localization.T("pg.class.group.rangerrogue"),
                "Mage"        => Localization.T("pg.class.group.mage"),
                "DivineHybrid"=> Localization.T("pg.class.group.divinehybrid"),
                _             => group
            };
            IsAllowed = ClassManager.IsClassAllowed(character.Race, cls);
            _isActive = character.Class.Equals(cls, StringComparison.OrdinalIgnoreCase);

            SelectCommand = new RelayCommand(Select, () => IsAllowed && !IsActive && ClassManager.CanChangeClass(_character));
        }

        private void Select()
        {
            _executeChange(_class);
            _onChanged();
        }

        public void Refresh()
        {
            IsActive = _character.Class.Equals(_class, StringComparison.OrdinalIgnoreCase);
            OnPropertyChanged(nameof(ClassLevel));
            OnPropertyChanged(nameof(ProgressFraction));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public class ClassPanelViewModel : BaseViewModel
    {
        protected readonly Character _character;

        public string Title         => Localization.T("pg.class.title");
        public string BtnBack       => Localization.T("app.general.UI.back");
        public string PenaltyInfo   => Localization.T("pg.class.penalty_info", "500");
        public string TransferInfo  => Localization.T("pg.class.transfer_info");
        public string CurrentClass  => Localization.T($"class.{_character.Class}");
        public string CurrentLevel  => Localization.T("pg.class.level", ClassManager.GetClassLevel(_character, _character.Class).ToString());

        public bool IsCooldownActive => !ClassManager.CanChangeClass(_character);
        public string CooldownText
        {
            get
            {
                var remaining = ClassManager.GetClassChangeCooldownRemaining(_character);
                if (remaining <= TimeSpan.Zero) return "";
                int days = (int)Math.Ceiling(remaining.TotalDays);
                return Localization.T("pg.class.cooldown", days.ToString());
            }
        }

        public ObservableCollection<ClassOptionVm> Classes { get; } = new();

        public ICommand GoBackCommand { get; }

        public ClassPanelViewModel(Character character, Action goBack)
        {
            _character    = character;
            GoBackCommand = new RelayCommand(goBack);

            foreach (var cls in Myria.Lib.Core.Entities.Characters.ClassProfile.All.Keys)
                Classes.Add(new ClassOptionVm(character, cls, OnClassChanged, ExecuteClassChange));
        }

        protected virtual void ExecuteClassChange(string cls)
        {
            ClassManager.SetClass(_character, cls);
        }

        private void OnClassChanged()
        {
            foreach (var opt in Classes)
                opt.Refresh();

            OnPropertyChanged(nameof(CurrentClass));
            OnPropertyChanged(nameof(CurrentLevel));
            OnPropertyChanged(nameof(IsCooldownActive));
            OnPropertyChanged(nameof(CooldownText));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
}
