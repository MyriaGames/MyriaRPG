# MyriaRPG — Project ToDo

Legend: ✅ Done | 🔧 In Progress | ⬜ Not Started | ⚠️ Partial / Needs Polish

---
-
## Core Systems

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 1 | Jobs system | ⚠️ Partial | Foundation done; several mechanics still missing — see Jobs detail section below |
| 2 | Quest system | ⚠️ Partial | Core done; several mechanics missing — see Quest detail section below |
| 3 | NPC interaction panels | ✅ Done | Dialog, Shop, Upgrade, Craft, Class, JobMaster, QuestDialog |
| 4 | Inventory & Items | ✅ Done | 200+ items, multi-tab inventory, drag-drop equip |
| 5 | Shop / Crafting / Upgrade | ✅ Done | Buy/sell, recipe crafting, stat upgrades |
| 6 | Skill system | ✅ Done | Learn, slot, combine skills; SkillDetailWindow |
| 7 | Character creation | ⚠️ Partial | Race + naming done; class selection missing — see Class detail section |
| 8 | Character selection / save | ✅ Done | Server-persisted via SqlCharacterRepository |
| 9 | Map / Room navigation | ⚠️ Partial | Navigation done; world map has bugs + missing features — see Map detail section below |
| 10 | Localization (EN / DE) | ✅ Done | Auto-wired via [LocalizedKey] attribute |
| 11 | Theming (Light / Dark) | ✅ Done | Resource-dictionary swap at runtime |
| 12 | Settings persistence | ✅ Done | Visuals, keybindings, language |
| 13 | Combat (singleplayer) | ✅ Done | Client-side encounter simulation |
| 14 | Gathering spots | ✅ Done | GatherService integrated with room system |

---

## Jobs System — Detailed Design

What is already done: 3-aspect XP model (Skill / Knowledge / Fame), level math, 1-week switch cooldown,
Fame sell-value multiplier, Skill gather/craft multiplier formulas, JobMaster NPC panel, XP grant methods.

| # | Item | Status | Notes |
|---|------|--------|-------|
| J1 | Skill XP from crafting/upgrading/gathering (not only active job) | ✅ Done | +50 % bonus when active, always granted |
| J2 | Fame XP only while job is active | ✅ Done | GrantFameXp checks ActiveJobId |
| J3 | Job switch 1-week cooldown | ✅ Done | JobManager.JobChangeCooldown = 7 days |
| J4 | Fame sell-value bonus | ✅ Done | JobManager.GetSellValue applies fame multiplier |
| J5 | Skill multiplier formula | ✅ Done | JobXpService.GetSkillMultiplier / ApplyGatherMultiplier |
| J6 | Knowledge gather-limit and upgrade-limit formulas | ✅ Done | GetGatherLimitBonus / GetMaxUpgradeLevel in JobXpService |
| J7 | Quest entity: `RequiredActiveJobId` field | ✅ Done | Added to Quest entity + Clone(); QuestManager.CanAccept checks it (J8 combined) |
| J8 | Quest availability filtering by active job | ✅ Done | QuestManager.CanAccept rejects quest if player.ActiveJobId ≠ RequiredActiveJobId; both WPF and console use QuestManager |
| J9 | Knowledge quest reward: grant Knowledge XP to matching job | ✅ Done | `JobKnowledgeRewardJobId/Amount` on Quest; Quest.GrantRewards calls JobManager.GrantKnowledgeXp — applies in both WPF and console |
| J10 | Fame quest reward: grant Fame XP to matching job | ✅ Done | `JobFameRewardJobId/Amount` on Quest; Quest.GrantRewards calls JobManager.GrantFameXp (active-job guard is internal) |
| J11 | Daily Fame tick (passive while active) | ✅ Done | `CharacterJob.LastFameTickDay`; JobManager.ApplyDailyTicks grants +5 FameXp once per game-day to active job; DayAdvanced wired in WPF App.xaml.cs + Console Game.cs |
| J12 | Skill decay: slow daily loss if not used that day | ✅ Done | `CharacterJob.LastSkillUsedDay` stamped in GrantSkillXp; ApplyDailyTicks subtracts up to 50 XP daily if not used, never below level floor |
| J13 | Knowledge decay: XP toward next level drains to 0 (no level loss) | ✅ Done | ApplyDailyTicks sets KnowledgeXp = TotalXpToReach(currentLevel) each day |
| J14 | Fame decay: XP and levels both decay when not active | ✅ Done | ApplyDailyTicks subtracts ~0.5 % FameXp daily (min 1) when job is not active; can reduce level |
| J15 | Wire Skill multiplier into crafting output | ✅ Done | WPF CraftLocal + server already done; console CraftMenu rewritten to use CraftingService + apply CraftQuality via GetSkillMultiplierFromXp |
| J16 | Wire Skill multiplier into upgrade output | ✅ Done | WPF UpgradeLocal + server already done; console UpgradeMenu now applies skill quality before TryUpgrade and uses npc.MasterJobId |
| J17 | Wire Skill multiplier into gather output | ✅ Done | GatherService.Gather applies ApplyGatherMultiplier to item.StackSize and grants 10 Skill XP; server already done |
| J18 | Wire Knowledge into gather daily limit | ✅ Done | Room.AddGatherBonus added; WPF + console subscribe to DayAdvanced and apply knowledge bonus each new day + once at session start |
| J19 | Wire Knowledge into upgrade level cap | ✅ Done | WPF UpgradePanelViewModel already uses KnowledgeMaxUpgradeLevel; console UpgradeMenu now uses npc.MasterJobId for knowledge check |
| J20 | Wire Knowledge into crafting recipe unlock | ✅ Done | WPF CraftPanelViewModel already filters; console CraftMenu rewritten to load from CraftingService + filter by knowledge level |
| J21 | Job quest daily repeat cap (max once per day) | ✅ Done | QuestManager.CanAccept already enforces RepeatDailyLimit; data authors must set RepeatDailyLimit = 1 in quests.json for job quests |
| J22 | Multiple job masters per job (data completeness) | ✅ Done | Added NpcType values (Woodcutter/Miner/Herbalist/Alchemist/Cook), locale keys, 5 new NPC entries across all 3 npcs.json files, GatheringMasterMenu in console; code uses MasterJobId fallbacks throughout |
| J23 | Fame sell bonus for player-to-player sales | ✅ Done | `JobManager.GetCharacterSellReceipt(seller, agreedPrice)` added; buyer pays agreedPrice, seller receives agreedPrice × active-job Fame multiplier; MP5/MP6/MP7 must call this when implemented |

---

## Quest System — Detailed Design

What is already done: kill/item objective tracking, multi-page dialog with named speakers (`npc` / `player` / `npc:<id>`),
repeatable quest infrastructure (RepeatRecord, daily/total/level caps), prerequisite chains, XP/gold/item rewards on return,
accept-dialog + return-dialog paging, QuestList page with Active/Available tabs, abandon support.

