using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class MultiplayerClassPanelViewModel : ClassPanelViewModel
    {
        public MultiplayerClassPanelViewModel(Character character, Action goBack)
            : base(character, goBack) { }

        protected override void ExecuteClassChange(string cls)
        {
            // Fire-and-forget: mirrors this onto the server's session character
            // (see GameHub.ChangeClass) so it isn't client-only.
            _ = GameHubService.ChangeClassAsync(cls);
            base.ExecuteClassChange(cls);
        }
    }
}
