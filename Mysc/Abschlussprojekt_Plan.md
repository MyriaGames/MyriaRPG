# MyriaRPG — Abschlussprojekt Plan

**Scope:** MyriaServer · MyriaRPG (WPF) · MyriaLib (shared)  
**Excluded:** ConsoleWorldRPG  
**Timeframe:** 1–2 months  
**Goal:** Demonstrably working multiplayer-capable RPG with WPF client and ASP.NET Core server

---

## What Already Exists

### MyriaLib — Shared Game Engine

All game logic is centralised here and used by both client and server.

**Entities:** Character, Monster, Item, Room, NPC, Quest, Job, Class, Skill, GatheringSpot, City

**Core systems (all complete):**
- Combat — `CombatEncounter` (1v1 SP) and `GroupCombatEncounter` (N players vs M monsters, round-robin turns, loot to first living player, quest progress for all, XP scaling by level gap)
- Gathering — `GatherService`, daily limits, job Skill multiplier applied to stack size
- Crafting — `CraftingService`, recipe filtering by Knowledge level, Skill quality bonus
- Upgrade — `UpgradeService`, Knowledge-gated level cap, Skill quality bonus
- Job system — 3-aspect XP (Skill / Knowledge / Fame), daily decay, 7-day switch cooldown, Fame sell bonus (`J1–J23` all done)
- Class system — 12 classes with `ClassProfile` stat growth, class XP with its own curve (`ClassXpService`), 7-day switch cooldown, 50 % same-group XP transfer, skill cleanup and stashing on switch (`CL1–CL9` all done)
- Quest system — kill/item objectives, repeatable quests, multi-page dialog with speaker lines, prerequisites, job/class/race/party-size requirements, daily repeat caps (`Q1–Q13` all done)
- Skill system — learn, slot, combine, composite; `SkillCombinationService`, `SkillSlotService`, `SkillFactory` with class-filtered skill lists
- Day/week cycle — passive ticks: job Skill/Knowledge/Fame decay, class XP penalty, gather limit reset
- Balance — XP scaling by level gap, skill damage mitigation formula, class stat growth tuned (`BA1–BA4` all done)
- Localization — `[LocalizedKey]` attribute + auto-wire via `LocalizationAutoWire`, EN/DE JSON files

**Content (all loaded from JSON):**
- 200+ items (weapons, armour, consumables, materials, currency)
- 32 monsters (L1–40+, tuned levels in monsters.json)
- 40+ quests: intro chain (CO1), story chain (CO2), forest chains (CO3b), job chains (CO3c)
- 12 crafting recipes gated by Knowledge level (CO3d)
- 12 classes, 8 races, full NPC type set (all job masters, class masters, shop/craft/upgrade types)

---

### MyriaServer — ASP.NET Core + SignalR

**Authentication:** JWT register/login, token validation middleware

**Character persistence:** REST CRUD, SQLite + EF Core with 3 migrations (InitialCreate → Characters → Friendships → Blocks)

**Social via REST:**
- Friends: send request / accept / remove (`FriendsController`)
- Block: block / unblock / list (`BlocksController`, enforced in GameHub)

**Real-time via SignalR `GameHub`:**
- Room presence: `JoinRoom`, `CharacterEntered`, `CharacterLeft`, `RoomCharacters`
- Chat: global / room / party / whisper channels; blocked players cannot whisper
- Party: create, invite, accept, decline, leave, kick, transfer leader (`PartyService`) — server fully implemented
- 1v1 combat: `StartCombat`, `CharacterAttack`, `CharacterCastSkill`
- Group combat: `StartGroupCombat`, `GroupCharacterAttack`, `GroupCharacterCastSkill`; `GroupCombatService` maps partyId → `GroupCombatEncounter`
- Server-side gather / craft / upgrade with job XP applied server-side
- Direct trade: `TradeService` state machine (Proposed → Active → BothReady → Completed/Cancelled); job Fame bonus applied (`MP5`)
- Character shops: `CharacterShopService` (in-memory, live while owner online); `OpenShop` / `BrowseShop` / `BuyFromShop` hub methods (`MP6`)

---

### MyriaRPG — WPF Client

**Authentication + character flow:** Login/register → character selection → character creation (race picker + class picker with ClassGroup info)