| # | Item | Status | Notes |
|---|------|--------|-------|
| Q1 | Multi-page dialog with speaker indication | ✅ Done | DialogLine.Speaker resolved by QuestDialogPanelViewModel.ResolveSpeaker |
| Q2 | Repeatable quest infrastructure | ✅ Done | RepeatRecord, daily/total/level limits, QuestManager.CanAccept |
| Q3 | Prerequisite quest chains | ✅ Done | PrerequisiteQuestIds checked in CanAccept |
| Q4 | Talk-only quest auto-complete on accept | ✅ Done | `Quest.IsTalkOnly` computed property added; WPF QuestDialogPanelViewModel + console NpcInteractionHandler both set Status = Completed immediately on accept when no kill/item objectives |
| Q5 | QuestList page: hide non-repeatable quests from Available tab | ✅ Done | `UpdateMode()` Available branch + `AvailableCountLabel` both filter to `q.IsRepeatable`; non-repeatables only accepted in person at NPC |
| Q6 | QuestList page: block returning non-repeatable quests | ✅ Done | `IsRepeatable` added to `QuestListItemVm`; `IsSelectedQuestReturnable` hides button for non-repeatables; `ReturnQuest` command guards with `if (!active.IsRepeatable) return` |
| Q7 | QuestList page: repeatables only shown after first completion | ✅ Done | `HasBeenCompletedBefore()` helper added; gates `AvailableCountLabel`, `UpdateMode()`, `IsSelectedQuestReturnable`, and `ReturnQuest` command — first run always requires the NPC |
| Q8 | Quest entity: `AcceptItems` field (items granted on accept) | ✅ Done | `AcceptItems` added to Quest + Clone(); `Quest.GrantAcceptItems(player)` method added; called in WPF QuestDialogPanelViewModel + console NpcInteractionHandler on accept |
| Q9 | Quest entity: `RequiredActiveJobId` field | ✅ Done | Shared with job system (J7/J8); already implemented — `Quest.RequiredActiveJobId` field + `QuestManager.CanAccept` check |
| Q10 | Quest entity: `RequiresParty` / `RequiredPartySize` field | ✅ Done | Fields added to Quest + Clone(); `QuestManager.CanAccept` checks both; `partySize` param threaded through `GetAvailableForCharacter` + `GetAcceptableForNpc`; console passes `ConsoleHubClient.CurrentPartyMembers.Count` |
| Q11 | Quest entity: job aspect level requirements | ✅ Done | `RequiredAspectJobId`, `RequiredSkillLevel`, `RequiredKnowledgeLevel`, `RequiredFameLevel` added to Quest + Clone(); `QuestManager.CanAccept` checks all three via `JobXpService.GetLevel` |
| Q12 | Quest entity: class requirement | ✅ Done | `RequiredClass` (`CharacterClass?`) added to Quest + Clone(); `QuestManager.CanAccept` checks `player.Class == RequiredClass` |
| Q13 | Quest entity: race requirement | ✅ Done | `RequiredRace` (`CharacterRace?`) added to Quest + Clone(); `QuestManager.CanAccept` checks `player.Race == RequiredRace` |
| Q14 | Quest list groupings (Main / Side / Faction) | ⬜ Not Started | The quest list should support grouping by category (e.g. Main Quests, Side Quests, Faction Quests) as seen in the UI reference. Requires: a `QuestCategory` enum on `Quest` + JSON data, group-header logic in `QuestListPageViewModel`, and a grouped ItemsControl (or `CollectionViewSource`) in `Page_QuestList.xaml`. |

---

## Class System — Detailed Design

What is already done: 12 classes (enum + ClassProfile stat-growth table), class bonuses wired into player derived stats
(`ExtraSTR/DEX/END/INT/SPR`, `ExtraBaseHealth/ExtraBaseMana`), class XP tracked per class in `Character.ClassXp`,
daily XP penalty for inactive classes (`ClassManager.ApplyDailyPenalty`, 500 XP/day), class level + progress bar shown
in the NPC ClassPanel, race-based class restrictions, `SkillFactory.GetSkillsFor` filters by `player.Class`.

| # | Item | Status | Notes |
|---|------|--------|-------|
| CL1 | Class selection in character creation | ✅ Done | `ClassOptionVm` added; race picker → `RebuildClasses()`; class picker XAML section added to `Page_CharacterCreation.xaml` (multiplayer-only, respects `ClassManager.GetAllowedClasses(race)`); preview shows class name |
| CL2 | Class level shown on character sheet | ✅ Done | `ClassLevelLabel`, `ClassXpFraction`, `ClassXpText` added to `CharacterPageViewModel`; header shows "Class Level N" after class name; class XP bar added below player XP bar in `Page_Character.xaml` |
| CL3 | Class switch 1-week cooldown | ✅ Done | `Character.LastClassChanged` added; `ClassManager.CanChangeClass` / `GetClassChangeCooldownRemaining` / `ClassChangeCooldown` added; `SetClass` enforces cooldown + stamps timestamp; WPF ClassPanel shows cooldown notice + greys button; console NpcInteractionHandler checks cooldown with days-remaining message |
| CL4 | Skill removal / hiding on class switch | ✅ Done | `ClassManager.SetClass` now removes all base `Skill` objects belonging to the old class from `player.Skills`, strips `Regular` `SkillSlot` entries that pointed to those skills, then calls `SkillFactory.UpdateSkills` to grant the new class's base skills (level-gated). Combined/Composite slot cleanup is CL5. |
| CL5 | Combined / composite skill cleanup on class switch | ✅ Done | `Character.StashedCombinedSkills` and `Character.StashedCompositeSkills` (`Dictionary<CharacterClass, List<...>>`) added; `ClassManager.SetClass` stashes old-class combined+composite skills, clears `ActiveCompositeSkillIds`, removes their `SkillSlot` entries, then restores previously stashed skills for the new class |
| CL6 | Class groups + partial level transfer on in-group switch | ✅ Done | `ClassGroup` enum added (Physical / RangerRogue / Mage / DivineHybrid); `ClassManager.GetClassGroup()` maps all 12 classes; `SetClass` grants 50% of old class XP to new class when groups match, 0% cross-group; WPF ClassPanel shows group badge per card + transfer info notice |
| CL7 | Separate XP curve for class levels | ✅ Done | `ClassXpService` created (`n × 5000` per level, cap 50); `ClassManager.GetClassLevel` now uses `ClassXpService.GetLevel`; `ClassPanelViewModel.ClassOptionVm` and `CharacterPageViewModel` swapped from `JobXpService` to `ClassXpService` for class XP progress/formatting |
| CL8 | Quest: class requirement field | ✅ Done | Implemented as Q12 |
| CL9 | Quest: race requirement field | ✅ Done | Implemented as Q13 |

