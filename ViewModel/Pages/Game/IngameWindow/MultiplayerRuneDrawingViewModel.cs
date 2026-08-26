using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class MultiplayerRuneDrawingViewModel : RuneDrawingViewModel
    {
        protected override void ExecuteGrantRune(Character player, string baseRuneId)
        {
            // Fire-and-forget: mirrors this grant onto the server's session character
            // (see GameHub.GrantRune) so it isn't client-only.
            _ = GameHubService.GrantRuneAsync(baseRuneId);
            base.ExecuteGrantRune(player, baseRuneId);
        }
    }
}