**In-game (game room):**
- Room page: description, exits, NPC list, gathering spots, combat trigger
- Characters-in-room list (populated from `RoomCharacters` / `CharacterEntered` / `CharacterLeft` broadcasts)
- Chat panel with global / room / party / whisper tabs

**Combat:**
- SP 1v1 fight page: attack button, run button, skill bar (slotted skills as clickable buttons with tooltip showing name, description, mana cost), scrolling combat log
- MP group fight: party member HP panel, selectable monster target list, turn indicator, group attack/skill via hub

**NPC interaction panels (all as dedicated pages):**
ShopPanel · CraftPanel · UpgradePanel · DialogPanel · JobMasterPanel · ClassPanel · QuestDialogPanel

**In-game menu (ingame window pages):**
- Character: stats, class level + XP bar, stat allocation (unspent points)
- Inventory: multi-tab (equipment / consumables / materials / all), drag-drop equip
- Skills: learn page, combination page, slot management page
- Jobs page: aspect progress bars for all jobs
- Quest list: Active / Available tabs (repeatables only, first-run-at-NPC rule)
- Friends: list, incoming requests, send request
- Local map: BFS layout, pan/zoom, zone-group collapse, current room highlight, theme-aware colours
- Character shop: owner mode (list/add/remove) + buyer mode (browse/buy)
- Trade page: propose → confirm → complete/cancel state UI
- Settings: visual (theme), language (EN/DE), keybindings

**Infrastructure:** Light/Dark theme swap at runtime, full EN/DE localization auto-wire, RelayCommand + MVVM base, static `Navigation` service with named frames

---

## Core Goals — must be done (≈ 3–4 weeks)

### C1 — Fix character stat initialization (TODO #18)
`ViewmModel_CaracterCreationPage.cs` line 185 uses `new Stats()` (all zeroes) instead of class-profile starting stats.  
**Deliverable:** Starting stats derived from `ClassProfile` for the selected class, consistent with how stats grow during levelling.  
**Effort:** 0.5 day

---

### C2 — Fix filename typos (TODO #22, #23)
Two files have typos baked into filenames, class names, and namespaces:
- `ViewmModel_CaracterCreationPage.cs` → `ViewModel_CharacterCreationPage.cs`
- `QuestListPageViewModelcs.cs` → `QuestListPageViewModel.cs`

**Deliverable:** Files renamed, class names and `namespace` declarations updated, all references in other files updated.  
**Effort:** 0.5 day

---

### C3 — Party management WPF page (TODO MP10)
The TODO marks this as done, but no `Page_Party.xaml` or party ViewModel file exists in the project. `PartyMemberVm.cs` and the `GameHubService` party events are present; the UI is missing.  
**Deliverable:** New `Page_Party.xaml` + `PartyPageViewModel`. Accessible from the in-game menu. Shows:
- Member list with name, HP bar, mana bar (data already broadcast via `PartyMemberStats`)
- Leader crown indicator
- Kick button (leader-only, calls `KickFromParty`)
- Transfer Leader button (leader-only, calls `TransferPartyLeader`)
- Leave button

**Effort:** 2–3 days

---

### C4 — Online player browser / lobby (TODO #21)
No way currently exists for players to discover who is online or find partners.  
**Deliverable:**
- New server endpoint: `GET /api/lobby/online` returning list of `{name, level, roomName}` for all connected players via `CharacterPresenceService`
- New WPF `Page_Lobby.xaml` + ViewModel (accessible from the startup menu or a persistent sidebar), showing the online list with a "Send Party Invite" button per row
- Refresh button + auto-refresh every 30 seconds

**Effort:** 2–3 days

---

### C5 — Map: zone group click + NPC tooltip (TODO M6, M7)
**M6 — Zone click fires nothing:** The `GroupNodeClickedCommand` is wired but click events are absorbed before reaching the handler because node `Border` elements live inside a `TransformGroup`-scaled Canvas layer.  
Fix: add a transparent overlay `Canvas` outside the transform for hit-testing, routing clicks to the correct node via `VisualTreeHelper.HitTest` in `Viewport_MouseUp`.