---

## Map System — Detailed Design

What is already done: world-map BFS layout, zone group nodes (city/cave/forest/dungeon collapsed), current-room highlight,
pan + zoom (mouse wheel + drag), zone group-view when player is inside a big area, level/quest room gating.

| # | Item | Status | Notes |
|---|------|--------|-------|
| M1 | World map BFS layout | ✅ Done | Direction-based grid, zone collapsing, cascade-correct for bridge corridors |
| M2 | Pan / zoom | ✅ Done | Mouse-wheel zoom around cursor, drag to pan, zoom buttons |
| M3 | Group view for big areas | ✅ Done | When player is inside a zone, shows that zone's rooms with adjacent zone group nodes |
| M4 | Diagonal edges on world map | ✅ Done | Confirmed resolved |
| M5 | Map layout consistency across starting rooms | ✅ Done | Confirmed resolved |
| M6 | Click on group node → open area interior map | ✅ Done | Confirmed resolved |
| M7 | NPC tooltip on node hover | ⚠️ Partial | NpcTooltip string populated on MapNodeVm from room.NpcRefs; ToolTip property set on Border in Redraw(). Tooltip does not appear — WPF ToolTip on Canvas children inside a RenderTransform-scaled layer may need IsHitTestVisible = true on the Canvas or a ToolTipService.ShowOnDisabled workaround; alternatively attach tooltip to the TextBlock label instead of the Border |
| M8 | Background color from theme | ✅ Done | Color.Map.Node.* + Brush.MapBackground keys added to both Light.xaml and Dark.xaml; LocalMapControl.xaml uses DynamicResource Brush.MapBackground; Redraw() resolves node fill colors via TryFindResource with hardcoded fallbacks |

---

## In Progress / Needs Work

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 15 | Multiplayer — server combat validation | ⚠️ Partial | Basic 1v1 combat on server; group combat + full client wiring missing — see MP section |
| 16 | Party system — client UI | ⚠️ Partial | Server party logic complete; no WPF party management page — see MP section |
| 17 | Party-based room gating | ⚠️ Partial | RoomService line 96 TODO — gate is a no-op in singleplayer |
| 18 | Character stat initialization | ⚠️ Partial | ViewmModel_CaracterCreationPage.cs line 185 TODO — using placeholder Stats() instead of class-profile rules |
| 19 | World data — caves / dungeons / forests / cities | ⬜ Sparse | Template stubs only (7–18 lines each); needs full area definitions |
| 20 | Multiplayer — real-time game state sync | ⬜ Not Started | Beyond auth + char save; actual in-game action sync not implemented |
| 21 | Multiplayer — lobby / matchmaking UI | ⬜ Not Started | No WPF page for joining or browsing sessions |

---

## Multiplayer System — Detailed Design

What is already done: JWT auth, character save/load, SignalR hub (GameHub), room presence
(`JoinRoom` / `CharacterEntered` / `CharacterLeft` / `RoomCharacters`), chat channels (global / room / party / whisper),
party create/invite/accept/decline/leave with auto-leader on creation and auto-transfer on leader leave
(`PartyService`), friends send-request/accept/remove REST API (`FriendsController`, `Friendship` table),
server-side 1v1 combat (`StartCombat`, `CharacterAttack`, `CharacterCastSkill`), server-side gathering/crafting/upgrade
with job XP.

**Room presence / social UI**

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP1 | Show players in same room (client UI) | ✅ Done | Server already broadcasts `RoomCharacters` / `CharacterEntered` / `CharacterLeft`; client WPF needs a panel or overlay in the Room page listing current players with their names; this list is also the entry point for direct trade and party invites |
| MP2 | Friend request via chat right-click menu | ✅ Done | Server `POST /api/friends/request` already exists; client chat UI needs a right-click context menu on player names with "Send Friend Request" that calls the API |
| MP3 | Block player — server enforcement | ✅ Done | No `BlockedCharacters` table. Need: DB migration + `Blocked` model, REST endpoints (block/unblock/list), enforce in `GameHub.SendMessage` (whispers), `InviteToParty`, and `FriendsController.SendRequest` so blocked players cannot reach the blocker |
| MP4 | Block player — client UI | ✅ Done | Right-click context menu on player name in chat (alongside friend request): "Block Character"; blocked players' messages hidden client-side as well |

**Trading**

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP5 | Direct player-to-player trade | ✅ Done | `TradeService` state machine (Proposed → Active → BothReady → Completed/Cancelled), hub methods, atomic item/gold swap with J23 fame bonus. WPF: `TradeViewModel` + `Page_Trade.xaml`, room-presence context menu. Console: `TradeCommands.cs`, hub events in `ConsoleHubClient` |
| MP6 | Character shop — server | ✅ Done | `CharacterShopService` singleton (in-memory, open while owner is online). Hub methods: `OpenShop`, `CloseShop`, `AddShopListing`, `RemoveShopListing`, `BrowseShop`, `BuyFromShop`. J23 fame bonus on sales. `ShopOpened`/`ShopClosed` broadcast to room |
| MP7 | Character shop — client UI | ✅ Done | WPF: `Page_CharacterShop.xaml` + `CharacterShopViewModel` (owner/buyer modes). Room context menu "Visit Shop". Console: `ShopCommands.cs` with open/close/list/add/remove/visit/buy. Hub events wired in both clients |

**Party management**

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP8 | Kick player from party | ✅ Done | `PartyService` has no `Kick` method. Add `Kick(leaderName, targetName)` to `PartyService`; add `KickFromParty(targetName)` hub method that verifies caller is the leader, removes target, notifies party. Target receives a `KickedFromParty` event |
| MP9 | Transfer party leader manually | ✅ Done | `PartyService` auto-transfers on leave but has no manual transfer. Add `TransferLeader(partyId, newLeader)` to `PartyService`; add `TransferPartyLeader(targetName)` hub method (leader-only). All members receive `PartyUpdated` with the new leader |
| MP10 | Party management client UI | ✅ Done | No WPF party management page. Needs a panel (accessible from the ingame menu) showing member list with HP/mana bars (already broadcast via `UpdatePartyStats` / `PartyMemberStats`), leader crown indicator, and buttons: Kick (leader only), Transfer Leader (leader only), Leave |

