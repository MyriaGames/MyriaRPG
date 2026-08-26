using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;
using System.Collections.ObjectModel;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class DialogPanelViewModel : BaseViewModel
    {
        protected readonly Npc _npc;
        protected readonly Character _character;
        private readonly Action<string> _onNavigate;

        private string _dialogText;
        public string DialogText
        {
            get => _dialogText;
            set { _dialogText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ServiceOption> ServiceOptions { get; } = new();

        public DialogPanelViewModel(Npc npc, Character character, Action<string> onNavigate)
        {
            _npc = npc;
            _character = character;
            _onNavigate = onNavigate;

            DialogText = Localization.T($"npc.{npc.Id}.dialog");
            RefreshServiceOptions();
        }

        private void RefreshServiceOptions()
        {
            ServiceOptions.Clear();

            foreach (var service in _npc.Services)
            {
                var s = service;
                ServiceOptions.Add(new ServiceOption
                {
                    Text        = Localization.T($"npc.service.{s}.title"),
                    Description = Localization.T($"npc.service.{s}.desc"),
                    Command     = new RelayCommand(() => HandleService(s))
                });
            }

            foreach (var quest in QuestManager.GetAcceptableForNpc(_character, _npc.Id))
            {
                var q = quest;
                ServiceOptions.Add(new ServiceOption
                {
                    Text        = Localization.T("npc.service.quest.title"),
                    Description = LocalizationText.LocalizeQuestText(q.Name),
                    Command     = new RelayCommand(() => _onNavigate($"quest:{q.Id}"))
                });
            }

            foreach (var quest in QuestManager.GetReturnableForNpc(_character, _npc.Id))
            {
                var q = quest;
                ServiceOptions.Add(new ServiceOption
                {
                    Text        = Localization.T("npc.service.quest_return.title"),
                    Description = LocalizationText.LocalizeQuestText(q.Name),
                    Command     = new RelayCommand(() => _onNavigate($"quest_return:{q.Id}"))
                });
            }
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            DialogText = Localization.T($"npc.{_npc.Id}.dialog");
            RefreshServiceOptions();
        }

        private void HandleService(string serviceId)
        {
            if (serviceId == "heal")
            {
                ExecuteHeal();
                return;
            }

            if (serviceId == "talk")
            {
                DialogText = Localization.T($"npc.{_npc.Id}.dialog");
                return;
            }

            _onNavigate(serviceId);
        }

        protected virtual void ExecuteHeal()
        {
            NpcActionResult res = _npc.HealingAction(_character);
            DialogText = Localization.T(res.MessageKey, res.MessageArgs);
        }
    }
}
