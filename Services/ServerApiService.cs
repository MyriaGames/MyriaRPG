using Myria.Lib.Core.Entities.Jobs;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models.BaseModel;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Myria.Wpf.Services
{
    public enum AuthResult { Success, InvalidCredentials, Conflict, ValidationError, ServerError }
    public enum AccountUpdateResult { Success, InvalidCredentials, Conflict, ValidationError, RealmUnreachable, ServerError }

    public static partial class ServerApiService
    {
        // AuthBaseUrl points at the shared MyriaAuthServer — stays fixed regardless of realm
        // selection; every realm trusts JWTs it issues via a shared signing key, no live
        // validation call back to it needed. BaseUrl points at the selected realm's own
        // MyriaServer instance — set when the player picks a realm on the lobby screen.
        public static string AuthBaseUrl { get; set; } = "http://localhost:5050";
        public static string BaseUrl     { get; set; } = "http://localhost:5001";

        /// <summary>Prepends a scheme if the user typed a server address with none (e.g.
        /// "myria.duckdns.org:5000") - without it, Uri parsing reads everything before the
        /// first colon as the scheme itself and throws "'host' scheme is not supported".
        /// Defaults to https:// for anything but loopback, so a remote server address doesn't
        /// silently send login credentials over plain HTTP unless it's the player's own machine.</summary>
        public static string NormalizeAddress(string address)
        {
            address = address.Trim();
            if (address.Length == 0) return address;

            if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var host = address.Split('/')[0].Split(':')[0];
                var isLoopback = host is "localhost" or "127.0.0.1" or "::1";
                address = (isLoopback ? "http://" : "https://") + address;
            }

            return address;
        }

        // Any operator can run their own Auth/Realm server - not just the official one - and
        // most won't have a domain to get a publicly-trusted certificate for, so a self-signed
        // cert has to be accepted somehow. Rather than pinning one hardcoded thumbprint (which
        // would only ever work for a single specific server), trust-on-first-use each server
        // address independently via TrustedServerCertStore: the first successful connection to
        // a given host:port remembers its certificate's thumbprint, and every later connection
        // to that same host:port must present that exact certificate again. A real CA-issued
        // cert still validates the usual way and never reaches this path at all.
        private static HttpMessageHandler CreatePinnedHandler() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, _, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None) return true;
                if (cert is null || request.RequestUri is null) return false;
                var thumbprint = cert.GetCertHashString(HashAlgorithmName.SHA256);
                return Myria.Lib.Core.Utils.TrustedServerCertStore.TrustOnFirstUse(
                    request.RequestUri.Authority, thumbprint);
            }
        };

        /// <summary>Used by GameHubService so the SignalR connection trusts the same pinned
        /// certificate as every other request instead of falling back to default validation.</summary>
        public static HttpMessageHandler CreateHttpMessageHandler() => CreatePinnedHandler();

        private static readonly HttpClient _http = new(CreatePinnedHandler());
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        public static string? Token        { get; private set; }
        public static string? LastUsername { get; private set; }
        public static string? LastError    { get; private set; }

        /// <summary>Raised whenever <see cref="Token"/> is set or cleared, so long-lived
        /// ViewModels (created before login) can refresh bindings like IsChatAvailable.</summary>
        public static event Action? AuthStateChanged;

        // ── Auth ─────────────────────────────────────────────────────────────────

        public static async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{AuthBaseUrl}/api/auth/login",
                    new { username, password });

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    return AuthResult.InvalidCredentials;

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                    return AuthResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AuthResult.Success : AuthResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AuthResult.ServerError;
            }
        }

        public static async Task<AuthResult> RegisterAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{AuthBaseUrl}/api/auth/register",
                    new { username, password });

                if (resp.StatusCode == HttpStatusCode.Conflict)
                    return AuthResult.Conflict;

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    LastError = await ParseValidationErrorAsync(resp);
                    return AuthResult.ValidationError;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                    return AuthResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AuthResult.Success : AuthResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AuthResult.ServerError;
            }
        }

        public static async Task<AccountUpdateResult> DeleteAccountAsync(string password)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, $"{AuthBaseUrl}/api/auth/account")
                {
                    Content = JsonContent.Create(new { username = LastUsername, password })
                };
                var resp = await _http.SendAsync(req);

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    return AccountUpdateResult.InvalidCredentials;
                if (resp.StatusCode == HttpStatusCode.BadGateway)
                {
                    LastError = await ParseApiErrorAsync(resp);
                    return AccountUpdateResult.RealmUnreachable;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    LastError = await ParseApiErrorAsync(resp);
                    return AccountUpdateResult.ServerError;
                }

                ClearToken();
                return AccountUpdateResult.Success;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AccountUpdateResult.ServerError;
            }
        }

        public static async Task<AccountUpdateResult> ChangeUsernameAsync(string password, string newUsername)
        {
            try
            {
                var resp = await _http.PutAsJsonAsync($"{AuthBaseUrl}/api/auth/username",
                    new { username = LastUsername, password, newUsername });

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    return AccountUpdateResult.InvalidCredentials;
                if (resp.StatusCode == HttpStatusCode.Conflict)
                    return AccountUpdateResult.Conflict;
                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    LastError = await ParseValidationErrorAsync(resp);
                    return AccountUpdateResult.ValidationError;
                }
                if (resp.StatusCode == HttpStatusCode.BadGateway)
                {
                    LastError = await ParseApiErrorAsync(resp);
                    return AccountUpdateResult.RealmUnreachable;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    LastError = await ParseApiErrorAsync(resp);
                    return AccountUpdateResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AccountUpdateResult.Success : AccountUpdateResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AccountUpdateResult.ServerError;
            }
        }

        public static async Task<AccountUpdateResult> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var resp = await _http.PutAsJsonAsync($"{AuthBaseUrl}/api/auth/password",
                    new { username = LastUsername, oldPassword, newPassword });

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    return AccountUpdateResult.InvalidCredentials;
                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    LastError = await ParseValidationErrorAsync(resp);
                    return AccountUpdateResult.ValidationError;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    LastError = await ParseApiErrorAsync(resp);
                    return AccountUpdateResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AccountUpdateResult.Success : AccountUpdateResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AccountUpdateResult.ServerError;
            }
        }

        // ── Character Save ────────────────────────────────────────────────────────

        public static async Task<bool> SaveCharacterAsync(Character character)
        {
            try
            {
                character.CurrentRoomId = character.CurrentRoom?.Id ?? character.CurrentRoomId;

                var req = new CharacterSaveDto
                {
                    Name                    = character.Name,
                    Level                   = character.Level,
                    Experience              = character.Experience,
                    ExpForNextLvl           = character.ExpForNextLvl,
                    PotionTierAvailable     = character.PotionTierAvailable,
                    Class                   = character.Class,
                    Race                    = character.Race,
                    RaceSelected            = character.RaceSelected,
                    CurrentRoomId           = character.CurrentRoomId,
                    LastHealerRoomId        = character.LastHealerRoomId,
                    CurrentHealth           = character.CurrentHealth,
                    CurrentMana             = character.CurrentMana,
                    StatStrength            = character.Stats.Strength,
                    StatDexterity           = character.Stats.Dexterity,
                    StatEndurance           = character.Stats.Endurance,
                    StatIntelligence        = character.Stats.Intelligence,
                    StatSpirit              = character.Stats.Spirit,
                    StatStrengthBonus       = character.Stats.StrengthBonus,
                    StatDexterityBonus      = character.Stats.DexterityBonus,
                    StatEnduranceBonus      = character.Stats.EnduranceBonus,
                    StatIntelligenceBonus   = character.Stats.IntelligenceBonus,
                    StatSpiritBonus         = character.Stats.SpiritBonus,
                    StatUnusedPoints        = character.Stats.UnusedPoints,
                    StatBaseHealth          = character.Stats.BaseHealth,
                    StatBaseMana            = character.Stats.BaseMana,
                    WeaponItemId            = character.WeaponSlot?.Id,
                    ArmorItemId             = character.ArmorSlot?.Id,
                    AccessoryItemId         = character.AccessorySlot?.Id,
                    MoneyBronze             = character.Money.Balance.BronzeTotal,
                    MoneyCapacity           = character.Money.Capacity,
                    InventoryPages          = character.Inventory.Pages,
                    LastClassPenaltyApplied = character.LastClassPenaltyApplied,
                    LastClassChanged        = character.LastClassChanged,
                    ActiveJobId             = character.ActiveJobId,
                    LastJobChanged          = character.LastJobChanged,

                    InventoryItems = character.Inventory.Items
                        .Select((item, idx) => new InventoryItemDto { ItemId = item.Id, StackSize = item.StackSize, SlotIndex = idx })
                        .ToList(),

                    SkillIds = character.Skills.Select(s => s.Id).ToList(),

                    ActiveQuests = character.ActiveQuests.Select(q => new ActiveQuestDto
                    {
                        QuestId          = q.Id,
                        Status           = (int)q.Status,
                        KillProgressJson = JsonSerializer.Serialize(
                            q.KillProgress.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)),
                        ItemProgressJson = JsonSerializer.Serialize(q.ItemProgress)
                    }).ToList(),

                    CompletedQuestIds = character.CompletedQuests.Select(q => q.Id).ToList(),

                    RepeatableQuests = character.RepeatableQuestRecords.Select(kv => new RepeatableQuestDto
                    {
                        QuestId            = kv.Key,
                        TimesCompleted     = kv.Value.TimesCompleted,
                        CompletionsToday   = kv.Value.CompletionsToday,
                        LastCompletionDate = kv.Value.LastCompletionDate
                    }).ToList(),

                    Jobs = character.Jobs.Select(j => new JobDto
                    {
                        JobId           = j.JobId,
                        SkillXp         = j.SkillXp,
                        KnowledgeXp     = j.KnowledgeXp,
                        FameXp          = j.FameXp,
                        LastFameTickDay  = j.LastFameTickDay,
                        LastSkillUsedDay = j.LastSkillUsedDay
                    }).ToList(),

                    SkillSlots = character.SkillSlots.Select((slot, idx) => new SkillSlotDto
                    {
                        SlotIndex = idx,
                        Source    = (int)slot.Source,
                        SkillId   = slot.SkillId
                    }).ToList(),

                    CompositeSkills = BuildCompositeSkillDtos(character),

                    CombinedSkills = BuildCombinedSkillDtos(character),

                    KnownRunes = character.KnownRunes.Select(r => new KnownRuneDto
                    {
                        InstanceId   = r.Id,
                        BaseRuneId   = r.BaseRuneId,
                        AddedWordIds = r.AddedWordIds.ToList()
                    }).ToList(),

                    RuneDictionary = character.RuneDictionary.Select(e => new RuneDictEntryDto
                    {
                        WordId              = e.WordId,
                        CharacterLabel         = e.CharacterLabel,
                        IsOfficiallyLearned = e.IsOfficiallyLearned
                    }).ToList(),

                    RoomGatheringStatus = character.RoomGatheringStatus.Select(kv => new RoomGatheringDto
                    {
                        RoomId        = kv.Key,
                        LastGatheredAt = kv.Value
                    }).ToList(),

                    ClassXp = character.ClassXp.Select(kv => new ClassXpDto
                    {
                        Class = kv.Key,
                        Xp    = kv.Value
                    }).ToList()
                };

                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/characters", req, _jsonOpts);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                }
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex) { LastError = ex.Message; return false; }
        }

        // ── Character Load ────────────────────────────────────────────────────────

        public static async Task<Character?> LoadCharacterAsync(string name)
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/characters/{Uri.EscapeDataString(name)}");
                if (!resp.IsSuccessStatusCode) return null;

                var dto = await resp.Content.ReadFromJsonAsync<CharacterLoadDto>(_jsonOpts);
                if (dto is null) return null;

                // ── Build Stats ───────────────────────────────────────────────────
                var stats = new Myria.Lib.Core.Entities.Stats
                {
                    Strength         = dto.StatStrength,
                    Dexterity        = dto.StatDexterity,
                    Endurance        = dto.StatEndurance,
                    Intelligence     = dto.StatIntelligence,
                    Spirit           = dto.StatSpirit,
                    StrengthBonus    = dto.StatStrengthBonus,
                    DexterityBonus   = dto.StatDexterityBonus,
                    EnduranceBonus   = dto.StatEnduranceBonus,
                    IntelligenceBonus= dto.StatIntelligenceBonus,
                    SpiritBonus      = dto.StatSpiritBonus,
                    UnusedPoints     = dto.StatUnusedPoints,
                    BaseHealth       = dto.StatBaseHealth,
                    BaseMana         = dto.StatBaseMana
                };

                // ── Construct Character ──────────────────────────────────────────────
                var character = new Character(dto.Name, stats)
                {
                    Level                   = dto.Level,
                    Experience              = dto.Experience,
                    ExpForNextLvl           = dto.ExpForNextLvl,
                    PotionTierAvailable     = dto.PotionTierAvailable,
                    Class                   = dto.Class,
                    Race                    = dto.Race,
                    RaceSelected            = dto.RaceSelected,
                    CurrentRoomId           = dto.CurrentRoomId,
                    LastHealerRoomId        = dto.LastHealerRoomId,
                    CurrentHealth           = dto.CurrentHealth,
                    CurrentMana             = dto.CurrentMana,
                    LastClassPenaltyApplied = dto.LastClassPenaltyApplied,
                    LastClassChanged        = dto.LastClassChanged,
                    ActiveJobId             = dto.ActiveJobId,
                    LastJobChanged          = dto.LastJobChanged
                };

                character.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == dto.CurrentRoomId);

                // ── Money ─────────────────────────────────────────────────────────
                character.Money = new MoneyBag
                {
                    Balance  = new Myria.Lib.Core.Entities.Characters.Money(dto.MoneyBronze),
                    Capacity = dto.MoneyCapacity
                };

                // ── Inventory ─────────────────────────────────────────────────────
                character.Inventory.Pages = dto.InventoryPages;
                character.Inventory.Items.Clear();
                foreach (var ii in dto.InventoryItems.OrderBy(x => x.SlotIndex))
                {
                    var item = ItemFactory.CreateItem(ii.ItemId, ii.StackSize);
                    if (item is not null)
                        character.Inventory.Items.Add(item);
                }

                // ── Equipment ─────────────────────────────────────────────────────
                if (!string.IsNullOrEmpty(dto.WeaponItemId))
                    character.WeaponSlot = ItemFactory.CreateItem(dto.WeaponItemId) as Myria.Lib.Core.Entities.Items.EquipmentItem;
                if (!string.IsNullOrEmpty(dto.ArmorItemId))
                    character.ArmorSlot = ItemFactory.CreateItem(dto.ArmorItemId) as Myria.Lib.Core.Entities.Items.EquipmentItem;
                if (!string.IsNullOrEmpty(dto.AccessoryItemId))
                    character.AccessorySlot = ItemFactory.CreateItem(dto.AccessoryItemId) as Myria.Lib.Core.Entities.Items.EquipmentItem;

                // ── Active Quests ─────────────────────────────────────────────────
                character.ActiveQuests.Clear();
                foreach (var aq in dto.ActiveQuests)
                {
                    var template = QuestManager.GetQuestById(aq.QuestId);
                    if (template is null) continue;
                    var quest = template.Clone();
                    quest.Status = (QuestStatus)aq.Status;
                    try
                    {
                        quest.KillProgress = (JsonSerializer.Deserialize<Dictionary<string, int>>(aq.KillProgressJson) ?? new())
                            .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
                    }
                    catch { quest.KillProgress = new(); }
                    try
                    {
                        quest.ItemProgress = JsonSerializer.Deserialize<Dictionary<string, int>>(aq.ItemProgressJson) ?? new();
                    }
                    catch { quest.ItemProgress = new(); }
                    character.ActiveQuests.Add(quest);
                }

                // ── Completed Quests ──────────────────────────────────────────────
                character.CompletedQuests.Clear();
                foreach (var qId in dto.CompletedQuestIds)
                {
                    var template = QuestManager.GetQuestById(qId);
                    if (template is not null)
                        character.CompletedQuests.Add(template.Clone());
                }

                // ── Repeatable Quest Records ──────────────────────────────────────
                character.RepeatableQuestRecords.Clear();
                foreach (var rq in dto.RepeatableQuests)
                    character.RepeatableQuestRecords[rq.QuestId] = new RepeatRecord
                    {
                        TimesCompleted     = rq.TimesCompleted,
                        CompletionsToday   = rq.CompletionsToday,
                        LastCompletionDate = rq.LastCompletionDate
                    };

                // ── Jobs ──────────────────────────────────────────────────────────
                character.Jobs.Clear();
                foreach (var j in dto.Jobs)
                    character.Jobs.Add(new CharacterJob
                    {
                        JobId           = j.JobId,
                        SkillXp         = j.SkillXp,
                        KnowledgeXp     = j.KnowledgeXp,
                        FameXp          = j.FameXp,
                        LastFameTickDay  = j.LastFameTickDay,
                        LastSkillUsedDay = j.LastSkillUsedDay
                    });

                // ── Skill Slots ───────────────────────────────────────────────────
                character.SkillSlots.Clear();
                foreach (var slot in dto.SkillSlots.OrderBy(s => s.SlotIndex))
                    character.SkillSlots.Add(new SkillSlot
                    {
                        Source  = (SlottedSkillSource)slot.Source,
                        SkillId = slot.SkillId
                    });

                // ── Composite Skills (active) ─────────────────────────────────────
                character.CompositeSkills.Clear();
                character.ActiveCompositeSkillIds.Clear();
                character.StashedCompositeSkills.Clear();

                foreach (var cs in dto.CompositeSkills)
                {
                    var composite = new CompositeSkill
                    {
                        Id           = cs.InstanceId,
                        ComponentIds = cs.ComponentIds.ToList()
                    };

                    if (cs.IsStashed && !string.IsNullOrEmpty(cs.StashedForClass))
                    {
                        var cls = cs.StashedForClass!;
                        if (!character.StashedCompositeSkills.ContainsKey(cls))
                            character.StashedCompositeSkills[cls] = new();
                        character.StashedCompositeSkills[cls].Add(composite);
                    }
                    else
                    {
                        character.CompositeSkills.Add(composite);
                        if (cs.IsActive)
                            character.ActiveCompositeSkillIds.Add(cs.InstanceId);
                    }
                }

                // ── Combined Skills ───────────────────────────────────────────────
                character.CombinedSkills.Clear();
                character.StashedCombinedSkills.Clear();

                foreach (var cs in dto.CombinedSkills)
                {
                    var combined = new CombinedSkill
                    {
                        Id       = cs.InstanceId,
                        SkillIds = cs.SkillIds.ToList()
                    };

                    if (cs.IsStashed && !string.IsNullOrEmpty(cs.StashedForClass))
                    {
                        var cls = cs.StashedForClass!;
                        if (!character.StashedCombinedSkills.ContainsKey(cls))
                            character.StashedCombinedSkills[cls] = new();
                        character.StashedCombinedSkills[cls].Add(combined);
                    }
                    else
                    {
                        character.CombinedSkills.Add(combined);
                    }
                }

                // ── Known Runes ───────────────────────────────────────────────────
                character.KnownRunes.Clear();
                foreach (var r in dto.KnownRunes)
                    character.KnownRunes.Add(new CompositeRune
                    {
                        Id           = r.InstanceId,
                        BaseRuneId   = r.BaseRuneId,
                        AddedWordIds = r.AddedWordIds.ToList()
                    });

                // ── Rune Dictionary ───────────────────────────────────────────────
                character.RuneDictionary.Clear();
                foreach (var e in dto.RuneDictionary)
                    character.RuneDictionary.Add(new CharacterRuneWordEntry
                    {
                        WordId              = e.WordId,
                        CharacterLabel         = e.CharacterLabel,
                        IsOfficiallyLearned = e.IsOfficiallyLearned
                    });

                // ── Room Gathering Status ─────────────────────────────────────────
                character.RoomGatheringStatus.Clear();
                foreach (var rg in dto.RoomGatheringStatus)
                    character.RoomGatheringStatus[rg.RoomId] = rg.LastGatheredAt;

                // ── Class XP ──────────────────────────────────────────────────────
                character.ClassXp.Clear();
                foreach (var cx in dto.ClassXp)
                    character.ClassXp[cx.Class] = cx.Xp;

                // ── Post-load resolution ──────────────────────────────────────────
                character.Inventory.Items.RemoveAll(i => i is null);
                character.RecalculateUnusedPoints();
                character.ValidateQuestStatuses();
                SkillFactory.UpdateSkills(character);
                BaseRuneService.ResolveRunes(character);
                SkillFusionSystem.ResolveCompositeSkills(character);
                SkillCombinationService.ResolveCombinedSkills(character);
                SkillSlotService.ResolveSlots(character);
                SkillSlotService.MigrateIfEmpty(character);

                return character;
            }
            catch { return null; }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static List<CompositeSkillDto> BuildCompositeSkillDtos(Character character)
        {
            var list = new List<CompositeSkillDto>();

            foreach (var cs in character.CompositeSkills)
                list.Add(new CompositeSkillDto
                {
                    InstanceId      = cs.Id,
                    ComponentIds    = cs.ComponentIds.ToList(),
                    IsStashed       = false,
                    StashedForClass = null,
                    IsActive        = character.ActiveCompositeSkillIds.Contains(cs.Id)
                });

            foreach (var (cls, stashList) in character.StashedCompositeSkills)
                foreach (var cs in stashList)
                    list.Add(new CompositeSkillDto
                    {
                        InstanceId      = cs.Id,
                        ComponentIds    = cs.ComponentIds.ToList(),
                        IsStashed       = true,
                        StashedForClass = cls,
                        IsActive        = false
                    });

            return list;
        }

        private static List<CombinedSkillDto> BuildCombinedSkillDtos(Character character)
        {
            var list = new List<CombinedSkillDto>();

            foreach (var cs in character.CombinedSkills)
                list.Add(new CombinedSkillDto
                {
                    InstanceId      = cs.Id,
                    SkillIds        = cs.SkillIds.ToList(),
                    IsStashed       = false,
                    StashedForClass = null
                });

            foreach (var (cls, stashList) in character.StashedCombinedSkills)
                foreach (var cs in stashList)
                    list.Add(new CombinedSkillDto
                    {
                        InstanceId      = cs.Id,
                        SkillIds        = cs.SkillIds.ToList(),
                        IsStashed       = true,
                        StashedForClass = cls
                    });

            return list;
        }

        // ── Other character endpoints ─────────────────────────────────────────────

        public static async Task<bool> DeleteCharacterAsync(string name)
        {
            try
            {
                var resp = await _http.DeleteAsync($"{BaseUrl}/api/characters/{Uri.EscapeDataString(name)}");
                return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
            }
            catch { return false; }
        }

        // Realms are genuinely separate MyriaServer processes now — the static id/name/url
        // list comes from the shared MyriaAuthServer's realm directory, but online/character
        // status can only be known by asking each realm directly, so every realm's own
        // /api/status is polled concurrently. One unreachable realm just shows as offline;
        // it doesn't stall the rest of the list.
        public static async Task<List<Model.LobbyInfo>> GetLobbiesAsync()
        {
            List<RealmDto> realms;
            try
            {
                var resp = await _http.GetAsync($"{AuthBaseUrl}/api/realms");
                if (!resp.IsSuccessStatusCode) return [];
                realms = await resp.Content.ReadFromJsonAsync<List<RealmDto>>(_jsonOpts) ?? [];
            }
            catch { return []; }

            var statusTasks = realms.Select(async realm =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var resp = await _http.GetAsync($"{realm.Url}/api/status", cts.Token);
                    if (!resp.IsSuccessStatusCode)
                        return new Model.LobbyInfo { Id = realm.Id, Name = realm.Name, Url = realm.Url, IsOnline = false };

                    var status = await resp.Content.ReadFromJsonAsync<StatusDto>(_jsonOpts, cts.Token);
                    return new Model.LobbyInfo
                    {
                        Id             = realm.Id,
                        Name           = realm.Name,
                        Url            = realm.Url,
                        CharacterCount = status?.PlayerCount ?? 0,
                        IsOnline       = true
                    };
                }
                catch
                {
                    return new Model.LobbyInfo { Id = realm.Id, Name = realm.Name, Url = realm.Url, IsOnline = false };
                }
            });

            return (await Task.WhenAll(statusTasks)).ToList();
        }

        public static async Task<List<string>> GetCharacterNamesAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/characters");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<string>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static void ClearToken()
        {
            Token = null;
            LastUsername = null;
            _http.DefaultRequestHeaders.Authorization = null;
            AuthStateChanged?.Invoke();
        }

        // ── Friend API ───────────────────────────────────────────────────────
        public record FriendInfo(int FriendshipId, string CharacterName, bool IsOnline)
        {
            public int    Level         { get; init; }
            public string ClassName     { get; init; } = string.Empty;
            public int?   CurrentRoomId { get; init; }
            public bool   InParty       { get; init; }
        }
        public record FriendRequestInfo(int FriendshipId, string FromCharacterName);

        public static async Task<List<FriendInfo>> GetFriendsAsync()
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.GetAsync($"{BaseUrl}/api/friends?characterName={Uri.EscapeDataString(me)}");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<FriendInfo>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static async Task<List<FriendRequestInfo>> GetFriendRequestsAsync()
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.GetAsync($"{BaseUrl}/api/friends/requests?characterName={Uri.EscapeDataString(me)}");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<FriendRequestInfo>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static async Task<string?> SendFriendRequestAsync(string characterName)
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/friends/request",
                    new { characterName, fromCharacterName = me }, _jsonOpts);
                if (resp.IsSuccessStatusCode) return null;
                return await ParseApiErrorAsync(resp);
            }
            catch (Exception ex) { return ex.Message; }
        }

        public static async Task<bool> AcceptFriendRequestAsync(int friendshipId)
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.PostAsync(
                    $"{BaseUrl}/api/friends/{friendshipId}/accept?characterName={Uri.EscapeDataString(me)}", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> RemoveFriendAsync(int friendshipId)
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.DeleteAsync(
                    $"{BaseUrl}/api/friends/{friendshipId}?characterName={Uri.EscapeDataString(me)}");
                return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
            }
            catch { return false; }
        }

        // ── Block API ─────────────────────────────────────────────────────────
        public record BlockInfo(int BlockId, string Username);

        public static async Task<List<BlockInfo>> GetBlocksAsync()
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.GetAsync($"{BaseUrl}/api/blocks?characterName={Uri.EscapeDataString(me)}");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<BlockInfo>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static async Task<bool> BlockCharacterAsync(string characterName)
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/blocks",
                    new { characterName, fromCharacterName = me }, _jsonOpts);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> UnblockAsync(int blockId)
        {
            try
            {
                var me = UserAccoundService.CurrentCharacter?.Name ?? string.Empty;
                var resp = await _http.DeleteAsync(
                    $"{BaseUrl}/api/blocks/{blockId}?characterName={Uri.EscapeDataString(me)}");
                return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
            }
            catch { return false; }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static async Task<string> ParseApiErrorAsync(HttpResponseMessage resp)
        {
            try
            {
                var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? string.Empty;
                if (doc.RootElement.TryGetProperty("errors", out var errors))
                    foreach (var field in errors.EnumerateObject())
                        if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                            return field.Value[0].GetString() ?? string.Empty;
            }
            catch { }
            return $"Error {(int)resp.StatusCode}";
        }

        private static async Task<string> ParseValidationErrorAsync(HttpResponseMessage resp)
        {
            try
            {
                var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("errors", out var errors))
                    foreach (var field in errors.EnumerateObject())
                        if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                            return field.Value[0].GetString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
        }

        private static void SetToken(string? token, string? username = null)
        {
            Token = token;
            LastUsername = username;
            _http.DefaultRequestHeaders.Authorization = token is not null
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
            AuthStateChanged?.Invoke();
        }

        // ── Private DTO types (mirror server-side shapes for JSON deserialization) ─

        private record AuthResponse(string Token, string Username, DateTime ExpiresAt);

        private class RealmDto
        {
            public string Id   { get; set; } = "";
            public string Name { get; set; } = "";
            public string Url  { get; set; } = "";
        }

        private class StatusDto
        {
            public int PlayerCount { get; set; }
        }

        // Save request (sent to server)
        private class CharacterSaveDto
        {
            public string   Name                    { get; set; } = "";
            public int      Level                   { get; set; }
            public long     Experience              { get; set; }
            public long     ExpForNextLvl           { get; set; }
            public int      PotionTierAvailable     { get; set; }
            public string Class { get; set; } = "Fighter";
            public string Race { get; set; } = "Myralu";
            public bool     RaceSelected            { get; set; }
            public int      CurrentRoomId           { get; set; }
            public int?     LastHealerRoomId        { get; set; }
            public int      CurrentHealth           { get; set; }
            public int      CurrentMana             { get; set; }
            public int      StatStrength            { get; set; }
            public int      StatDexterity           { get; set; }
            public int      StatEndurance           { get; set; }
            public int      StatIntelligence        { get; set; }
            public int      StatSpirit              { get; set; }
            public int      StatStrengthBonus       { get; set; }
            public int      StatDexterityBonus      { get; set; }
            public int      StatEnduranceBonus      { get; set; }
            public int      StatIntelligenceBonus   { get; set; }
            public int      StatSpiritBonus         { get; set; }
            public int      StatUnusedPoints        { get; set; }
            public int      StatBaseHealth          { get; set; }
            public int      StatBaseMana            { get; set; }
            public string?  WeaponItemId            { get; set; }
            public string?  ArmorItemId             { get; set; }
            public string?  AccessoryItemId         { get; set; }
            public long     MoneyBronze             { get; set; }
            public long     MoneyCapacity           { get; set; }
            public int      InventoryPages          { get; set; }
            public DateTime LastClassPenaltyApplied { get; set; }
            public DateTime LastClassChanged        { get; set; }
            public string?  ActiveJobId             { get; set; }
            public DateTime LastJobChanged          { get; set; }

            public List<InventoryItemDto>   InventoryItems      { get; set; } = new();
            public List<string>             SkillIds            { get; set; } = new();
            public List<ActiveQuestDto>     ActiveQuests        { get; set; } = new();
            public List<string>             CompletedQuestIds   { get; set; } = new();
            public List<RepeatableQuestDto> RepeatableQuests    { get; set; } = new();
            public List<JobDto>             Jobs                { get; set; } = new();
            public List<SkillSlotDto>       SkillSlots          { get; set; } = new();
            public List<CompositeSkillDto>  CompositeSkills     { get; set; } = new();
            public List<CombinedSkillDto>   CombinedSkills      { get; set; } = new();
            public List<KnownRuneDto>       KnownRunes          { get; set; } = new();
            public List<RuneDictEntryDto>   RuneDictionary      { get; set; } = new();
            public List<RoomGatheringDto>   RoomGatheringStatus { get; set; } = new();
            public List<ClassXpDto>         ClassXp             { get; set; } = new();
        }

        // Load response (received from server) — same shape as save request
        private class CharacterLoadDto : CharacterSaveDto { }

        private class InventoryItemDto
        {
            public string ItemId    { get; set; } = "";
            public int    StackSize { get; set; } = 1;
            public int    SlotIndex { get; set; }
        }

        private class ActiveQuestDto
        {
            public string QuestId          { get; set; } = "";
            public int    Status           { get; set; }
            public string KillProgressJson { get; set; } = "{}";
            public string ItemProgressJson { get; set; } = "{}";
        }

        private class RepeatableQuestDto
        {
            public string    QuestId            { get; set; } = "";
            public int       TimesCompleted     { get; set; }
            public int       CompletionsToday   { get; set; }
            public DateTime? LastCompletionDate { get; set; }
        }

        private class JobDto
        {
            public string JobId          { get; set; } = "";
            public long   SkillXp        { get; set; }
            public long   KnowledgeXp    { get; set; }
            public long   FameXp         { get; set; }
            public int    LastFameTickDay  { get; set; }
            public int    LastSkillUsedDay { get; set; }
        }

        private class SkillSlotDto
        {
            public int    SlotIndex { get; set; }
            public int    Source    { get; set; }
            public string SkillId   { get; set; } = "";
        }

        private class CompositeSkillDto
        {
            public string       InstanceId      { get; set; } = "";
            public List<string> ComponentIds    { get; set; } = new();
            public bool         IsStashed       { get; set; }
            public string? StashedForClass { get; set; }
            public bool         IsActive        { get; set; }
        }

        private class CombinedSkillDto
        {
            public string       InstanceId      { get; set; } = "";
            public List<string> SkillIds        { get; set; } = new();
            public bool         IsStashed       { get; set; }
            public string? StashedForClass { get; set; }
        }

        private class KnownRuneDto
        {
            public string       InstanceId   { get; set; } = "";
            public string       BaseRuneId   { get; set; } = "";
            public List<string> AddedWordIds { get; set; } = new();
        }

        private class RuneDictEntryDto
        {
            public string  WordId              { get; set; } = "";
            public string? CharacterLabel         { get; set; }
            public bool    IsOfficiallyLearned { get; set; }
        }

        private class RoomGatheringDto
        {
            public int      RoomId         { get; set; }
            public DateTime LastGatheredAt { get; set; }
        }

        private class ClassXpDto
        {
            public string Class { get; set; } = "Fighter";
            public long Xp    { get; set; }
        }
    }

    // ── Guild API ────────────────────────────────────────────────────────────

    public static partial class ServerApiService
    {
        public record GuildMemberInfo(int CharacterId, string CharacterName, string Rank, DateTime JoinedAt);
        public record GuildRookieInfo(int CharacterId, string CharacterName, string RecruiterName, DateTime HiredAt);
        public record GuildSettingsInfo(string ApplicationMode, string DepositWithdrawMinRank);
        public record GuildInfo(int Id, string Name, string Tag, string? Description,
            GuildMemberInfo[] Members, GuildRookieInfo[] Rookies, GuildSettingsInfo Settings, long TreasuryBalance);
        public record GuildInviteInfo(int InviteId, int GuildId, string GuildName, string GuildTag,
            bool IsRookieInvite, DateTime ExpiresAt);

        private static string Me => UserAccoundService.CurrentCharacter?.Name ?? string.Empty;

        public static async Task<GuildInfo?> GetMyGuildAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/guilds/me?characterName={Uri.EscapeDataString(Me)}");
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadFromJsonAsync<GuildInfo>(_jsonOpts);
            }
            catch { return null; }
        }

        public static async Task<List<GuildInviteInfo>> GetGuildInvitesAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/guilds/invites?characterName={Uri.EscapeDataString(Me)}");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<GuildInviteInfo>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static async Task<(bool success, string error)> CreateGuildAsync(string name, string tag, string? description)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds",
                    new { CharacterName = Me, Name = name, Tag = tag, Description = description });
                if (resp.IsSuccessStatusCode) return (true, "");
                var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(_jsonOpts);
                return (false, body.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "Could not create guild.");
            }
            catch { return (false, "Network error."); }
        }

        public static async Task<bool> DisbandGuildAsync()
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/api/guilds")
                {
                    Content = JsonContent.Create(new { CharacterName = Me })
                };
                var resp = await _http.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> LeaveGuildAsync()
        {
            try
            {
                var resp = await _http.PostAsync(
                    $"{BaseUrl}/api/guilds/leave?characterName={Uri.EscapeDataString(Me)}", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> AcceptGuildInviteAsync(int inviteId)
        {
            try
            {
                var resp = await _http.PostAsync(
                    $"{BaseUrl}/api/guilds/invites/{inviteId}/accept?characterName={Uri.EscapeDataString(Me)}", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> DeclineGuildInviteAsync(int inviteId)
        {
            try
            {
                var resp = await _http.PostAsync(
                    $"{BaseUrl}/api/guilds/invites/{inviteId}/decline?characterName={Uri.EscapeDataString(Me)}", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> KickGuildMemberAsync(string targetName)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds/members/kick",
                    new { CharacterName = Me, TargetCharacterName = targetName });
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> PromoteGuildMemberAsync(string targetName)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds/members/promote",
                    new { CharacterName = Me, TargetCharacterName = targetName });
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<bool> DemoteGuildMemberAsync(string targetName)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds/members/demote",
                    new { CharacterName = Me, TargetCharacterName = targetName });
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public record GuildRoomInfo(int Id, int RoomId, string Name, string Description, string RoomType, string MinRankRequired, bool IsBuilt, DateTime? BuiltAt);
        public record GuildPropertyInfo(int Id, string Type, int? CityRoomId, int? BaseAnchorRoomId, long? PricePaid, DateTime AcquiredAt, GuildRoomInfo[] Rooms);

        public static async Task<GuildPropertyInfo?> GetGuildPropertyAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/guilds/property?characterName={Uri.EscapeDataString(Me)}");
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadFromJsonAsync<GuildPropertyInfo>(_jsonOpts);
            }
            catch { return null; }
        }

        public static async Task<(bool success, string error)> PurchaseGuildHouseAsync(int cityRoomId)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds/property/house",
                    new { CharacterName = Me, CityRoomId = cityRoomId });
                if (resp.IsSuccessStatusCode) return (true, "");
                var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(_jsonOpts);
                return (false, body.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "Purchase failed.");
            }
            catch { return (false, "Network error."); }
        }

        public static async Task<(bool success, string error)> EstablishGuildBaseAsync(int anchorRoomId)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/guilds/property/base",
                    new { CharacterName = Me, AnchorRoomId = anchorRoomId });
                if (resp.IsSuccessStatusCode) return (true, "");
                var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(_jsonOpts);
                return (false, body.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "Failed.");
            }
            catch { return (false, "Network error."); }
        }
    }
}