**Group combat**

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP11 | Multi-entity combat engine (MyriaLib) | ✅ Done | `GroupCombatEncounter`: N players vs M monsters, round-robin turns, all living players get XP, loot to first living player, quest progress for all |
| MP12 | PvE targeting restriction | ✅ Done | `SkillTarget.SingleAlly` added to enum; `GroupCombatEncounter.CharacterCastSkill` routes ally skills to Characters list, enemy skills to Monsters list |
| MP13 | Group combat — server shared encounter | ✅ Done | `GroupCombatService` (singleton) maps partyId → `GroupCombatEncounter` + connId → partyId; hub methods: `StartGroupCombat`, `GroupCharacterAttack`, `GroupCharacterCastSkill`; broadcasts `GroupCombatStarted/Updated/Finished` to party group |
| MP14 | Group combat — client sync | ✅ Done | WPF: `GroupCombatantVm`, group panel in `Page_Fight.xaml`, `IsGroupCombat`/`GroupCharacters`/`GroupMonsters`/`IsMyTurn` in `ViewModel_PageFight`, `StartGroupFightCommand` in `ViewModel_PageGame`, "Group Fight" button in `Page_Game.xaml`; Console: `CombatCommands` group commands (`group fight`, `attack <n>`, `cast <skill> <n>`, `group status`), events wired in `Game.cs` |

**Server-authoritative validation audit (2026-08-08)**

Goal is correctness/desync fixes, not anti-cheat hardening. Audited every public `GameHub.cs` method against both clients.
Solid (server computes, both clients synced, no action needed): `Heal`, `JoinRoom`, `SendMessage`, `Gather`, `Craft`, `Upgrade`,
`BuyFromNpcShop`/`SellItemToNpc` (WPF only — see MP15), `StartCombat`/`CharacterAttack`/`CharacterCastSkill`, all `GroupCombat*`,
all Party methods, Trade (`ConfirmTrade` re-validates both sides at swap time), Character Shop, Guild (server side, DB-backed).

Two independent tracks — Track A (console parity) is mechanical and high-value; Track B (mirror-method authority) is a design
decision about how much server-side re-verification the alpha needs yet.

*Track A — console client parity*

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP15 | Console: NPC shop / guild hub wiring | ✅ Done | `ConsoleHubClient` gained `BuyFromNpcShopAsync`/`SellItemToNpcAsync` (wired into `NpcInteractionHandler.TryBuy`/`SellMenu`, await-then-gate) and full `Guild*Async` wrappers + event listeners; new `GuildCommands.cs` (invite/inviterookie/accept/decline/leave/kick/promote/demote/transfer/disband/hire/promoterookie/fire) registered in `CommandRouter`. Guild had no console concept at all before — this is a new feature, not just wiring. Builds clean. **Needs a live two-session test** (see verification note below) |
| MP16 | Console: stats/equipment/skills hub wiring | ✅ Done | `ConsoleHubClient` gained `SyncStatAllocationAsync`, `EquipItemAsync`, `UnequipItemAsync`, `UseItemAsync`, `SlotSkillAsync`/`UnslotSkillAsync`/`ReorderSkillSlotAsync`/`CombineSkillsAsync`, wired into `PlayerCommands.AllocStat` (fire-and-forget) and `InventoryCommands`/`SkillCommands` (await-then-gate for equip/unequip/use/slot, fire-and-forget for unslot/reorder/combine since server does no independent validation there). Builds clean |
| MP17 | Console: quest/job/class/heal/session hub wiring | ✅ Done | `ConsoleHubClient` gained `QuestActionAsync`, `ToggleJobAsync`, `ChangeClassAsync`, `HealAsync`, `SaveSessionAsync`, `AbandonCombatAsync`, wired into `NpcInteractionHandler` (quest accept/return, job toggle, class change, healer — heal now applies server-authoritative HP/mana from `HealActionResult`) and `Game.cs`/`PlayerCommands.cs` exit/logout paths. Rune wiring explicitly deferred per decision (rune-drawing class is deactivated, no local console flow exists to attach it to). `AbandonCombat` wrapper added but left unwired — no flee/abandon command exists in `CombatCommands.cs` yet; needs a minimal command added when that UI is built. Builds clean |

*Track B — mirror-method server authority*

| # | Item | Status | Notes |
|---|------|--------|-------|
| MP18 | `SyncStatAllocation` point-budget validation | ⬜ Not Started | `GameHub.cs:723` — server assigns whatever stat values the client sends verbatim; doc comment explicitly notes this is a conscious "fine for a friends-only alpha" tradeoff. Add level-derived point-budget check if/when this stops being acceptable |
| MP19 | `QuestAction` objective re-verification | ⬜ Not Started | `GameHub.cs:744` — checks the quest is legitimately in `ActiveQuests` before granting rewards, but never re-checks that kill/item objectives were actually completed; server trusts the client's claim the quest is done |
| MP20 | `GrantRune` cost/cooldown check | ⬜ Not Started | `GameHub.cs:796` — delegates straight to `BaseRuneService.GrantBaseRune` with no visible server-side cost or cooldown check in the hub; needs verifying inside `BaseRuneService` itself |
| MP21 | `EquipItem`/`UnequipItem` stat re-derivation | ⬜ Not Started | Ownership/usability is checked, but equipped-stat totals aren't independently re-derived server-side after the swap — same documented trust-the-client tradeoff as MP18 |

**Verification note (MP15–MP17, 2026-08-08):** All changes verified by clean `dotnet build` after each part
(`MyriaRpgConsole.csproj`, 0 errors) — new call sites follow the exact same `IsConnected`-gated
`InvokeAsync<T>` pattern already proven in production for `Craft`/`Gather`/`Upgrade`. A live two-client
end-to-end run (register test account, create character, exercise each action, confirm server-side
persistence via reconnect, verify guild broadcasts land on a second session) was attempted but not
completed — the console's interactive login/character-creation prompt sequence didn't script cleanly via
piped stdin in the time available, and further scripting was descoped rather than burning excessive time
on harness fragility. **Recommend a manual live pass** (run `MyriaServer` + two `MyriaRpgConsole`
instances) before flipping this off the radar for good, particularly for: guild invite/accept across two
sessions, quest return after a fresh reconnect, and the healer's new server-authoritative HP/mana apply.

---

## Polish / Bugs / Cleanup

