using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.UserControls;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class SkillPageViewModel : BaseViewModel
    {
        private string tbl_Title;
        [LocalizedKey("pg.skills.title")]
        public string TblTitle
        {
            get { return tbl_Title; }
            set
            {
                tbl_Title = value;
                OnPropertyChanged();
            }

        }

        private const int PageSize = 5;
        private readonly List<SkillVm> _allSkills = new();
        private int _currentPage = 1;

        public ObservableCollection<SkillVm> PagedSkills { get; } = new();

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = Math.Clamp(value, 1, TotalPages);
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageText));
                RefreshPage();
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(_allSkills.Count / (double)PageSize));
        public string PageText => $"{_currentPage} / {TotalPages}";

        private RelayCommand _prevPageCommand = null!;
        private RelayCommand _nextPageCommand = null!;
        public ICommand PrevPageCommand => _prevPageCommand;
        public ICommand NextPageCommand => _nextPageCommand;

        public ICommand OpenDetailsCommand { get; }
        public ICommand OpenCombineCommand { get; }
        public ICommand OpenSlotsCommand { get; }

        [LocalizedKey("pg.skill_combo.title")]
        public string TblCombineSkills { get; set; }

        [LocalizedKey("pg.skill_slots.title")]
        public string TblManageSlots { get; set; }

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }
        private string _windowTitle = Localization.T("pg.skills.title");

        public string HeaderText => string.Format(Localization.T("pg.skills.header.learned"), _allSkills.Count);

        [LocalizedKey("pg.skills.mp")]
        public string TblMp { get; set; } = string.Empty;

        [LocalizedKey("pg.skills.min_level")]
        public string TblMinLevel { get; set; } = string.Empty;

        [LocalizedKey("pg.skills.tag.healing")]
        public string TblHealingTag { get; set; } = string.Empty;

        [LocalizedKey("app.general.UI.details")]
        public string TblDetails { get; set; } = string.Empty;

        public SkillPageViewModel()
        {
            var player = UserAccoundService.CurrentCharacter;

            // Regular class skills (all classes)
            foreach (var s in player.Skills)
                _allSkills.Add(new SkillVm(s));

            // Combined skills
            foreach (var combined in player.CombinedSkills)
            {
                if (combined.ResolvedSkill != null)
                    _allSkills.Add(new SkillVm(combined.ResolvedSkill, "Combined"));
            }

            // Rune skills � magic classes that have runes in their collection
            foreach (var rune in player.KnownRunes)
            {
                if (rune.ResolvedSkill != null)
                    _allSkills.Add(new SkillVm(rune.ResolvedSkill, "Rune"));
            }

            // Fusion skills � physical classes that have composed skills
            foreach (var composite in player.CompositeSkills)
            {
                if (composite.ResolvedSkill != null)
                    _allSkills.Add(new SkillVm(composite.ResolvedSkill, "Fusion"));
            }

            OpenDetailsCommand = new RelayCommand<SkillVm?>(OpenDetails);
            OpenCombineCommand = new RelayCommand(() => Navigation.Current.Navigate(new Page_SkillCombination()));
            OpenSlotsCommand = new RelayCommand(() => Navigation.Current.Navigate(new Page_SkillSlots()));

            _prevPageCommand = new RelayCommand(() => CurrentPage--, () => _currentPage > 1);
            _nextPageCommand = new RelayCommand(() => CurrentPage++, () => _currentPage < TotalPages);
            RefreshPage();
        }

        private void RefreshPage()
        {
            PagedSkills.Clear();
            foreach (var s in _allSkills.Skip((_currentPage - 1) * PageSize).Take(PageSize))
                PagedSkills.Add(s);
            _prevPageCommand?.RaiseCanExecuteChanged();
            _nextPageCommand?.RaiseCanExecuteChanged();
        }

        private void OpenDetails(SkillVm? skill)
        {
            if (skill == null) return;

            var page = new Page_SkillDetail { DataContext = new SkillDetailViewModel(skill) };
            var win  = MainWindow.Instance.skillDetailWindow;
            win.NavigateTo(page, skill.Name);
            win.Visibility = System.Windows.Visibility.Visible;
        }

    }

    public class SkillVm : BaseViewModel
    {
        private readonly Skill _skill;

        /// <summary>"Rune", "Fusion", or empty string for regular class skills.</summary>
        public string Tag { get; }
        public bool HasTag => !string.IsNullOrEmpty(Tag);

        public SkillVm(Skill skill, string tag = "")
        {
            _skill = skill;
            Tag = tag;
        }

        public string Id => _skill.Id;
        public string Name => _skill.Name;
        public string Description => _skill.Description;
        public int ManaCost => _skill.ManaCost;
        public int MinLevel => _skill.MinLevel;
        public bool IsHealing => _skill.IsHealing;

        public string ClassName => Localization.T($"class.{_skill.Class}");
        public string TypeText => Localization.T($"pg.skills.type.{_skill.Type}");
        public string TargetText => _skill.Target switch
        {
            SkillTarget.SingleEnemy => Localization.T("pg.skills.target.single_enemy"),
            SkillTarget.AllEnemies  => Localization.T("pg.skills.target.all_enemies"),
            SkillTarget.Self        => Localization.T("pg.skills.target.self"),
            _                       => _skill.Target
        };

        public string ShortDescription =>
            string.IsNullOrWhiteSpace(_skill.Description)
                ? Localization.T("pg.skills.no_description")
                : _skill.Description.Length > 120
                    ? _skill.Description[..120] + "..."
                    : _skill.Description;

        public string TimingText
        {
            get
            {
                if (_skill.CastTime == 0 && _skill.RecoveryTime == 0)
                    return Localization.T("pg.skills.timing.instant");
                if (_skill.CastTime > 0 && _skill.RecoveryTime == 0)
                    return string.Format(Localization.T("pg.skills.timing.cast"), _skill.CastTime);
                if (_skill.CastTime == 0 && _skill.RecoveryTime > 0)
                    return string.Format(Localization.T("pg.skills.timing.recovery"), _skill.RecoveryTime);
                return string.Format(Localization.T("pg.skills.timing.cast_recovery"), _skill.CastTime, _skill.RecoveryTime);
            }
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(ClassName));
            OnPropertyChanged(nameof(TypeText));
            OnPropertyChanged(nameof(TargetText));
            OnPropertyChanged(nameof(ShortDescription));
            OnPropertyChanged(nameof(TimingText));
        }

        public float ScalingFactor => _skill.ScalingFactor;
        public string StatToScaleFrom => _skill.StatToScaleFrom;
    }

}
