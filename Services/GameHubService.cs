using Microsoft.AspNetCore.SignalR.Client;
using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Services;
using Myria.Wpf.Model;

namespace Myria.Wpf.Services
{
    public static class GameHubService
    {
        private static HubConnection? _connection;

        public static bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        public static event Action? HubConnected;
        public static event Action? ForceLoggedOut;
        public static event Action<string, string, string>? ChatMessageReceived; // sender, message, channel
        public static event Action<string>? CharacterEntered;
        public static event Action<string>? CharacterLeft;
        public static event Action<List<string>>? RoomCharactersReceived;

        // Bystander activity events — a room-mate started/stopped a visible action.
        public static event Action<string>? CharacterGathering;
        public static event Action<string>? CharacterCrafting;
        public static event Action<string>? CharacterUpgrading;
        public static event Action<string>? CharacterInCombat;
        public static event Action<string>? CharacterCombatEnded;

        // Trade events
        public static event Action<string, string>? TradeProposed;      // fromName, tradeId
        public static event Action<string>?         TradeStarted;       // partnerName
        public static event Action<TradeSnapshot>?  TradeUpdated;
        public static event Action<TradeCompletedResult>? TradeCompleted;
        public static event Action<string>?         TradeCancelled;     // reason

        // Group combat events
        public static event Action<StartGroupCombatResult>? GroupCombatStarted;
        public static event Action<GroupCombatSnapshot>?    GroupCombatUpdated;
        public static event Action<GroupCombatSnapshot>?    GroupCombatFinished;
        public static event Action<int, string>?            CharacterRespawned;   // roomId, roomName

        // Shop events
        public static event Action<List<string>>?          RoomShopsReceived;    // owner names, sent on room join
        public static event Action<string>?                ShopOpened;           // ownerName
        public static event Action<string>?                ShopClosed;           // ownerName
        public static event Action<List<ShopListingVm>>?   MyShopUpdated;        // owner's own full storage view
        public static event Action<string, string, int, long, long>? ShopSale;   // buyerName, itemId, qty, totalPaid, fee
        public static event Action<bool, string, string, int, long>? ShopBuyResult; // ok, error, itemId, qty, paid
        public static event Action<string>?                ShopErrorReceived;    // error code

        // Generic server-authoritative character sync - see registration below for what applies it.
        public static event Action<CharacterUpdateDto>?    CharacterUpdated;

        // Party events
        public static event Action<string, string>? PartyInviteReceived;                           // fromUsername, partyId
        public static event Action<List<string>, string?>? PartyUpdated;                           // members, leaderUsername
        public static event Action? PartyDisbanded;
        public static event Action? KickedFromParty;
        public static event Action<string, int, int, int, int, int>? PartyMemberStatsUpdated;     // username, hp, maxHp, mana, maxMana, level