| # | Item | Status | Notes |
|---|------|--------|-------|
| 22 | Rename `ViewmModel_CaracterCreationPage.cs` | ⬜ Not Started | Typo: "Viewm" + "Caractor" in filename & class namespace |
| 23 | Rename `QuestListPageViewModelcs.cs` | ⬜ Not Started | Typo: ".cs" embedded inside filename |
| 24 | Skill usage during combat | ⚠️ Partial | SkillSlot system exists; combat skill-selection UI incomplete |
| 25 | Multiplayer — anti-cheat / server authority | ⚠️ Partial | Stale/superseded by the MP-section audit below: combat itself is already server-authoritative (`CombatEncounter`/`GroupCombatEncounter` run server-side, `GameHub` returns the results). The real remaining gaps are the specific trust-the-client methods tracked as MP18–MP21 (stat allocation, quest objective verification, rune grant, equip stat re-derivation) — see the Multiplayer System section. |
| 26 | Localisation for everything | ⚠️ Partial | Localisation is missing mainly in console but also here and there in wpf |
| 27 | NPC shop — sell doesn't reach server in multiplayer | ✅ Done | Fixed via uncommitted working-tree changes found already in progress: `MultiplayerInventoryGridViewModel.SellAmount` now overrides to call `GameHubService.SellItemToNpcAsync` before applying the local sell, and `ShopPanel.xaml.cs` now instantiates `MultiplayerInventoryGridViewModel` (not the base) when `GameHubService.IsConnected`. Verified `MyriaRPG.csproj` builds clean. Known minor follow-up: after server success it still applies the local sell via `base.SellAmount` → `Inventory.SellItem` (re-derives price via `JobManager.GetSellValue` rather than using `result.TotalGain` directly like the buy path does) — kept as-is because bypassing `SellItem` would also skip the `ItemSold` event that feeds the buyback list; not a correctness bug since both sides use the same formula, just duplicated computation. Confirmed working by manual WPF click-test (buy and sell both verified in a live multiplayer session). |
| 28 | Character selection — new character missing after creation | ⬜ Not Started | Creating a character then returning to the character-selection screen (leaving the session) doesn't show the new character — client doesn't re-fetch the character list on navigating back to `Page_CharacterSelection`, it's using a stale list from before creation |
| 29 | Character-per-account-per-realm cap (max 5) | ⬜ Not Started | No cap currently enforced anywhere. Needs: server-side check (reject character creation past 5 for that account+realm) and the same cap in singleplayer (no server, so must be enforced client-side against local save data) |
| 30 | Realm selection navigation — wrong target / login page full-screen | ⬜ Not Started | From realm selection, navigation goes back to the login page directly instead of to the startup menu (which is where the login page is supposed to be opened from as a sub-view); as a result the login page currently opens full-screen instead of hosted inside the startup menu frame |
| 31 | Console — split singleplayer / multiplayer login (mirrors WPF) | ✅ Done | Console previously conflated the two: `Login`/`Register` read/wrote a local `Data/users/{username}.json` file *and* opportunistically also tried a server login on top — registration was effectively broken for multiplayer since the account it created never existed server-side. Rewired to match WPF's architecture: new `LoginManager.PlaySingleplayer()` (menu option 1) uses a single hardcoded `Data/users/localUser.json`, no server call at all, straight into character selection — mirrors `ViewModel_StartupMenuPage.SingleCharacterAction`. `Login()`/`Register()` (menu options 2/3) now always talk to the server only (`ConsoleHubClient.LoginAsync`/new `RegisterAsync`), never touch a local account file — mirrors `ViewModel_LoginPage`/`ViewModel_RegisterPage`. Character select/create menu extracted into shared `SelectOrCreateCharacter()`, branches on `ConsoleHubClient.HasToken` exactly as before. Verified live end-to-end against a running `MyriaServer`: register → login → create character (race/class selection, previously impossible — see item 33) → play → exit all worked; console still has no realm/lobby-selection concept (single fixed `BaseUrl`) — out of scope, WPF-only concept per item 30. |
| 32 | `ConsoleHubClient.SaveCharacterAsync`/`LoadCharacterAsync` send/expect the wrong request shape | ✅ Done | Pre-existing bug, unrelated to item 31's login rewrite — found during live multiplayer testing of item 31 and fixed as a follow-up. Console wrapped the character as `{name, level, experience, currentRoomId, dataJson: "<self-serialized blob>"}`, but the server's real `SaveCharacterRequest`/`CharacterLoadResponse` DTOs (`MyriaServer/Models/Dto/`) are fully flat (`class`, `race`, `statStrength`, `inventoryItems`, `skillIds`, etc. as top-level fields, no `dataJson`). `SaveCharacterAsync` was silently losing everything except `name`/`level`/`experience`/`currentRoomId`; `LoadCharacterAsync` always returned `null`. Fixed by porting WPF's `ServerApiService.cs` field-by-field mapping (`SaveCharacterAsync`/`LoadCharacterAsync`, `BuildCompositeSkillDtos`/`BuildCombinedSkillDtos`) into `ConsoleHubClient.cs`: added the full mirror DTO set (`CharacterSaveDto`/`CharacterLoadDto` + 11 nested DTOs) matching the server's shape exactly, rewrote both methods to map every field (stats, equipment, money, inventory, quests, jobs, skill slots, composite/combined skills incl. stashed-for-class, runes, room gathering, class XP), and added the two post-load resolution calls console was missing (`BaseRuneService.ResolveRunes`, `SkillFusionSystem.ResolveCompositeSkills`). Removed the now-unused `_playerOpts`/`ItemConverter` machinery that only existed for the old blob-serialization approach. **Verified live end-to-end**: created a character (Rotuka Knight), saved, exited, reconnected, and reloaded it — stats/class/race all came back correctly instead of the previous crash/null. |
| 33 | Console `Data/common/` was missing `races.json`/`classes.json` entirely | ✅ Done | Discovered while testing item 31 — character creation crashed into an infinite "Select race (1-0)" loop because `RaceProfile.All`/`ClassProfile.All` were empty. WPF/`MyriaServer` get these files by linking `MyriaLib/Data/common/**` directly in their `.csproj`; `ConsoleWorldRPG` instead maintains its own hand-copied `Data/common/` file set, and these two were never added to it (unlike `caves.json`/`items.json`/etc., which were). Fixed by copying `races.json`+`classes.json` from `MyriaLib/Data/common/` into `ConsoleWorldRPG/Data/common/` and adding matching `<Content Include>` entries to `MyriaRpgConsole.csproj`. Worth auditing the rest of `ConsoleWorldRPG/Data/common/` against `MyriaLib/Data/common/` for further silent gaps — not done exhaustively here, only the two that blocked this test were found/fixed. |
| 34 | Console: register-failure message flashed past unnoticed; password shown in plain text | ✅ Done | User-reported: registering with an already-taken username "behaved as if everything had worked fine" — live-tested and the error message (`❌ That username already exists.`) was actually printing correctly, but `Register()` returned straight to the login menu redraw with no pause, so the message could scroll past unnoticed in a real terminal. Added `Press Enter to continue...` after the result message. Also fixed a real, separate gap found while in this code: password entry used plain `Console.ReadLine()`, echoing the typed password in cleartext. Added a `ReadPassword()` helper (`LoginManager.cs`) using `Console.ReadKey(intercept:true)` to mask input with `*` (with backspace support), falling back to plain `ReadLine` when input is redirected/piped (masking is impossible there anyway, and `ReadKey` throws if there's no real console). Wired into both `Register()` and `Login()`. |
| 35 | WPF: monster-drop loot not applied to client inventory until relog | ✅ Done | User-reported: after winning a fight, dropped items don't appear in the inventory UI or the item-received log line, though the drop genuinely happened server-side (visible after relog). First fix attempt targeted the wrong path (`CombatTurnResult`/`ApplyCombatTurnResult`, the old solo `StartCombatAsync` route) — turned out the room page always fights via `StartGroupCombatAsync(roomId, soloOnly: true)` (`ViewModel_PageRoomMultiplayer.cs:23`), so solo and party fights both go through **group combat**, whose DTO (`GroupCombatSnapshot`) had no loot field at all. Real fix: added `GroupCombatEncounter.LootGrantedByCharacter` (mirrors the existing `XpGrantedByCharacter` dict, populated in `HandleMonsterDeath` alongside the existing server-side `Inventory.AddItem` call); added `LootItemIds` to the `GroupCombatantState` DTO (`GameActionResults.cs`); `GameHub.BuildCharacterStates` now populates it from the encounter; `ViewModel_PageFightMultiplayer.ApplyServerXpGain` (the group-combat-finished handler) now resolves each id via `ItemFactory.CreateItem` and calls `character.Inventory.AddItem(...)`, same pattern as the XP mirroring right below it. The original solo-path fix in `ApplyCombatTurnResult` was left in place too (harmless, correct if that path is ever reached from elsewhere). **Confirmed working by manual WPF click-test.** Known follow-up, not fixed here: both the solo and group loot lists are built from `Dictionary<string,int>` (item id → quantity) via `.Keys`/`.Add(drop.Id)`, discarding stack counts — a kill dropping 2+ of the same item only transmits/grants 1 client-side (server-side inventory still gets the correct count); a wire-protocol precision loss, not a correctness bug for today's typical single-drop loot tables, but worth fixing properly later. |
| 36 | Auto-update download races when two app instances overlap | ✅ Done | User reported "install didn't do anything" after 0.2.11 released — `Data/Misc/update.log` on the installed machine showed two `Checking for updates...` entries logged within the same second, both `UpdateService.CheckForUpdatesAsync` calls downloading to the exact same shared temp path (`%TEMP%\MyriaRPG_Setup_{version}.exe`) and racing: the second `File.Create` throws `IOException` ("used by another process"), aborting that check; the first attempt's own log trail then goes silent with no completion/failure line either (consistent with the app being closed mid-download, e.g. an impatient relaunch). Root cause is almost certainly **no single-instance guard** in `App.xaml.cs` — nothing stops two `MyriaRPG.exe` processes running at once (a stray double-launch, or relaunching before a previous instance has fully exited), and each independently runs its own startup-time update check against the same shared file. Same failure mode reproduced identically back on the 0.2.6→0.2.7 update (see log history) — not new, just newly noticed. Fixed the crash symptom: `UpdateService.cs` now suffixes the temp download path with `Environment.ProcessId`, so two concurrent checks (from any cause) can never collide on the same file. **Not fixed**: the actual root cause (no single-instance enforcement) — flagged for the user to decide whether it's worth adding, since it's a bigger behavioral change (would need to define what a second launch attempt does: focus the existing window? refuse to start? forward args?). This fix ships in the next release; anyone still on 0.2.10/0.2.11 needs either a successful non-overlapping auto-update or a manual install of the release that contains it. |
| 37 | Auto-update: visible pre-launch check window + cross-instance coordination | ✅ Done | Follow-up to item 36 — the update check ran silently after `MainWindow` was already shown, and while item 36 fixed the temp-path *crash*, it didn't stop overlapping checks from happening at all. Added: `Services/UpdateCoordinator.cs` (new) — a session-local named `Mutex("MyriaRPG_UpdateCheck")`; whichever instance acquires it immediately becomes the "leader" and performs the real check, any other instance ("follower") waits up to 90s (via `Task.Run`-wrapped `WaitOne` so it never blocks the UI thread) for the leader to finish and then just proceeds — no duplicate network hit/download, no race. Handles `AbandonedMutexException` (a crashed leader) as a successful acquire so it can never permanently wedge future launches. `UpdateService.CheckForUpdatesAsync` now takes an `IProgress<UpdateProgress>` and reports `Checking`/`Downloading` (with real percent via a manual buffered-copy loop against `Content.Headers.ContentLength`, replacing the old fire-and-forget `GetStreamAsync`)/`LaunchingInstaller`/`UpToDate`/`Failed`, and returns `bool` (installer launched) so the caller knows whether to keep going. New `View/Windows/UpdateCheckWindow.xaml(.cs)` — small themed window (reuses `Brush.Background`/`Brush.GoldSoft`) shown for both leader and follower for consistent UX (follower just shows the same "checking" spinner while it waits, since it doesn't know the leader's real progress). `App.xaml.cs`: `OnStartup` is now `async void` so the flow can be awaited without freezing the UI; settings/localization/theme/asset-dictionary loading already ran earlier in the existing step order so the window can show real localized/themed text; the update flow runs after those steps but before race/class/rune/job data loading and `MainWindow` creation. Had to add `ShutdownMode = ShutdownMode.OnExplicitShutdown` around the window (default `OnLastWindowClose` would otherwise shut the whole app down when the update window closes, since `MainWindow` doesn't exist yet) — restored to `OnLastWindowClose` right after `MainWindow.Show()`. New localization keys `update.checking`/`update.downloading`/`update.restarting` (EN+DE, `MyriaLib/Data/locales/`). **Not in scope** (flagged, same as item 36): true single-instance *enforcement* — two full game sessions can still run side-by-side; this only prevents the update-check itself from racing. Confirmed working by manual WPF launch test before the v0.2.12 release that shipped it. |

