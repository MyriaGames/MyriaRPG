using Myria.Lib.Core.Entities.Jobs;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class JobMasterPanelViewModel : BaseViewModel
    {
        protected readonly Character _character;
        protected readonly string _jobId;
        private readonly Job? _job;

        public string Title => Localization.T("npc.jobmaster.title");
        public string BtnBack => Localization.T("app.general.UI.back");

        public string JobName => _job != null ? Localization.T(_job.Name) : _jobId;
        public string JobDescription => _job != null ? Localization.T(_job.Description) : "";
        public string JobType => _job?.Type ?? "";

        public int SkillLevel => JobXpService.GetLevel(Entry.SkillXp);
        public int KnowledgeLevel => JobXpService.GetLevel(Entry.KnowledgeXp);
        public int FameLevel => JobXpService.GetLevel(Entry.FameXp);

        public double SkillFraction => JobXpService.GetProgressFraction(Entry.SkillXp);
        public double KnowledgeFraction => JobXpService.GetProgressFraction(Entry.KnowledgeXp);
        public double FameFraction => JobXpService.GetProgressFraction(Entry.FameXp);

        public string SkillProgress => JobXpService.FormatProgress(Entry.SkillXp);
        public string KnowledgeProgress => JobXpService.FormatProgress(Entry.KnowledgeXp);
        public string FameProgress => JobXpService.FormatProgress(Entry.FameXp);

        public string SkillLabel => Localization.T("npc.jobmaster.skill") + $"  {Localization.T("npc.jobmaster.level", SkillLevel)}";
        public string KnowledgeLabel => Localization.T("npc.jobmaster.knowledge") + $"  {Localization.T("npc.jobmaster.level", KnowledgeLevel)}";
        public string FameLabel => Localization.T("npc.jobmaster.fame") + $"  {Localization.T("npc.jobmaster.level", FameLevel)}";

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            private set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveButtonLabel)); }
        }

        public string ActiveButtonLabel => IsActive
            ? Localization.T("npc.jobmaster.stop_working")
            : Localization.T("npc.jobmaster.work_as", JobName);

        public ICommand GoBackCommand { get; }
        public ICommand ToggleActiveCommand { get; }

        private CharacterJob Entry => JobManager.GetOrAdd(_character, _jobId);

        public JobMasterPanelViewModel(Npc npc, Character character, Action goBack)
        {
            _character = character;
            _jobId = npc.MasterJobId ?? "";
            _job = JobManager.GetById(_jobId);

            GoBackCommand = new RelayCommand(goBack);
            ToggleActiveCommand = new RelayCommand(ToggleActive);

            _isActive = _character.ActiveJobId == _jobId;
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(BtnBack));
            OnPropertyChanged(nameof(JobName));
            OnPropertyChanged(nameof(JobDescription));
            OnPropertyChanged(nameof(SkillLabel));
            OnPropertyChanged(nameof(KnowledgeLabel));
            OnPropertyChanged(nameof(FameLabel));
            OnPropertyChanged(nameof(ActiveButtonLabel));
        }

        private void ToggleActive() => ExecuteJobToggle();

        protected virtual void ExecuteJobToggle()
        {
            if (IsActive)
                JobManager.SetActiveJob(_character, null);
            else
                JobManager.SetActiveJob(_character, _jobId);

            IsActive = _character.ActiveJobId == _jobId;
        }
    }
}