        public static async Task ConnectAsync()
        {
            if (ServerApiService.Token is null) return;
            if (_connection is not null) await DisconnectAsync();

            // Reload game data without mods before connecting so the client uses
            // the same vanilla data set as the server.
            Myria.Lib.Core.Systems.Mods.ModLoader.ApplyMultiplayerMode(true);

            _connection = new HubConnectionBuilder()
                .WithUrl($"{ServerApiService.BaseUrl}/hubs/game", opts =>
                {
                    opts.AccessTokenProvider = () =>
                        Task.FromResult<string?>(ServerApiService.Token);
                    // Trust the same pinned self-signed cert as every other request
                    // (see ServerApiService.CreateHttpMessageHandler) instead of falling back
                    // to default TLS validation, which would reject it outright.
                    opts.HttpMessageHandlerFactory = _ => ServerApiService.CreateHttpMessageHandler();
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On("ForceLogout", () =>
                Dispatch(() => ForceLoggedOut?.Invoke()));

            _connection.On<string, string, string>("ChatMessage", (sender, msg, channel) =>
                Dispatch(() => ChatMessageReceived?.Invoke(sender, msg, channel)));

            _connection.On<string, string>("TradeProposed", (from, tradeId) =>
                Dispatch(() => TradeProposed?.Invoke(from, tradeId)));

            _connection.On<string>("TradeStarted", partnerName =>
                Dispatch(() => TradeStarted?.Invoke(partnerName)));

            _connection.On<TradeSnapshot>("TradeUpdated", snap =>
                Dispatch(() => TradeUpdated?.Invoke(snap)));

            _connection.On<TradeCompletedResult>("TradeCompleted", result =>
                Dispatch(() => TradeCompleted?.Invoke(result)));

            _connection.On<string>("TradeCancelled", reason =>
                Dispatch(() => TradeCancelled?.Invoke(reason)));

            _connection.On<string>("CharacterEntered", name =>
                Dispatch(() => CharacterEntered?.Invoke(name)));

            _connection.On<string>("CharacterLeft", name =>
                Dispatch(() => CharacterLeft?.Invoke(name)));

            _connection.On<List<string>>("RoomCharacters", players =>
                Dispatch(() => RoomCharactersReceived?.Invoke(players)));

            _connection.On<string>("CharacterGathering", name =>
                Dispatch(() => CharacterGathering?.Invoke(name)));

            _connection.On<string>("CharacterCrafting", name =>
                Dispatch(() => CharacterCrafting?.Invoke(name)));

            _connection.On<string>("CharacterUpgrading", name =>
                Dispatch(() => CharacterUpgrading?.Invoke(name)));

            _connection.On<string>("CharacterInCombat", name =>
                Dispatch(() => CharacterInCombat?.Invoke(name)));

            _connection.On<string>("CharacterCombatEnded", name =>
                Dispatch(() => CharacterCombatEnded?.Invoke(name)));

            _connection.On<string, string>("PartyInvite", (from, partyId) =>
                Dispatch(() => PartyInviteReceived?.Invoke(from, partyId)));

            _connection.On<List<string>, string?>("PartyUpdated", (members, leader) =>
                Dispatch(() => PartyUpdated?.Invoke(members, leader)));

            _connection.On("PartyDisbanded", () =>
                Dispatch(() => PartyDisbanded?.Invoke()));

            _connection.On("KickedFromParty", () =>
                Dispatch(() => KickedFromParty?.Invoke()));

            _connection.On<string, int, int, int, int, int>("PartyMemberStats",
                (user, hp, maxHp, mana, maxMana, level) =>
                    Dispatch(() => PartyMemberStatsUpdated?.Invoke(user, hp, maxHp, mana, maxMana, level)));

            _connection.On<StartGroupCombatResult>("GroupCombatStarted", result =>
                Dispatch(() => GroupCombatStarted?.Invoke(result)));

            _connection.On<GroupCombatSnapshot>("GroupCombatUpdated", snap =>
                Dispatch(() => GroupCombatUpdated?.Invoke(snap)));

            _connection.On<GroupCombatSnapshot>("GroupCombatFinished", snap =>
                Dispatch(() => GroupCombatFinished?.Invoke(snap)));

            _connection.On<int, string>("CharacterRespawned", (roomId, roomName) =>
                Dispatch(() => CharacterRespawned?.Invoke(roomId, roomName)));

            // ── Guild events ──────────────────────────────────────────────────
            _connection.On<string, string>("GuildMemberOnline",  (n, r)     => Dispatch(() => GuildMemberOnline?.Invoke(n, r)));
            _connection.On<string>        ("GuildMemberOffline", n          => Dispatch(() => GuildMemberOffline?.Invoke(n)));
            _connection.On<string, string>("GuildMemberJoined",  (n, r)     => Dispatch(() => GuildMemberJoined?.Invoke(n, r)));
            _connection.On<string, bool>  ("GuildMemberLeft",    (n, kicked) => Dispatch(() => GuildMemberLeft?.Invoke(n, kicked)));
            _connection.On<string, string>("GuildRankChanged",   (n, r)     => Dispatch(() => GuildRankChanged?.Invoke(n, r)));
            _connection.On<string, string>("GuildLeaderChanged", (o, nu)    => Dispatch(() => GuildLeaderChanged?.Invoke(o, nu)));
            _connection.On<string>        ("GuildRookieAdded",   n          => Dispatch(() => GuildRookieAdded?.Invoke(n)));
            _connection.On              ("GuildDisbanded",                () => Dispatch(() => GuildDisbanded?.Invoke()));
            _connection.On              ("GuildLeft",                     () => Dispatch(() => GuildLeft?.Invoke()));
            _connection.On              ("GuildKicked",                   () => Dispatch(() => GuildKicked?.Invoke()));
            _connection.On<int, int, string?, string?, bool>("GuildInviteReceived",
                (iid, gid, gn, gt, rk) => Dispatch(() => GuildInviteReceived?.Invoke(iid, gid, gn, gt, rk)));
            _connection.On<string>("GuildError", msg => Dispatch(() => GuildError?.Invoke(msg)));

            _connection.On<List<string>>("RoomShops", ownerNames =>
                Dispatch(() => RoomShopsReceived?.Invoke(ownerNames)));

            _connection.On<string>("ShopOpened", ownerName =>
                Dispatch(() => ShopOpened?.Invoke(ownerName)));

            _connection.On<string>("ShopClosed", ownerName =>
                Dispatch(() => ShopClosed?.Invoke(ownerName)));

            _connection.On<List<ShopListingVm>>("MyShopUpdated", items =>
                Dispatch(() => MyShopUpdated?.Invoke(items)));

            _connection.On<string, string, int, long, long>("ShopSale", (buyer, itemId, qty, paid, fee) =>
                Dispatch(() => ShopSale?.Invoke(buyer, itemId, qty, paid, fee)));

            _connection.On<bool, string, string, int, long>("ShopBuyResult", (ok, err, itemId, qty, paid) =>
                Dispatch(() => ShopBuyResult?.Invoke(ok, err, itemId, qty, paid)));

            _connection.On<string>("ShopError", error =>
                Dispatch(() => ShopErrorReceived?.Invoke(error)));

            // Generic server-authoritative character sync (inventory/gold/hp/mp/progress) - applied
            // directly to the live character here (so it takes effect even if no page happens to be
            // subscribed to CharacterUpdated right now), then re-raised for any page that wants to
            // react explicitly (e.g. flashing a "+5 gold" toast).
            _connection.On<CharacterUpdateDto>("CharacterUpdated", update => Dispatch(() =>
            {
                UserAccountService.CurrentCharacter?.ApplyCharacterUpdate(update);
                CharacterUpdated?.Invoke(update);
            }));

            _connection.Reconnected += _ =>
            {
                Dispatch(() => HubConnected?.Invoke());
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                Dispatch(() => HubConnected?.Invoke());
            }
            catch { /* hub unavailable — game continues in single-player mode */ }
        }

        public static async Task DisconnectAsync()
        {
            var conn = _connection;
            if (conn is null) return;
            _connection = null;
            try { await conn.StopAsync(); } catch { }
            await conn.DisposeAsync();

            // Restore mod data now that the multiplayer session has ended.
            Myria.Lib.Core.Systems.Mods.ModLoader.ApplyMultiplayerMode(false);
        }

        public static async Task SetCharacterNameAsync(string characterName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("SetCharacterName", characterName); } catch { }
        }

        public static async Task<bool> JoinRoomAsync(int roomId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("JoinRoom", roomId); } catch { }
            return false;
        }