---

## Balance & Rebalancing

| # | Item | Status | Notes |
|---|------|--------|-------|
| BA1 | XP scaling by level gap | ✅ Done | `Monster.Level` field added; monsters.json assigns levels 1–20 by stat tier; `CombatEncounter` + `GroupCombatEncounter` apply `scaledXp = max(1, baseXp × (monsterLevel/playerLevel)²)` when player outlevels the monster |
| BA2 | Skill damage ignores defense | ✅ Done | `ExecuteSkillOnEnemy` / `ExecuteSkillOnMonster` now apply `raw*(raw/(raw+def))` mitigation (physical or magic defense chosen by `skill.Type`), matching the `CombatSystem.CalculateDamage` formula; minimum 1 damage |
| BA3 | Skill scaling factor audit | ✅ Done | All `ScalingFactor` values in skills.json raised to compensate for the new mitigation pass: offensive skills ~1.5×–2.2×, AoE skills 1.0×–1.2×, heals 1.0×–1.3× |
| BA4 | Character damage too high / defense too low | ✅ Done | `ClassProfile` stat growth per class level trimmed ~20–30 % across all 12 classes (e.g. Fighter STR 5→4, Barbarian STR 8→6, ArcanMage INT 8→6); HP/mana-per-class-level reduced proportionally |

