using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.World;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;

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

        // ── v0.3 quest rework: supply requests, bounties, Adventurers' Guild ──

        private int _offersVersion;
        private bool IsGuildHouse => _npc.Id == DynamicQuestRules.GuildNpcId;

        protected override bool SupportsService(string serviceId) => true;

        protected override void HandleService(string serviceId)
        {
            if (serviceId == DynamicQuestRules.GuildNpcId) { _ = ToggleGuildMembershipAsync(); return; }
            base.HandleService(serviceId);
        }

        /// <summary>Appends what this NPC is asking for right now (fetched from the server; a stale
        /// result from an earlier refresh is dropped).</summary>
        protected override void AddExtraOptions()
        {
            int version = ++_offersVersion;
            if (IsGuildHouse)
                DialogText = Localization.T(_character.AdventurersGuildMember ? "guild.adv.member" : "guild.adv.not_member");
            _ = AddOffersAsync(version);
        }

        private async Task AddOffersAsync(int version)
        {
            var offers = await GameHubService.GetDynamicQuestOffersAsync(_npc.Id);
            if (version != _offersVersion) return;

            if (IsGuildHouse && _character.AdventurersGuildMember && offers.Count == 0)
                DialogText = Localization.T("guild.adv.no_bounties");

            string label = Localization.T(IsGuildHouse ? "guild.adv.bounties" : "quest.dyn.offers");
            foreach (var offer in offers)
            {
                var preview = DynamicQuests.FromDto(offer.Preview);
                var token = offer.Token;
                ServiceOptions.Add(new ServiceOption
                {
                    Text        = label,
                    Description = QuestText.Name(preview) + "  ·  " + Localization.T("quest.dyn.reward",
                        preview.RewardXp, new Myria.Lib.Core.Entities.Characters.Money(preview.RewardGold).ToString()),
                    Command     = new RelayCommand(() => _ = AcceptOfferAsync(token))
                });
            }
        }

        private async Task AcceptOfferAsync(string token)
        {
            var dto = await GameHubService.AcceptDynamicQuestAsync(token);
            if (dto == null)
            {
                DialogText = Localization.T("quest.dyn.accept_failed");
                RefreshServiceOptions();
                return;
            }
            var quest = DynamicQuests.FromDto(dto);
            QuestManager.RegisterDynamic(quest);
            QuestManager.StartQuest(_mpCharacter, quest);
            DialogText = QuestText.Description(quest);
            RefreshServiceOptions();
        }

        private async Task ToggleGuildMembershipAsync()
        {
            bool wasMember = _mpCharacter.AdventurersGuildMember;
            bool ok = wasMember
                ? await GameHubService.LeaveAdventurersGuildAsync()
                : await GameHubService.JoinAdventurersGuildAsync();
            if (!ok)
            {
                DialogText = Localization.T("pg.fight.log.connection_error");
                return;
            }
            _mpCharacter.AdventurersGuildMember = !wasMember;
            RefreshServiceOptions();
            DialogText = Localization.T(wasMember ? "guild.adv.left" : "guild.adv.joined");
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
