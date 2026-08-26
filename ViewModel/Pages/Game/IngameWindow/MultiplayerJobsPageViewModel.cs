using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class MultiplayerJobsPageViewModel : JobsPageViewModel
    {
        public MultiplayerJobsPageViewModel(Character character) : base(character) { }

        internal override void SetActive(string? jobId)
        {
            // Fire-and-forget: mirrors this onto the server's session character
            // (see GameHub.ToggleJob) so it isn't client-only.
            _ = GameHubService.ToggleJobAsync(jobId);
            base.SetActive(jobId);
        }
    }
}
