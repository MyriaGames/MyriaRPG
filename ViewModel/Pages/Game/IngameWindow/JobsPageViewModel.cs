using Myria.Lib.Core.Entities.Jobs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.Model;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class JobsPageViewModel : BaseViewModel
    {
        protected readonly Character _character;
        private string _tblBack = string.Empty;
        private string _tblTitle = string.Empty;
        private string _tblSubtitle = string.Empty;
        private string _tblCurrentJob = string.Empty;
        private string _tblNoActive = string.Empty;
        private string _tblNoActiveHint = string.Empty;
        private string _tblOverview = string.Empty;
        private string _tblAvailable = string.Empty;
        private string _tblActiveBadge = string.Empty;
        private string _tblSkill = string.Empty;
        private string _tblKnowledge = string.Empty;
        private string _tblFame = string.Empty;
        // ── Localized labels ─────────────────────────────────────────────────
        [LocalizedKey("app.general.UI.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.subtitle")]
        public string TblSubtitle
        {
            get => _tblSubtitle;
            set { _tblSubtitle = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.current")]
        public string TblCurrentJob
        {
            get => _tblCurrentJob;
            set { _tblCurrentJob = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.no_active")]
        public string TblNoActive
        {
            get => _tblNoActive;
            set { _tblNoActive = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.no_active.hint")]
        public string TblNoActiveHint
        {
            get => _tblNoActiveHint;
            set { _tblNoActiveHint = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.overview")]
        public string TblOverview
        {
            get => _tblOverview;
            set { _tblOverview = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.available")]
        public string TblAvailable
        {
            get => _tblAvailable;
            set { _tblAvailable = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.jobs.badge.active")]
        public string TblActiveBadge
        {
            get => _tblActiveBadge;
            set { _tblActiveBadge = value; OnPropertyChanged(); }
        }
        [LocalizedKey("npc.jobmaster.skill")]
        public string TblSkill
        {
            get => _tblSkill;
            set { _tblSkill = value; OnPropertyChanged(); }
        }
        [LocalizedKey("npc.jobmaster.knowledge")]
        public string TblKnowledge
        {
            get => _tblKnowledge;
            set { _tblKnowledge = value; OnPropertyChanged(); }
        }
        [LocalizedKey("npc.jobmaster.fame")]
        public string TblFame
        {
            get => _tblFame;
            set { _tblFame = value; OnPropertyChanged(); }
        }

        public ObservableCollection<JobVm> Jobs { get; } = new();

        public JobVm? CurrentJob    => Jobs.FirstOrDefault(j => j.IsActive);
        public bool   HasActiveJob  => CurrentJob != null;
        public bool   HasNoActiveJob => !HasActiveJob;

        public ICommand GoBackCommand { get; }

        private string _cooldownMessage = "";
        public string CooldownMessage
        {
            get => _cooldownMessage;
            private set { _cooldownMessage = value; OnPropertyChanged(); }
        }

        public bool IsOnCooldown => !JobManager.CanChangeJob(_character);

        public JobsPageViewModel(Character character)
        {
            _character = character;
            GoBackCommand = new RelayCommand(() => Navigation.Current.Navigate(new Page_Character()));
            Refresh();
            RefreshCooldown();
        }

        private void Refresh()
        {
            Jobs.Clear();
            foreach (var job in JobManager.GetAll())
            {
                var entry = JobManager.GetOrAdd(_character, job.Id);
                Jobs.Add(new JobVm(job, entry, _character, this));
            }
        }

        internal virtual void SetActive(string? jobId)
        {
            bool ok = JobManager.SetActiveJob(_character, jobId);
            RefreshCooldown();
            foreach (var vm in Jobs)
                vm.RefreshActive(_character.ActiveJobId);
            OnPropertyChanged(nameof(CurrentJob));
            OnPropertyChanged(nameof(HasActiveJob));
            OnPropertyChanged(nameof(HasNoActiveJob));
        }

        private void RefreshCooldown()
        {
            OnPropertyChanged(nameof(IsOnCooldown));
            var remaining = JobManager.GetCooldownRemaining(_character);
            CooldownMessage = remaining <= TimeSpan.Zero
                ? ""
                : string.Format(Localization.T("pg.jobs.cooldown_message"), FormatRemaining(remaining));
        }

        private static string FormatRemaining(TimeSpan t)
        {
            if (t.TotalDays >= 1)
                return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes}m";
            return $"{t.Minutes}m";
        }
    }

    public class JobVm : BaseViewModel
    {
        private readonly JobsPageViewModel _parent;
        private readonly CharacterJob _entry;
        private readonly Character _character;
        private readonly string _nameKey;
        private readonly string _descriptionKey;

        public string Id   { get; }
        public string Name => Localization.T(_nameKey);
        public string Description => Localization.T(_descriptionKey);
        public string Type { get; }

        public int SkillLevel    => JobXpService.GetLevel(_entry.SkillXp);
        public int KnowledgeLevel => JobXpService.GetLevel(_entry.KnowledgeXp);
        public int FameLevel     => JobXpService.GetLevel(_entry.FameXp);

        public string SkillProgress    => JobXpService.FormatProgress(_entry.SkillXp);
        public string KnowledgeProgress => JobXpService.FormatProgress(_entry.KnowledgeXp);
        public string FameProgress     => JobXpService.FormatProgress(_entry.FameXp);

        public double SkillFraction    => JobXpService.GetProgressFraction(_entry.SkillXp);
        public double KnowledgeFraction => JobXpService.GetProgressFraction(_entry.KnowledgeXp);
        public double FameFraction     => JobXpService.GetProgressFraction(_entry.FameXp);

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            private set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveLabel)); }
        }

        public string ActiveLabel => _isActive
            ? Localization.T("npc.jobmaster.stop_working")
            : string.Format(Localization.T("npc.jobmaster.work_as"), Name);

        public string SkillLevelText    => string.Format(Localization.T("npc.jobmaster.level"), SkillLevel);
        public string KnowledgeLevelText => string.Format(Localization.T("npc.jobmaster.level"), KnowledgeLevel);
        public string FameLevelText     => string.Format(Localization.T("npc.jobmaster.level"), FameLevel);
        public string ActiveSkillLevelText => string.Format(Localization.T("pg.jobs.level_full"), SkillLevel);

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(ActiveLabel));
            OnPropertyChanged(nameof(SkillLevelText));
            OnPropertyChanged(nameof(KnowledgeLevelText));
            OnPropertyChanged(nameof(FameLevelText));
            OnPropertyChanged(nameof(ActiveSkillLevelText));
        }

        public ICommand ToggleActiveCommand { get; }

        public JobVm(Job job, CharacterJob entry, Character character, JobsPageViewModel parent)
        {
            _entry  = entry;
            _parent = parent;
            _character = character;

            Id          = job.Id;
            _nameKey        = job.Name;
            _descriptionKey = job.Description;
            Type        = job.Type;
            _isActive   = character.ActiveJobId == job.Id;

            // Stopping is always free; starting requires the cooldown to have elapsed.
            ToggleActiveCommand = new RelayCommand(
                ToggleActive,
                () => _isActive || JobManager.CanChangeJob(_character));
        }

        private void ToggleActive()
            => _parent.SetActive(_isActive ? null : Id);

        internal void RefreshActive(string? activeJobId)
        {
            IsActive = activeJobId == Id;
        }
    }
}