        public static async Task SendMessageAsync(string message, string channel = "room", string? target = null)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("SendMessage", message, channel, target); } catch { }
        }

        // ── Party ─────────────────────────────────────────────────────────────

        public static async Task InviteToPartyAsync(string username)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("InviteToParty", username); } catch { }
        }

        public static async Task AcceptPartyInviteAsync(string partyId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("AcceptPartyInvite", partyId); } catch { }
        }

        public static async Task DeclinePartyInviteAsync(string partyId, string fromUsername)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("DeclinePartyInvite", partyId, fromUsername); } catch { }
        }

        public static async Task LeavePartyAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("LeaveParty"); } catch { }
        }

        // ── Trade ─────────────────────────────────────────────────────────────

        public static async Task ProposeTradeAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("ProposeTrade", targetName); } catch { }
        }

        public static async Task AcceptTradeAsync(string tradeId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("AcceptTrade", tradeId); } catch { }
        }

        public static async Task DeclineTradeAsync(string tradeId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("DeclineTrade", tradeId); } catch { }
        }

        public static async Task AddTradeItemAsync(string itemId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("AddTradeItem", itemId, quantity); } catch { }
        }

        public static async Task RemoveTradeItemAsync(string itemId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("RemoveTradeItem", itemId); } catch { }
        }

        public static async Task SetTradeGoldAsync(long amount)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("SetTradeGold", amount); } catch { }
        }

        public static async Task ConfirmTradeAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("ConfirmTrade"); } catch { }
        }

        public static async Task CancelTradeAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("CancelTrade"); } catch { }
        }

        public static async Task KickFromPartyAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("KickFromParty", targetName); } catch { }
        }

        public static async Task TransferPartyLeaderAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("TransferPartyLeader", targetName); } catch { }
        }

        public static async Task UpdatePartyStatsAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("UpdatePartyStats"); } catch { }
        }

        // ── Group Combat ─────────────────────────────────────────────────────

        public static async Task<StartGroupCombatResult?> StartGroupCombatAsync(int roomId, bool soloOnly = false)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<StartGroupCombatResult>("StartGroupCombat", roomId, soloOnly); } catch { }
            return null;
        }

        public static async Task<GroupCombatSnapshot?> GroupCharacterAttackAsync(int targetMonsterIndex)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<GroupCombatSnapshot>("GroupCharacterAttack", targetMonsterIndex); } catch { }
            return null;
        }

        public static async Task<GroupCombatSnapshot?> GroupCharacterCastSkillAsync(string skillId, int targetIndex)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<GroupCombatSnapshot>("GroupCharacterCastSkill", skillId, targetIndex); } catch { }
            return null;
        }

        /// <summary>On-demand resync for a group fight - null means the server doesn't think the
        /// caller is in an active one (already ended, or the local turn belief was simply wrong).</summary>
        public static async Task<GroupCombatSnapshot?> GetGroupCombatStateAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<GroupCombatSnapshot?>("GetGroupCombatState"); } catch { }
            return null;
        }

        // ── Shop ─────────────────────────────────────────────────────────────

        public static async Task OpenShopAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("OpenShop"); } catch { }
        }

        public static async Task CloseShopAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("CloseShop"); } catch { }
        }

        public static async Task<List<ShopListingVm>> GetMyShopAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<List<ShopListingVm>>("GetMyShop"); } catch { }
            return new();
        }

        public static async Task<bool> DepositShopItemAsync(string itemId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("DepositShopItem", itemId, quantity); } catch { }
            return false;
        }

        public static async Task<int> WithdrawShopItemAsync(string itemId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<int>("WithdrawShopItem", itemId, quantity); } catch { }
            return 0;
        }

        public static async Task SetShopItemPriceAsync(string itemId, long price)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("SetShopItemPrice", itemId, price); } catch { }
        }

        public static async Task UnlistShopItemAsync(string itemId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("UnlistShopItem", itemId); } catch { }
        }

        public static async Task<List<ShopListingVm>> BrowseShopAsync(string ownerName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<List<ShopListingVm>>("BrowseShop", ownerName); } catch { }
            return new();
        }

        public static async Task BuyFromShopAsync(string ownerName, string itemId, int qty)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("BuyFromShop", ownerName, itemId, qty); } catch { }
        }

        // ── Session ───────────────────────────────────────────────────────────

        public static async Task<bool> LoadCharacterOnServerAsync(string characterName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("LoadCharacter", characterName); } catch { }
            return false;
        }

        public static async Task SaveSessionAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("SaveSession"); } catch { }
        }

        // ── NPC Shop ──────────────────────────────────────────────────────────

        public static async Task<List<string>?> GetNpcShopItemsAsync(string npcId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<List<string>>("GetNpcShopItems", npcId); } catch { }
            return null;
        }

        public static async Task<NpcShopBuyResult?> BuyFromNpcShopAsync(string npcId, string itemId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<NpcShopBuyResult>("BuyFromNpcShop", npcId, itemId, quantity); } catch { }
            return null;
        }

        public static async Task<NpcSellResult?> SellItemToNpcAsync(string itemId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<NpcSellResult>("SellItemToNpc", itemId, quantity); } catch { }
            return null;
        }

        // ── Inventory ─────────────────────────────────────────────────────────

        public static async Task<EquipItemResult> EquipItemAsync(string itemId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<EquipItemResult>("EquipItem", itemId); } catch (Exception ex) { return new EquipItemResult(false, ex.Message); }
            return new EquipItemResult(false, "Not connected.");
        }

        /// <summary>Mirrors client-side class switching onto the server's session character
        /// (see GameHub.ChangeClass).</summary>
        public static async Task<bool> ChangeClassAsync(string cls)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("ChangeClass", cls); } catch { }
            return false;
        }

        /// <summary>Mirrors client-side active-job switching onto the server's session character
        /// (see GameHub.ToggleJob).</summary>
        public static async Task<bool> ToggleJobAsync(string? jobId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("ToggleJob", jobId); } catch { }
            return false;
        }

        /// <summary>Mirrors client-side rune granting onto the server's session character
        /// (see GameHub.GrantRune).</summary>
        public static async Task<bool> GrantRuneAsync(string baseRuneId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("GrantRune", baseRuneId); } catch { }
            return false;
        }

        /// <summary>Mirrors client-side quest accept/return onto the server's session character
        /// (see GameHub.QuestAction) - without this, quest rewards (including XP) never reach
        /// the server's session copy.</summary>
        public static async Task<bool> QuestActionAsync(string questId, bool isReturn)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("QuestAction", questId, isReturn); } catch { }
            return false;
        }

        /// <summary>Mirrors client-side stat point allocation onto the server's session character
        /// (see GameHub.SyncStatAllocation) - without this the server keeps using login-time stats
        /// for MaxHealth/MaxMana in Heal, combat, etc.</summary>
        public static async Task SyncStatAllocationAsync(int strengthAdded, int dexterityAdded, int enduranceAdded,
            int intelligenceAdded, int spiritAdded, int unusedPoints)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try
                {
                    await _connection.InvokeAsync("SyncStatAllocation",
                        strengthAdded, dexterityAdded, enduranceAdded, intelligenceAdded, spiritAdded, unusedPoints);
                }
                catch { }
        }

        public static async Task<bool> UnequipItemAsync(string slotType)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("UnequipItem", slotType); } catch { }
            return false;
        }

        public static async Task<bool> UseItemAsync(string itemId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("UseItem", itemId); } catch { }
            return false;
        }

        // ── Skills ────────────────────────────────────────────────────────────

        public static async Task<bool> SlotSkillAsync(string source, string skillId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("SlotSkill", source, skillId); } catch { }
            return false;
        }

        public static async Task<bool> UnslotSkillAsync(string source, string skillId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("UnslotSkill", source, skillId); } catch { }
            return false;
        }

        public static async Task<bool> ReorderSkillSlotAsync(int fromIndex, int toIndex)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("ReorderSkillSlot", fromIndex, toIndex); } catch { }
            return false;
        }

        public static async Task<bool> CombineSkillsAsync(List<string> skillIds)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<bool>("CombineSkills", skillIds); } catch { }
            return false;
        }

        public static async Task AbandonCombatAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("AbandonCombat"); } catch { }
        }

        // ── Game actions ──────────────────────────────────────────────────────

        public static async Task<GatherActionResult?> GatherAsync(int roomId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<GatherActionResult>("Gather", roomId); } catch { }
            return null;
        }

        public static async Task<CraftActionResult?> CraftAsync(string npcId, string recipeId, int quantity)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<CraftActionResult>("Craft", npcId, recipeId, quantity); } catch { }
            return null;
        }

        public static async Task<UpgradeActionResult?> UpgradeAsync(string npcId, string itemId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<UpgradeActionResult>("Upgrade", npcId, itemId); } catch { }
            return null;
        }

        public static async Task<StartCombatResult?> StartCombatAsync(int roomId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<StartCombatResult>("StartCombat", roomId); } catch { }
            return null;
        }

        public static async Task<HealActionResult?> HealAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<HealActionResult>("Heal"); } catch { }
            return null;
        }

        public static async Task<CharacterProgressResult?> GetCharacterProgressAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<CharacterProgressResult?>("GetCharacterProgress"); } catch { }
            return null;
        }

        public static async Task<List<QuestProgressState>> GetActiveQuestProgressAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<List<QuestProgressState>>("GetActiveQuestProgress"); } catch { }
            return [];
        }

        public static async Task<CombatTurnResult?> CharacterAttackAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<CombatTurnResult>("CharacterAttack"); } catch { }
            return null;
        }

        public static async Task<CombatTurnResult?> CharacterCastSkillAsync(string skillId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { return await _connection.InvokeAsync<CombatTurnResult>("CharacterCastSkill", skillId); } catch { }
            return null;
        }

        // ── Guild events ──────────────────────────────────────────────────────

        public static event Action<string, string>? GuildMemberOnline;    // name, rank
        public static event Action<string>?         GuildMemberOffline;   // name
        public static event Action<string, string>? GuildMemberJoined;    // name, rank
        public static event Action<string, bool>?   GuildMemberLeft;      // name, wasKicked
        public static event Action<string, string>? GuildRankChanged;     // name, newRank
        public static event Action<string, string>? GuildLeaderChanged;   // oldLeader, newLeader
        public static event Action<string>?         GuildRookieAdded;     // name
        public static event Action?                 GuildDisbanded;
        public static event Action?                 GuildLeft;
        public static event Action?                 GuildKicked;
        public static event Action<int, int, string?, string?, bool>? GuildInviteReceived; // inviteId, guildId, guildName, guildTag, isRookie
        public static event Action<string>?         GuildError;

        // ── Guild hub wrappers ────────────────────────────────────────────────

        public static async Task GuildSendInviteAsync(string targetName, bool asRookie)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildSendInvite", targetName, asRookie); } catch { }
        }

        public static async Task GuildAcceptInviteAsync(int inviteId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildAcceptInvite", inviteId); } catch { }
        }

        public static async Task GuildDeclineInviteAsync(int inviteId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildDeclineInvite", inviteId); } catch { }
        }

        public static async Task GuildLeaveAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildLeave"); } catch { }
        }

        public static async Task GuildKickAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildKick", targetName); } catch { }
        }

        public static async Task GuildPromoteAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildPromote", targetName); } catch { }
        }

        public static async Task GuildDemoteAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildDemote", targetName); } catch { }
        }

        public static async Task GuildTransferLeadershipAsync(string targetName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildTransferLeadership", targetName); } catch { }
        }

        public static async Task GuildDisbandAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildDisband"); } catch { }
        }

        public static async Task GuildHireRookieAsync(string rookieName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildHireRookie", rookieName); } catch { }
        }

        public static async Task GuildPromoteRookieAsync(string rookieName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildPromoteRookie", rookieName); } catch { }
        }

        public static async Task GuildFireRookieAsync(string rookieName)
        {
            if (_connection?.State == HubConnectionState.Connected)
                try { await _connection.InvokeAsync("GuildFireRookie", rookieName); } catch { }
        }

        private static void Dispatch(Action action)
        {
            if (System.Windows.Application.Current?.Dispatcher is { } d)
                d.Invoke(action);
        }
    }
}
