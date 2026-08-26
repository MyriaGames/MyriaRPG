using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class MultiplayerJobMasterPanelViewModel : JobMasterPanelViewModel
    {
        public MultiplayerJobMasterPanelViewModel(Npc npc, Character character, Action goBack)
            : base(npc, character, goBack) { }

        protected override void ExecuteJobToggle()
        {
            // Fire-and-forget: mirrors this onto the server's session character
            // (see GameHub.ToggleJob) so it isn't client-only.
            var newJobId = IsActive ? null : _jobId;
            _ = GameHubService.ToggleJobAsync(newJobId);
            base.ExecuteJobToggle();
        }
    }
}