**M7 — NPC tooltip doesn't appear:** `ToolTip` on a `Border` inside a scaled Canvas doesn't trigger.  
Fix: move the `ToolTip` from the `Border` to the `TextBlock` label inside it.

**Deliverable:** Clicking a zone group node opens that zone's interior room map; hovering a room node shows its NPC names.  
**Effort:** 1–2 days

---

### C6 — WPF localization sweep (TODO #26)
Some WPF strings are still hardcoded English (combat log messages, map labels, button texts).  
**Deliverable:** Audit all `.xaml` files and ViewModels for hardcoded strings not covered by `[LocalizedKey]`; add missing keys to `en.json` and `de.json`. At minimum: combat log labels, fight page button text, map zoom/pan button labels, lobby page strings.  
**Effort:** 1–2 days

---

### C7 — CO5 Group 1: Xaryre story-path locations (TODO CO5)
The main singleplayer story path (Royas → Hydea → Xarra) requires locations that do not yet exist as room data. These are the highest-priority entries from `WorldMap_Plan.md`:

| # | Location | Rooms | Level |
|---|----------|-------|-------|
| 1 | Royas → Hydea trail (Nuvmito) | 3–5 | 1–5 |
| 2 | Hydis city | 3–4 | 12–18 |
| 3 | Hydis → Xarra via Vari Pass | 4–6 | 12–20 |
| 4 | Xarra rework (terrace / canyon feel) | existing rooms | 20+ |

**Deliverable:** Room definitions in `rooms.json` / `cities.json`, NPCs in `npcs.json`, locale strings in `en.json` / `de.json`, appropriate monsters assigned (existing IDs 1–32 cover all level ranges; new monsters added only if a gap exists).  
**Effort:** 5–8 days

---

### C8 — Party-based room gating (TODO #17)
`RoomService` has a no-op TODO at line 96: room gating by required party size is never enforced.  
**Deliverable:** If a room has `RequiredPartySize > 1`, entry is blocked unless the player is in a party of at least that size. The WPF room page shows a localized "This area requires a party of N" message on denied movement.  
**Effort:** 1 day

---

