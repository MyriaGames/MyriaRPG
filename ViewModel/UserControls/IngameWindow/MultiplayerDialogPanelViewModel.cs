using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class MultiplayerDialogPanelViewModel : DialogPanelViewModel
    {
        private readonly Character _mpCharacter;

        public MultiplayerDialogPanelViewModel(Npc npc, Character character, Action<string> onNavigate)
            : base(npc, character, onNavigate)
        {
            _mpCharacter = character;
        }

        protected override async void ExecuteHeal()
        {
            // The server holds the authoritative character for this session - heal there, then
            // apply the result via SetHealth/SetMana so HealthChanged/ManaChanged fire and the
            // HP/MP bars actually refresh (unlike the base class's local-only Heal/RestoreMana).
            var result = await GameHubService.HealAsync();
            if (result == null || !result.Success)
            {
                DialogText = Localization.T("pg.fight.log.connection_error");
                return;
            }

            // Use the client's own Max*, not the server's result.CharacterHp/CharacterMana -
            // "heal" always means "fill to full", and SetHealth/SetMana clamp to the client's
            // own MaxHealth/MaxMana anyway. If the client and server's derived max ever drift
            // by even a little (e.g. a stat/gear sync gap), trusting the server's raw number
            // here would get silently clamped short of what the client's own bar shows as 100%.
            _mpCharacter.SetHealth(_mpCharacter.MaxHealth, "Healer");
            _mpCharacter.SetMana(_mpCharacter.MaxMana, "Healer");
            DialogText = Localization.T("npc.action.heal.ok");
        }
    }
}
