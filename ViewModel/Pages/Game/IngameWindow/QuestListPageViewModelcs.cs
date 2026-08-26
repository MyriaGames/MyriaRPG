using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Formatter;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class QuestListPageViewModel : BaseViewModel
    {
        private string tbl_Title;
        private string tbl_Info;
        private string tbl_Level;
        private string tbl_Description;
        private string tbl_Objectives;
        private string tbl_Rewards;
        private string btn_Track;
        private string btn_Abandon;
        private string btn_Accept;
        private string btn_Active;
        private string btn_Available;
        [LocalizedKey("pg.quests.title")]
        public string TblTitle
        {
            get { return tbl_Title; }
            set
            {
                tbl_Title = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.details.selecthint")]
        public string TblInfo
        {
            get { return tbl_Info; }
            set
            {
                tbl_Info = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.level")]
        public string TblLevel
        {
            get { return tbl_Level; }
            set
            {
                tbl_Level = value + " ";
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.section.description")]
        public string TblDescription
        {
            get { return tbl_Description; }
            set
            {
                tbl_Description = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.section.objectives")]
        public string TblObjectives
        {
            get { return tbl_Objectives; }
            set
            {
                tbl_Objectives = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.section.rewards")]
        public string TblRewards
        {
            get { return tbl_Rewards; }
            set
            {
                tbl_Rewards = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.btn.return")]
        public string BtnTrack
        {
            get { return btn_Track; }
            set
            {
                btn_Track = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.btn.abandon")]
        public string BtnAbandon
        {
            get { return btn_Abandon; }
            set
            {
                btn_Abandon = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.btn.accept")]
        public string BtnAccept
        {
            get { return btn_Accept; }
            set
            {
                btn_Accept = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.tab.active")]
        public string BtnActive
        {
            get { return btn_Active; }
            set
            {
                btn_Active = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveCountLabel));
            }

        }
        [LocalizedKey("pg.quests.tab.available")]
        public string BtnAvailable
        {
            get { return btn_Available; }
            set
            {
                btn_Available = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvailableCountLabel));
            }

        }

        public string ActiveCountLabel =>
            $"{btn_Active} ({_character.ActiveQuests.Count(q => q.Status != QuestStatus.Returned)})";

        public string AvailableCountLabel
        {
            get
            {
                var all = QuestManager.GetAvailableForCharacter(_character);
                var activeIds = new HashSet<string>(_character.ActiveQuests.Select(q => q.Id));
                return $"{btn_Available} ({all.Count(q => !activeIds.Contains(q.Id))})";
            }
        }

        public Visibility QuestListVisibility  => Quests.Count > 0 ? Visibility.Visible  : Visibility.Collapsed;
        public Visibility EmptyStateVisibility => Quests.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public string EmptyStateText => ShowActive
            ? $"You currently have no active quests."
            : $"There are currently no available quests.";
        private Character _character = UserAccoundService.CurrentCharacter;
        // Mode
        private bool _showActive = true;
        public bool ShowActive
        {
            get => _showActive;
            set
            {
                if (_showActive == value) return;
                _showActive = value;
                if (value) ShowAvailable = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsShowingActive));
                OnPropertyChanged(nameof(IsShowingAvailable));
                UpdateMode();
            }
        }

        private bool _showAvailable;
        public bool ShowAvailable
        {
            get => _showAvailable;
            set
            {
                if (_showAvailable == value) return;
                _showAvailable = value;
                if (value) ShowActive = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsShowingActive));
                OnPropertyChanged(nameof(IsShowingAvailable));
                UpdateMode();
            }
        }

        public Visibility IsShowingActive => ShowActive ? Visibility.Visible : Visibility.Hidden;
        public Visibility IsShowingAvailable => ShowAvailable ? Visibility.Visible : Visibility.Hidden;

        public string HeaderSuffix => ShowActive ? $"({Myria.Lib.Core.Systems.Localization.T("pg.quests.tab.active")})" : $"({Myria.Lib.Core.Systems.Localization.T("pg.quests.tab.available")})";

        // Data
        public ObservableCollection<QuestListItemVm> Quests { get; } = new();
        private QuestListItemVm? _selectedQuest;
        public QuestListItemVm? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                _selectedQuest = value;
                HasSelected = _selectedQuest != null ? Visibility.Visible : Visibility.Hidden;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GiverNpcHint));
                OnPropertyChanged(nameof(IsSelectedQuestReturnable));
                OnPropertyChanged(nameof(IsSelectedQuestDirectlyAcceptable));
                OnPropertyChanged(nameof(IsGiverNpcHintVisible));
            }
        }
        private Visibility _hasSelected = Visibility.Hidden;
        public Visibility HasSelected
        {
            get => _hasSelected;
            set {_hasSelected = value; OnPropertyChanged(); }
        }

        public Visibility IsSelectedQuestReturnable =>
            _selectedQuest != null && ShowActive
                && _selectedQuest.IsRepeatable
                && HasBeenCompletedBefore(_selectedQuest.Id)
                && _selectedQuest.Status == QuestStatus.Completed
                ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsSelectedQuestDirectlyAcceptable =>
            _selectedQuest != null && ShowAvailable
                && _selectedQuest.IsRepeatable
                && HasBeenCompletedBefore(_selectedQuest.Id)
                ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsGiverNpcHintVisible =>
            ShowAvailable && IsSelectedQuestDirectlyAcceptable != Visibility.Visible
                ? Visibility.Visible : Visibility.Collapsed;

        // Commands
        public ICommand AbandonQuestCommand       { get; }
        public ICommand ReturnQuestCommand        { get; }
        public ICommand AcceptQuestCommand        { get; }
        public ICommand SelectActiveTabCommand    { get; }
        public ICommand SelectAvailableTabCommand { get; }

        /// <summary>Hint shown in Available tab: "Speak to [NPC name] to accept."</summary>
        public string GiverNpcHint => _selectedQuest != null && !string.IsNullOrEmpty(_selectedQuest.GiverNpcId)
            ? Myria.Lib.Core.Systems.Localization.T("pg.quests.hint.speak_to", _selectedQuest.GiverNpcName)
            : string.Empty;

        // Optional: used by your in-game window title binding
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }
        private string _windowTitle = Myria.Lib.Core.Systems.Localization.T("pg.quests.title");

        public QuestListPageViewModel()
        {
            AbandonQuestCommand       = new RelayCommand<QuestListItemVm?>(AbandonQuest);
            ReturnQuestCommand        = new RelayCommand<QuestListItemVm?>(ReturnQuest);
            AcceptQuestCommand        = new RelayCommand<QuestListItemVm?>(AcceptQuest);
            SelectActiveTabCommand    = new RelayCommand(() => ShowActive    = true);
            SelectAvailableTabCommand = new RelayCommand(() => ShowAvailable = true);

            UpdateMode();
        }

        private void UpdateMode()
        {
            Quests.Clear();

            if (ShowActive)
            {
                foreach (var q in _character.ActiveQuests
                             /*.Where(q => q.Status != QuestStatus.Completed)*/)
                {
                    Quests.Add(ToVm(q));
                }

            }
            else
            {
                var all = QuestManager.GetAvailableForCharacter(_character);

                var activeIds = new HashSet<string>(_character.ActiveQuests.Select(q => q.Id));
                foreach (var q in all.Where(q => !activeIds.Contains(q.Id)))
                {
                    Quests.Add(ToVm(q));
                }

            }

            SelectedQuest = Quests.FirstOrDefault();
            OnPropertyChanged(nameof(HeaderSuffix));
            OnPropertyChanged(nameof(ActiveCountLabel));
            OnPropertyChanged(nameof(AvailableCountLabel));
            OnPropertyChanged(nameof(QuestListVisibility));
            OnPropertyChanged(nameof(EmptyStateVisibility));
            OnPropertyChanged(nameof(EmptyStateText));
            OnPropertyChanged(nameof(IsSelectedQuestReturnable));
            OnPropertyChanged(nameof(IsSelectedQuestDirectlyAcceptable));
            OnPropertyChanged(nameof(IsGiverNpcHintVisible));
        }
        private void AbandonQuest(QuestListItemVm? quest)
        {
            if (quest == null)
                return;
            Quest? playQuest = _character.ActiveQuests.FirstOrDefault(a => a.Id == quest.Id);
            if (playQuest == null) return;
            _character.ActiveQuests.Remove(playQuest);
            Quests.Remove(quest);
            OnPropertyChanged(nameof(ActiveCountLabel));
            OnPropertyChanged(nameof(AvailableCountLabel));
            OnPropertyChanged(nameof(QuestListVisibility));
            OnPropertyChanged(nameof(EmptyStateVisibility));
        }

        private void ReturnQuest(QuestListItemVm? questVm)
        {
            if (questVm == null) return;
            var active = _character.ActiveQuests.FirstOrDefault(q => q.Id == questVm.Id);
            if (active == null || active.Status != QuestStatus.Completed) return;
            if (!active.IsRepeatable) return;                   // non-repeatables must be returned at the NPC
            if (!HasBeenCompletedBefore(active.Id)) return;     // first run must also go through the NPC

            active.GrantRewards(_character);

            if (active.IsRepeatable)
            {
                if (!_character.RepeatableQuestRecords.TryGetValue(active.Id, out var rec))
                {
                    rec = new RepeatRecord();
                    _character.RepeatableQuestRecords[active.Id] = rec;
                }
                if (rec.LastCompletionDate?.Date != DateTime.Today)
                    rec.CompletionsToday = 0;
                rec.TimesCompleted++;
                rec.CompletionsToday++;
                rec.LastCompletionDate = DateTime.Now;
                _character.ActiveQuests.Remove(active);
            }
            else
            {
                active.Status = QuestStatus.Returned;
                _character.CompletedQuests.Add(active);
                _character.ActiveQuests.Remove(active);
            }

            Quests.Remove(questVm);
            OnPropertyChanged(nameof(ActiveCountLabel));
            OnPropertyChanged(nameof(AvailableCountLabel));
            OnPropertyChanged(nameof(QuestListVisibility));
            OnPropertyChanged(nameof(EmptyStateVisibility));
        }

        private void AcceptQuest(QuestListItemVm? questVm)
        {
            if (questVm == null) return;
            if (!questVm.IsRepeatable || !HasBeenCompletedBefore(questVm.Id)) return;

            var template = QuestManager.GetQuestById(questVm.Id);
            if (template == null) return;

            var clone = template.Clone();
            clone.GrantAcceptItems(_character);
            clone.Status = clone.IsTalkOnly ? QuestStatus.Completed : QuestStatus.InProgress;
            _character.ActiveQuests.Add(clone);

            UpdateMode();
        }

        private bool HasBeenCompletedBefore(string questId)
            => _character.RepeatableQuestRecords.TryGetValue(questId, out var rec) && rec.TimesCompleted >= 1;

        private static string ResolveNpcName(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return "";
            return Myria.Lib.Core.Services.NpcService.TryGet(npcId, out var npc) && npc != null
                ? Myria.Lib.Core.Systems.Localization.T(npc.NameKey)
                : npcId;
        }

        private QuestListItemVm ToVm(Quest quest)
        {
            var vm = new QuestListItemVm
            {
                Id               = quest.Id,
                Title            = LocalizationText.LocalizeQuestText(quest.Name),
                Level            = quest.RequiredLevel,
                Status           = quest.Status,
                IsRepeatable     = quest.IsRepeatable,
                GiverNpcId       = quest.GiverNpcId,
                GiverNpcName     = ResolveNpcName(quest.GiverNpcId),
                AreaName         = ResolveNpcName(quest.GiverNpcId),
                ShortDescription = LocalizationText.LocalizeQuestText(quest.Description),
                Description      = LocalizationText.LocalizeQuestText(quest.Description),
                ProgressText     = QuestFormatter.BuildProgressText(quest)
            };
            var objectives = new List<string>();
            foreach (string line in QuestFormatter.BuildItemsObjectivesLine(quest))
                objectives.Add(line);
            foreach (string line in QuestFormatter.BuildKillsObjectiveLine(quest))
                objectives.Add(line);
            vm.Objectives = objectives;
            vm.Rewards = QuestFormatter.BuildRewardsLine(quest);
            return vm;
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            WindowTitle = Myria.Lib.Core.Systems.Localization.T("pg.quests.title");
            UpdateMode();
            OnPropertyChanged(nameof(HeaderSuffix));
            OnPropertyChanged(nameof(GiverNpcHint));
        }

    }

    public class QuestListItemVm : BaseViewModel
    {
        public string      Id               { get; set; } = "";
        public string      Title            { get; set; } = "";
        public int         Level            { get; set; }
        public QuestStatus Status           { get; set; } = QuestStatus.InProgress;
        public bool        IsRepeatable     { get; set; }
        public string      GiverNpcId       { get; set; } = "";
        public string      GiverNpcName     { get; set; } = "";
        public string      AreaName         { get; set; } = "";
        public string      ShortDescription { get; set; } = "";
        public string      Description      { get; set; } = "";
        public string      ProgressText     { get; set; } = "";

        public IEnumerable<string> Objectives { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Rewards    { get; set; } = Array.Empty<string>();

        public string SubtitleText => !string.IsNullOrEmpty(ProgressText) ? ProgressText : AreaName;

        public Brush AccentBrush => Status switch
        {
            QuestStatus.Completed  => new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A)),
            QuestStatus.InProgress => new SolidColorBrush(Color.FromRgb(0xC9, 0xA8, 0x4C)),
            _                      => new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
        };

        public string StatusBadge    => Status == QuestStatus.Completed ? "DONE" : IsRepeatable ? "?" : string.Empty;
        public bool   HasStatusBadge => !string.IsNullOrEmpty(StatusBadge);
    }
}