### C9 — Server deployment setup (new — not in TODO)
MyriaServer runs only on localhost. For a real multiplayer demo and for the Abschlussprojekt presentation, it must be deployable to an external host.  
**Deliverable:**
- `appsettings.Production.json`: configurable JWT secret, CORS allowed origins, DB connection string (file path or SQLite in-app)
- HTTPS configured (Kestrel + Let's Encrypt certificate or dev self-signed with note)
- WPF `ServerApiService.BaseUrl` reads from a config file or is set at build time, so the client can target the live server
- One-page deployment guide: dotnet publish → copy to VPS → run as service

**Effort:** 1–2 days

---

## Optional / Stretch Goals (implement if time allows)

### O1 — CO5 Group 2: Yavelca + lowland road
Hydea → Yavelca lowland road (4–6 rooms, L25–30) and Yavelca city (8–10 rooms, L25–35). Yavelca is also the planned MP starting city (groundwork for O4).  
**Effort:** 4–6 days

### O2 — CO5 Group 3: Forest Ralune + Trecaxo foothills
Forest Ralune + Lichtung der Ahnen (3–4 rooms, quest-gated, L22–28) and Trecaxo foothills (2–3 exploration rooms, L25–35).  
**Effort:** 3–4 days

### O3 — CO4: More gathering spots
Expand gathering data in `rooms.json` and `items.json`: more herb, mushroom, ore, gem, and wood nodes across existing and new rooms. Each job (Herbalist, Miner, Woodcutter) should have significantly more gather nodes across the world.  
**Effort:** 2–3 days

### O4 — CO6/CO7: Multiplayer-era starting area
MP story is set before SP. New MP characters should start in Yavelca (requires O1) with NPCs, quests, and dialog that reflect the pre-war prosperous era rather than the wartime SP context. Requires separate starting room ID for online characters and a small introductory quest chain.  
**Effort:** 3–5 days (after O1)

### O5 — Lexica system (TODO LO2)
Characters discover and learn Myriac words through exploration and quests.  
**Deliverable:** `LexicaEntry` model (word, pronunciation, translation, discovered flag), `LexicaService` (load from JSON, track per-player discoveries), `Page_Lexica.xaml` in the ingame menu showing discovered words — Myriac on the left, translation on the right, undiscovered shown as `???`.  
**Effort:** 4–6 days

### O6 — Map layout consistency (TODO M5)
Root cause identified in the TODO: zone anchor positions are computed relative to the player's BFS tree, so the same zone lands at different grid coords depending on which room BFS reaches first. Fix requires anchoring all zones relative to a single fixed reference room (e.g. room 1). Technically involved; purely cosmetic.  
**Effort:** 2–4 days

### O7 — Combat log improvements (new — not in TODO)
The combat log shows plain text lines. Additions:
- Damage numbers in colour (red for received damage, green for heals, yellow for player damage)
- Critical hit indicator ("Critical!" tag)
- Skill name shown inline when a skill fires

**Effort:** 1–2 days

### O8 — Character creation stat preview (new — not in TODO)
The class picker in character creation shows a class name and group but not the actual starting stats. Add a stat-preview panel alongside the class picker showing base stats for the selected class (requires C1 first).  
**Effort:** 1 day

### O9 — More race and class profiles (TODO #27)
Stat growth profiles exist for all 12 classes but several are only placeholder-tuned. Balance pass + additional flavour text for race/class combination restrictions in the creation UI.  
**Effort:** 2–3 days

### O10 — Theme rework / fantasy aesthetic (TODO AR4)
Rework `Assets/Light.xaml`, `Assets/Layout.xaml`, and `Assets/Icons.xaml` with fantasy-appropriate typography, parchment-toned surface colours, rune-border control frames, and medieval-style buttons. Token system should allow future Unity UI Toolkit port.  
**Effort:** 5–10 days

### O11 — Item and skill icons (TODO AR2)
Items and skills currently have no icons (empty or default WPF styling). Each item category and skill type should have a dedicated icon. Requires sourcing or commissioning art assets.  
**Effort:** 3–8 days (art sourcing dominates)

---

## Out of Scope

Too large, no design, or not relevant to Server/WPF scope:

| Item | Reason |
|------|--------|
| PvP (#29) | No design exists; requires major combat system extension |
| Guild system (#30) | Beyond friend list; new DB schema + server architecture |
| Achievement system (#31) | Requires cross-system instrumentation; no design |
| Custom Myralic font (LO1) | Requires a font designer and build toolchain |
| Multiple in-world languages (LO3) | Depends on LO1 + LO2; major content and design work |
| Background images (AR1) | Art assets — commission or source separately |
| Full anti-cheat / server authority (#25) | Architectural overhaul; SP combat is intentionally client-side |

---

## Time Estimate

| Goal | Effort |
|------|--------|
| C1 — Stat initialization | 0.5 day |
| C2 — Rename typos | 0.5 day |
| C3 — Party management page | 2–3 days |
| C4 — Online player browser | 2–3 days |
| C5 — Map zone click + tooltip | 1–2 days |
| C6 — Localization sweep | 1–2 days |
| C7 — CO5 Group 1 (4 locations) | 5–8 days |
| C8 — Party room gating | 1 day |
| C9 — Server deployment | 1–2 days |
| **Core total** | **≈ 14–22 days (3–4 weeks)** |

| Optional | Effort |
|----------|--------|
| O1 — Yavelca + lowland road | 4–6 days |
| O2 — Forest Ralune + Trecaxo | 3–4 days |
| O3 — More gathering spots | 2–3 days |
| O4 — MP-era Yavelca | 3–5 days |
| O5 — Lexica system | 4–6 days |
| O6 — Map consistency | 2–4 days |
| O7 — Combat log improvements | 1–2 days |
| O8 — Stat preview in creation | 1 day |
| O9 — Race/class profile polish | 2–3 days |
| O10 — Theme rework | 5–10 days |
| O11 — Icons | 3–8 days |

**Realistic 2-month scenario:** All Core Goals (weeks 1–4) + O1–O3, O7–O8 (weeks 5–7) + buffer/polish (week 8).  
O4 (MP-era content) and O5 (Lexica) fit if Core Goals finish in under 3 weeks.  
O10/O11 (art) are independent of the schedule and can be done in parallel if art assets are sourced separately.

---

_Created: 2026-05-20_
