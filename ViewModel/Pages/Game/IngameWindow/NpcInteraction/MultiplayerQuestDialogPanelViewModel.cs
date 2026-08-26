using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class MultiplayerQuestDialogPanelViewModel : QuestDialogPanelViewModel
    {
        public MultiplayerQuestDialogPanelViewModel(Quest quest, Character character, Npc npc, bool isReturn, Action goBack)
            : base(quest, character, npc, isReturn, goBack) { }

        protected override void ExecuteQuestAction()
        {
            // Fire-and-forget: mirrors this accept/return onto the server's session character
            // (see GameHub.QuestAction) so quest rewards - including XP - aren't client-only.
            _ = GameHubService.QuestActionAsync(_quest.Id, _isReturn);
            base.ExecuteQuestAction();
        }
    }
}