---

## Content & Lore

| # | Item | Status | Notes |
|---|------|--------|-------|
| CO1 | Introductory quests (level 1–10) explaining mechanics | ✅ Done | 7-quest singleplayer intro chain added to quests.json (co1_first_day → co1_gather_ore → co1_first_ironwork → co1_goblins → co1_skill_intro → co1_troll → co1_trusted). Teaches: NPC interaction, gathering, crafting, combat, skills, harder combat. Giver NPCs: smith_default (Gorhen) and villager_lumina (Mira). Full dialog drafted for all quests |
| CO2 | Quests level 10 and beyond | ✅ Done | 11 new quests added (L10–36): ceralith_spirit_hunters, valley_wolves, treant_blight, bandit_pass, ceralith_cellar, woods_rangers, hollow_awakening, void_tide, harpy_crossings, ancient_guardians, crimson_hunt. Chain: lizzard_raids_1 → valley_wolves → treant_blight → bandit_pass → ceralith_cellar → woods_rangers → hollow_awakening → void_tide → harpy_crossings → ancient_guardians → crimson_hunt. Plus side quest ceralith_spirit_hunters (branching from lizzard_raids_1). Givers: villager_ceralith, villager_lumina, villager_default |
| CO3 | More monsters | ✅ Done | 15 new monsters added (IDs 18–32, L10–40+): Shadow Wolf, Withered Treant, Bandit Marauder, Plague Rat, Dread Archer, Hollow Knight, Void Wraith, Storm Harpy, Elder Golem, Crimson Drake (IDs 18–27) + Forest Sprite, Giant Beetle, Dire Wolf, Corrupted Dryad, Forest Witch (IDs 28–32). Room monster tables populated for rooms 14, 19–27; rooms 21–26 updated with forest monsters. Access levels adjusted: 19–20 → 13, 21–25 → 15, 26 → 18 |
| CO3b | Forest quest chain + repeatable patrol | ✅ Done | 6 forest quests added: forest_infestation, beetle_problem, dire_wolves, corrupted_heart, witch_coven (story chain) + whispering_woods_patrol (daily repeatable, L15+). NPC woodcutter_master placed in room 25 (whisperingwoods.lumina/sawmill) to give/receive these quests |
| CO3c | Job knowledge/fame quest chains | ✅ Done | 13 job-gated quests added across 4 chains: Blacksmith (job_smith_basics/forged/armored/mastery), Leathersmith (job_leather_basics/workshop/hunter), Miner (job_miner_first_haul/deep_vein/motherlode), Woodcutter (job_woodcutter_first_timber/lumber_run/forest_supply). All use `RequiredActiveJobId` + reward `JobKnowledgeRewardAmount`/`JobFameRewardAmount`. Sized to reach ~Knowledge L9 by chain end. miner_master placed in room 12 (lumina.north) |
| CO3d | Crafting recipes for all class types | ✅ Done | 12 new recipes added across 4 craft jobs: Smith (starter_gauntlets KL3, steel_sword KL15), Leathersmith (archer_garb KL3, padded_armor KL5, leather_bow KL8, hunter_coat KL10), Tailor (prayer_robes KL5, mage_robe KL8, storm_robes KL12), Artificer (mana_ring KL5, soul_shard KL8, crystal_focus_band KL12). Gated by `RequiredKnowledgeLevel` to reward job progression |
| CO4 | More gathering spots and resources | ⬜ Not Started | Expand gathering data: herbs, mushrooms, rare plants, ores, gems, wood types. New `GatheringSpot` entries in rooms.json and matching items in items.json. Each job type (Herbalist, Miner, Woodcutter) should have many more gather nodes across the world |
| CO5 | World locations from story lore | ⬜ Not Started | Existing important story locations do not exist in room/city data yet. These need to be added as rooms, cities, or area zones with appropriate NPCs, monsters, and quests. (List specific locations with Rhyen once story doc is available) |
| CO6 | Multiplayer starting area distinct from singleplayer | ⬜ Not Started | Multiplayer is set before the singleplayer story — it should start in a different location with different introductory NPCs reflecting that earlier time period. Needs: a new starting room ID for multiplayer characters (`ServerApiService.Token != null` path in character creation), different NPC set for shared-world areas, and introductory quests that fit the "before the story" era |
| CO7 | NPC and quest content for multiplayer-era locations | ⬜ Not Started | Companion to CO6 — NPCs in multiplayer areas need different names/roles/dialog that reflect the pre-story timeline. Job masters, quest givers, and shop NPCs should differ between SP and MP worlds |

---

## Language & Lore Systems

| # | Item | Status | Notes |
|---|------|--------|-------|
| LO1 | Myralic language font | ⬜ Not Started | The Myralic in-world script needs a custom font file. No font asset exists. Once created, it should be loadable as a WPF FontFamily resource; the Lexica UI (LO2) will use it for display |
| LO2 | Lexica system — in-game language learning | ⬜ Not Started | Characters should be able to discover and learn Myralic words/phrases through exploration and quests. Needs: a `LexicaEntry` model (word, translation, discovered flag), a `LexicaService` (load from JSON, track player discoveries), and a Lexica page in the ingame menu displaying discovered words in the Myralic font alongside translations |
| LO3 | Multiple in-world languages / dialects | ⬜ Not Started | The story includes different languages and dialects per region/race. Design decision needed: how many languages, which NPCs/areas use them, and how the player encounters/learns them. Mechanically: each language is a separate Lexica entry set; the player's knowledge level per language determines how much of NPC dialog is shown as Myralic glyphs vs translated text |

---

## Art & Visual Polish (WPF only)

| # | Item | Status | Notes |
|---|------|--------|-------|
| AR1 | Background images — room and UI screens | ⬜ Not Started | Most UI screens and room views still use placeholder or no background images. Needs thematic fantasy RPG backgrounds for: main menu, character selection/creation, in-game room view, combat screen, ingame menu panels |
| AR2 | Icons — replace default/missing icons | ⬜ Not Started | Items, skills, and UI buttons still using default WPF icons or no icon. Each item category (weapon, armor, consumable, material, currency) and each skill type should have a dedicated icon. Gather/craft/upgrade action buttons also need icons |
| AR3 | Game application icon | ✅ Done | Myriac-M glyph (Y/gamma shape, matching the script sheet) on themed backgrounds: WPF = deep purple + gold, Console = black + silver, Server = navy + cyan. Generated via `Tools/MakeIcons.ps1` (multi-size PNG-in-ICO: 16/32/48/256px). Wired via `<ApplicationIcon>` in all 3 .csproj files; WPF `MainWindow.Icon` set to pack URI; `Assets\App.ico` marked as `<Resource>` |
| AR4 | Theme rework — fantasy RPG aesthetic (WPF) | ⬜ Not Started | Current Light/Dark themes are generic. Rework `Assets/Light.xaml`, `Assets/Layout.xaml`, and `Assets/Icons.xaml` to use fantasy-appropriate typography, colors, and control styling (parchment textures, rune-border frames, medieval-style buttons). The color token system should be designed to port naturally to Unity UI Toolkit later |

