using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems.Enums;
using Localization = Myria.Lib.Core.Systems.Localization;
using Myria.Wpf.Utils;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class QuestDialogPanelViewModel : BaseViewModel
    {
        protected readonly Quest    _quest;
        protected readonly Character _character;
        private readonly Npc        _npc;
        protected readonly bool  _isReturn;
        private readonly Action  _goBack;
        private readonly List<DialogLine> _lines;
        private int _index;

        public string QuestTitle => LocalizationText.LocalizeQuestText(_quest.Name);

        private string _speakerName = "";
        public string SpeakerName
        {
            get => _speakerName;
            private set { _speakerName = value; OnPropertyChanged(); }
        }

        private string _lineText = "";
        public string LineText
        {
            get => _lineText;
            private set { _lineText = value; OnPropertyChanged(); }
        }

        public bool IsLastLine => _index >= _lines.Count - 1;

        public Visibility NextVisible   => !IsLastLine ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ActionVisible =>  IsLastLine ? Visibility.Visible : Visibility.Collapsed;

        public string ActionLabel  => _isReturn
            ? Localization.T("npc.quest.btn.return")
            : Localization.T("npc.quest.btn.accept");
        public string DeclineLabel => Localization.T("app.general.UI.back");
        public string NextLabel    => Localization.T("app.general.UI.next");

        public ICommand NextCommand    { get; }
        public ICommand ActionCommand  { get; }
        public ICommand DeclineCommand { get; }

        public QuestDialogPanelViewModel(Quest quest, Character character, Npc npc, bool isReturn, Action goBack)
        {
            _quest    = quest;
            _character   = character;
            _npc      = npc;
            _isReturn = isReturn;
            _goBack   = goBack;

            var dialog = isReturn ? quest.ReturnDialog : quest.AcceptDialog;
            _lines = dialog.Count > 0 ? dialog : [new DialogLine { Speaker = "npc", Text = "..." }];
            _index = 0;

            NextCommand    = new RelayCommand(Advance, () => !IsLastLine);
            ActionCommand  = new RelayCommand(PerformAction);
            DeclineCommand = new RelayCommand(goBack);

            Refresh();
        }

        private void Advance()
        {
            if (_index < _lines.Count - 1)
            {
                _index++;
                Refresh();
            }
        }

        private void Refresh()
        {
            SpeakerName = ResolveSpeaker(_lines[_index].Speaker);
            LineText    = LocalizationText.LocalizeQuestText(_lines[_index].Text);
            OnPropertyChanged(nameof(IsLastLine));
            OnPropertyChanged(nameof(NextVisible));
            OnPropertyChanged(nameof(ActionVisible));
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            Refresh();
            OnPropertyChanged(nameof(QuestTitle));
            OnPropertyChanged(nameof(ActionLabel));
            OnPropertyChanged(nameof(DeclineLabel));
            OnPropertyChanged(nameof(NextLabel));
        }

        private void PerformAction() => ExecuteQuestAction();

        protected virtual void ExecuteQuestAction()
        {
            if (_isReturn)
            {
                var active = _character.ActiveQuests.FirstOrDefault(q => q.Id == _quest.Id);
                if (active == null) { _goBack(); return; }

                active.GrantRewards(_character); // includes J9/J10 job rewards via Quest.GrantRewards

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
            }
            else
            {
                if (!_character.ActiveQuests.Any(q => q.Id == _quest.Id))
                {
                    var clone = _quest.Clone();
                    clone.Status = clone.IsTalkOnly ? QuestStatus.Completed : QuestStatus.InProgress;
                    clone.GrantAcceptItems(_character);
                    _character.ActiveQuests.Add(clone);
                }
            }

            _goBack();
        }

        private string ResolveSpeaker(string speaker)
        {
            if (speaker == "player") return _character.Name;
            if (speaker == "npc")   return Localization.T(_npc.NameKey);
            if (speaker.StartsWith("npc:"))
            {
                var id = speaker[4..];
                return NpcService.TryGet(id, out var other) && other != null
                    ? Localization.T(other.NameKey)
                    : id;
            }
            return speaker;
        }
    }
}