---

## Console Version (ConsoleWorldRPG) — Feature Parity & Library Alignment

The console already references MyriaLib and shares all JSON data files.
What it lacks is (a) replacing its own duplicate logic with MyriaLib calls and (b) implementing the features
that exist in WPF but were never ported. Multiplayer parity is the final goal: console and WPF players
connect to the same MyriaServer and can play together.

**Library alignment — replace console-only duplicates with MyriaLib**

| # | Item | Status | Notes |
|---|------|--------|-------|
| CN1 | Replace `EncounterRunner` with `CombatEncounter` | ✅ Done | `CombatSystem.cs` replaced with a thin I/O wrapper; `CharacterUseItem` added to MyriaLib. Quest rewards no longer granted on kill — player must return to NPC (CN3). |
| CN2 | Replace hardcoded `NpcInteractionHandler` with `NpcService` | ✅ Done | `NpcInteractionHandler` now looks up NPCs via `room.NpcRefs`, matches by ID prefix or NpcType, and dispatches based on `npc.Type`. |
| CN3 | Quest return flow via NPC | ✅ Done | Any NPC interaction now shows returnable quests and calls `quest.GrantRewards` + moves to `CompletedQuests`. Filtering by `ReturnNpcId` pending quest data update (GiverNpcId not yet set in quests.json). |

**Feature parity — missing systems**

| # | Item | Status | Notes |
|---|------|--------|-------|
| CN4 | Race selection at character creation | ✅ Done | Numbered race picker in `LoginManager.CreateNewCharacter`; shows stats + growth per level; applies `10 + BaseStatBonus` to initial stats; sets `RaceSelected = true`; filters class list by `ForbiddenClasses`. |
| CN5 | Class system in console | ✅ Done | Class XP + level added to `status` output; `ClassMasterMenu` in `NpcInteractionHandler` mirrors `ClassPanelViewModel` (list all classes with level/XP/forbidden state, switch via number); dispatched from SkillMaster NPCs that carry `"change_class"` service. |
| CN6 | Job system UI in console | ✅ Done | `jobs` command shows all CharacterJob entries with Skill/Knowledge/Fame levels + active/cooldown state; `JobMasterMenu` in `NpcInteractionHandler` (triggered from SmithMenu option 5 when NPC has `learn_job` service) shows 3-aspect progress and toggles active job with 7-day cooldown enforcement; `jobs.json` added to console data dir; `JobManager.LoadJobs()` wired into `GameService.InitializeGame()`. |
| CN7 | Skill combinations and slots | ✅ Done | New commands: `skills` (list learned/combined/slotted), `combine` (fuse 2–5 learned skills via `SkillCombinationService`), `slots` (manage combat bar via `SkillSlotService` with slot/unslot/move). `SkillMasterMenu` now routes to these. Skill fusion deferred (no `base_skills.json` yet). |
| CN8 | Quest dialog (multi-page, speaker) | ✅ Done | `PlayDialog` + `ResolveSpeakerConsole` in `NpcInteractionHandler`; pages through `DialogLine` list with speaker prefix and "Press Enter to continue"; y/n accept/return prompt at end |
| CN9 | Character stats display | ✅ Done | `character` / `stats` commands → `Printer.ShowCharacter`: race, full stat breakdown (base + allocated + class + gear = total), unspent points warning; `alloc <stat> [n]` command to spend stat points; `ShowHelp` updated |
| CN10 | Day cycle and passive ticks | ✅ Done | `SessionStarted` wired in `Game.Start()` → `StartInactivityTimer` + `ApplyDailyPenalty`; `StartSession(realCharacter)` called after login; timer stopped on logout/exit; day+segment shown on login. Combat/gather ticks already in MyriaLib. Job decay (J12–J14) deferred |

**Multiplayer — console SignalR client**

| # | Item | Status | Notes |
|---|------|--------|-------|
| CN11 | SignalR client integration | ✅ Done | `ConsoleHubClient.cs` — REST JWT login, hub connect/disconnect, all events (ChatMessage/CharacterEntered/CharacterLeft/RoomCharacters/PartyInvite/PartyUpdated/PartyDisbanded); `say`/`whisper` in CharacterCommands; `party`/`party invite/accept/decline/leave` in PartyCommands; JoinRoom on movement; events wired in Game.cs with color output |
| CN12 | Online character save/load | ✅ Done | REST login during local auth (before char selection); server char list; `LoadCharacterAsync` (REST→Character) in `ConsoleHubClient`; server-first load with warning+local fallback; new char saved to server only; `SaveCharacter` helper branches on `HasToken` at exit/logout |
| CN13 | Multiplayer game actions via hub | ✅ Done | Route `attack`, `gather`, `craft`, `upgrade` through the hub methods (`CharacterAttack`, `Gather`, `Craft`, `Upgrade`) when online, exactly as the server validates them. Offline path keeps using `CombatEncounter` / `GatherService` directly |
| CN14 | Chat and social commands | ✅ Done | `say` (room), `g` (global), `w`/`whisper` (private), `party <msg>` (party chat); `friend list/requests/add/accept/remove` via REST. Block deferred (MP3 — no server endpoint yet). |
| CN15 | Party commands | ✅ Done | `party kick <player>` + `party promote <player>` added; `Kick` + `TransferLeader` added to `PartyService`; `KickFromParty` + `TransferPartyLeader` hub methods added to `GameHub`; `KickedFromParty` event wired in console. Invite/accept/decline/leave were already implemented. |

---

## Future / Backlog

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| 27 | More race / class profiles | ⬜ Not Started | Stat profiles exist but only a few fully tuned |
| 28 | Additional quests & world content | ⬜ Not Started | Expand caves, dungeons, cities with real area data |
| 29 | PvP / player-vs-player mechanics | ⬜ Not Started | No design yet |
| 30 | Guild / social features | ✅ Done | Stale entry — guild is fully implemented: server-side `GuildService` (DB-backed, `MyriaServer/Hubs/GameHub.cs` Guild* methods), WPF `Page_Guild.xaml`/`GuildPageViewModel`/`GameHubService` Guild* wrappers, and console `GuildCommands.cs` (added this session, MP15) all exist alongside the friend list. |
| 31 | Achievement system | ⬜ Not Started | No design yet |

---

_Last updated: 2026-08-08_

