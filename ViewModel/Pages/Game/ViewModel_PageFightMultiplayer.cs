using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Events;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageFightMultiplayer : ViewModel_PageFight
    {
        // Server-mode backing state
        private int    _serverCharacterHp;
        private int    _serverCharacterHpMax;
        private int    _serverCharacterMp;
        private int    _serverCharacterMpMax;
        private int    _serverEnemyHp;
        private int    _serverEnemyHpMax;
        private int    _serverEnemyLevel;
        private string _serverEnemyName = "";
        private bool   _serverFinished;
        private bool   _canActServer = false;

        public override string ActiveMonsterName =>
            LocalizationText.LocalizeMonsterName(_encounter?.Enemy.Name ?? _serverEnemyName);
        public override int CharacterHp    => _encounter?.Character.CurrentHealth ?? _serverCharacterHp;
        public override int CharacterHpMax => _encounter?.Character.MaxHealth     ?? _serverCharacterHpMax;
        public override int CharacterMp    => _encounter?.Character.CurrentMana   ?? _serverCharacterMp;
        public override int CharacterMpMax => _encounter?.Character.MaxMana       ?? _serverCharacterMpMax;
        public override int EnemyHp    => _encounter?.Enemy.CurrentHealth ?? _serverEnemyHp;
        public override int EnemyHpMax => _encounter?.Enemy.MaxHealth     ?? _serverEnemyHpMax;
        public override int EnemyLevel => _encounter?.Enemy.Level         ?? _serverEnemyLevel;

        public override bool CanAct => _isGroupCombat
            ? (_isMyTurn && _canActServer)
            : (_encounter != null
                ? (_encounter.Phase != CombatPhase.EnemyTurn &&
                   _encounter.Phase != CombatPhase.Recovery  &&
                   _encounter.Phase != CombatPhase.Finished)
                : _canActServer);

        public ViewModel_PageFightMultiplayer() : base(false /* skip base init */)
        {
            var character = UserAccountService.CurrentCharacter;

            if (PendingGroupCombat != null)
            {
                IsGroupCombat = true;
                var pend      = PendingGroupCombat;
                PendingGroupCombat = null;

                if (PendingLocalEncounter != null)
                {
                    _groupEncounter       = PendingLocalEncounter;
                    PendingLocalEncounter = null;
                    _isLocalGroupCombat   = true;
                }

                _serverCharacterHp    = character.CurrentHealth;
                _serverCharacterHpMax = character.MaxHealth;
                _serverCharacterMp    = character.CurrentMana;
                _serverCharacterMpMax = character.MaxMana;

                InitGroupCombat(pend);
                _lastGroupLogIndex = _groupEncounter?.Log.Count ?? 0;

                foreach (var (skill, source) in SkillSlotService.GetCombatSkills(character))
                    Skills.Add(new FightSkillVm(skill, DetermineTag(character, skill, source)));

                if (!_isLocalGroupCombat)
                {
                    GameHubService.GroupCombatUpdated  += OnGroupCombatUpdated;
                    GameHubService.GroupCombatFinished += OnGroupCombatFinished;
                    GameHubService.CharacterRespawned     += OnCharacterRespawned;
                }
            }
            else if (ServerApiService.Token != null && GameHubService.IsConnected)
            {
                _serverCharacterHp    = character.CurrentHealth;
                _serverCharacterHpMax = character.MaxHealth;
                _serverCharacterMp    = character.CurrentMana;
                _serverCharacterMpMax = character.MaxMana;
                GameHubService.CharacterRespawned += OnCharacterRespawned;
                _ = InitServerCombatAsync(character);
            }
            else
            {
                // Offline fallback — behaves exactly like the base single-player VM.
                if (character.CurrentRoom.HasMonsters)
                    _monster = MonsterService.PickMonsterForFight(
                        character.CurrentRoom.Monsters,
                        character.CurrentRoom.EncounterableMonsters);

                // _encounter is readonly in base — we need a local copy that wraps it.
                // Since it's readonly, we can't assign here. Fall back to base default (null).
                // In offline mode with no encounter, CanAct falls back to _canActServer.
                _canActServer = false; // nothing to act on offline-and-not-in-fight
            }
        }

        // ── Server field sync ────────────────────────────────────────────────

        protected override void OnMonsterSelected(GroupCombatantVm m)
        {
            _serverEnemyName  = m.RawName;
            _serverEnemyHp    = m.Hp;
            _serverEnemyHpMax = m.MaxHp;
            _serverEnemyLevel = m.Level;
        }

        protected override void SyncFromGroupEncounter()
        {
            base.SyncFromGroupEncounter();

            if (_groupEncounter == null) return;

            var p = UserAccountService.CurrentCharacter;
            _serverCharacterHp = p.CurrentHealth;
            _serverCharacterMp = p.CurrentMana;

            if (_selectedMonsterIndex >= 0 && _selectedMonsterIndex < _groupEncounter.Monsters.Count)
            {
                var t             = _groupEncounter.Monsters[_selectedMonsterIndex];
                _serverEnemyHp    = t.CurrentHealth;
                _serverEnemyHpMax = t.MaxHealth;
                _serverEnemyName  = t.Name;
                _serverEnemyLevel = t.Level;
            }
        }

        // ── Group combat (server) ────────────────────────────────────────────

        protected override void InitGroupCombat(StartGroupCombatResult result)
        {
            base.InitGroupCombat(result);

            if (result.Monsters.Count > 0)
            {
                _serverEnemyName  = result.Monsters[0].Name;
                _serverEnemyHp    = result.Monsters[0].Hp;
                _serverEnemyHpMax = result.Monsters[0].MaxHp;
                _serverEnemyLevel = result.Monsters[0].Level;
            }
            _canActServer = IsMyTurn;
        }

        protected override void OnGroupCombatUpdated(GroupCombatSnapshot snap)
        {
            base.OnGroupCombatUpdated(snap);

            _canActServer = IsMyTurn;

            if (_selectedMonsterIndex >= 0 && _selectedMonsterIndex < GroupMonsters.Count)
            {
                _serverEnemyHp   = GroupMonsters[_selectedMonsterIndex].Hp;
                _serverEnemyName = GroupMonsters[_selectedMonsterIndex].RawName;
            }

            SyncRealCharacterHealth();
            SyncRealCharacterMana(snap);
            _serverCharacterHp = UserAccountService.CurrentCharacter.CurrentHealth;
            _serverCharacterMp = UserAccountService.CurrentCharacter.CurrentMana;

            // base.OnGroupCombatUpdated already raised property-changed notifications, but using
            // the stale server fields (the resync above happens after that). Raise again so the
            // focused monster's name/HP reflect the values we just synced.
            RaiseAll();
        }

        protected override void OnGroupCombatFinished(GroupCombatSnapshot snap)
        {
            _canActServer = false;
            base.OnGroupCombatFinished(snap);
            ApplyServerXpGain(snap);
            SyncRealCharacterHealth();
        }

        // Unlike solo combat (OnServerCombatUpdate below), the group-combat-finished snapshot
        // never used to touch the client's local Character at all - only SyncRealCharacterHealth
        // ran, which sets CurrentHealth but leaves MaxHealth/Level stale. Since SetHealth/SetMana
        // clamp to the (stale) local MaxHealth/MaxMana, this made the Healer look like it was
        // capping HP/MP at the previous level's max after a combat level-up. Apply the XP the
        // server actually granted (from GroupCombatEncounter.XpGrantedByCharacter) before syncing
        // health, so the level-up - and the resulting MaxHealth/MaxMana bump - always mirrors
        // what the server computed, never a local guess.
        private void ApplyServerXpGain(GroupCombatSnapshot snap)
        {
            if (!snap.CharactersWon) return;

            var character = UserAccountService.CurrentCharacter;
            var myState = snap.Characters.FirstOrDefault(c =>
                string.Equals(c.Name, character.Name, StringComparison.OrdinalIgnoreCase));
            if (myState == null) return;

            // Server already granted this loot to its own session Character
            // (GroupCombatEncounter.HandleMonsterDeath) - mirror it onto the client's Character too,
            // same reasoning as the solo-combat path in ApplyCombatTurnResult. Only the single living
            // recipient of a given kill has entries here, so this is a no-op for everyone else in
            // the party.
            if (myState.LootItemIds is { Count: > 0 })
            {
                foreach (var itemId in myState.LootItemIds)
                {
                    var drop = ItemFactory.CreateItem(itemId);
                    if (drop != null)
                        character.Inventory.AddItem(drop, character, "combat");
                }
            }

            if (myState.XpGained <= 0 && myState.RookieBonusXp <= 0) return;

            // Class XP mirroring stays as-is (separate progression track, not covered by
            // CharacterProgressResult) - only character Level/Experience now comes from the
            // server instead of being replayed locally.
            ClassManager.GrantClassXp(character, myState.XpGained);
            _ = SyncCharacterProgressAsync();
            _ = SyncQuestProgressAsync();
        }

        // Fetches the server's authoritative Level/Experience/stat progression and overwrites
        // the local Character with it, instead of trying to replay GainXp()/LevelUp() client
        // side - which drifted whenever a rookie bonus or level-gap XP scaling didn't exactly
        // match the server's math. Fire-and-forget: the UI briefly shows the pre-fight level
        // until this round-trip lands, same latency profile as every other server-authoritative
        // combat field already handled this way (HP/mana).
        private async Task SyncCharacterProgressAsync()
        {
            var progress = await GameHubService.GetCharacterProgressAsync();
            if (progress == null) return;
            var character = UserAccountService.CurrentCharacter;
            character.ApplySyncedProgress(progress);
            SkillFactory.UpdateSkills(character);
            RaiseAll();
        }

        // Fetches authoritative kill/item objective progress for every active quest and
        // overwrites the local Quest objects with it. Server-authoritative combat only ticks
        // QuestManager's counters against the server's own session Character - without this,
        // kill/item quest progress stays frozen client-side (showing 0/N or stale numbers)
        // until a full relog re-fetches the character from the server.
        private async Task SyncQuestProgressAsync()
        {
            var progress = await GameHubService.GetActiveQuestProgressAsync();
            if (progress.Count == 0) return;
            UserAccountService.CurrentCharacter.ApplySyncedQuestProgress(progress);
        }

        // Mirrors the local player's current GroupCharacters HP onto the real Character object
        // via SetHealth, so the main game page's character info panel (which only refreshes on
        // HealthChanged) stays in sync during and after multiplayer group fights.
        private void SyncRealCharacterHealth()
        {
            var character = UserAccountService.CurrentCharacter;
            var myVm = GroupCharacters.FirstOrDefault(p =>
                string.Equals(p.RawName, character.Name, StringComparison.OrdinalIgnoreCase));
            if (myVm != null) character.SetHealth(myVm.Hp);
        }

        // Mirrors SyncRealCharacterHealth for mana - GroupCombatantVm (the room-facing party list)
        // never carried a Mana field, so this reads straight off the raw snapshot DTO instead.
        private void SyncRealCharacterMana(GroupCombatSnapshot snap)
        {
            var character = UserAccountService.CurrentCharacter;
            var myState = snap.Characters.FirstOrDefault(c =>
                string.Equals(c.Name, character.Name, StringComparison.OrdinalIgnoreCase));
            if (myState != null) character.SetMana(myState.Mana);
        }

        // ── Exit / cleanup ───────────────────────────────────────────────────

        protected override void Run()
        {
            GameHubService.GroupCombatUpdated  -= OnGroupCombatUpdated;
            GameHubService.GroupCombatFinished -= OnGroupCombatFinished;
            GameHubService.CharacterRespawned     -= OnCharacterRespawned;
            _ = GameHubService.AbandonCombatAsync();
            base.Run();
        }

        // ── Solo server combat ───────────────────────────────────────────────

        private async Task InitServerCombatAsync(Character character)
        {
            var result = await GameHubService.StartCombatAsync(character.CurrentRoom.Id);
            if (result == null || !result.Success)
            {
                Run();
                return;
            }

            _serverEnemyName    = result.MonsterName ?? "";
            _serverEnemyHp      = result.MonsterHp;
            _serverEnemyHpMax   = result.MonsterMaxHp;
            _serverEnemyLevel   = result.MonsterLevel;
            _serverCharacterHp     = character.CurrentHealth;
            _serverCharacterHpMax  = character.MaxHealth;
            _serverCharacterMp     = character.CurrentMana;
            _serverCharacterMpMax  = character.MaxMana;
            _canActServer       = true;

            foreach (var (skill, source) in SkillSlotService.GetCombatSkills(character))
                Skills.Add(new FightSkillVm(skill, DetermineTag(character, skill, source)));

            LogLines.Add(LogLineVm.From("pg.fight.log.enemy_appears",
                Localization.T("pg.fight.log.enemy_appears", LocalizationText.LocalizeMonsterName(_serverEnemyName))));
            RaiseAll();
        }

        private void ApplyCombatTurnResult(CombatTurnResult? result)
        {
            if (result == null)
            {
                // The hub call failed or the connection dropped mid-action - re-enable the
                // button instead of leaving it permanently disabled with no way to retry.
                _canActServer = true;
                LogLines.Add(LogLineVm.From("pg.fight.log.connection_error",
                    Localization.T("pg.fight.log.connection_error")));
                RaiseAll();
                return;
            }

            foreach (var msg in result.LogEntries)
                LogLines.Add(LogLineVm.From(msg.Key,
                    Localization.T(msg.Key, LocalizationText.LocalizeMonsterArgs(msg.Args.Cast<object>()))));

            var character = UserAccountService.CurrentCharacter;

            // The server is authoritative for combat math — apply the HP it computed for
            // this turn. SetHealth (unlike assigning CurrentHealth directly) fires
            // HealthChanged, which is what keeps the main game page's character info panel
            // (CharacterHeaderVm) in sync during and after the fight.
            character.SetHealth(result.CharacterHp);
            character.SetMana(result.CharacterMana);
            _serverCharacterHp    = result.CharacterHp;
            _serverCharacterMp    = result.CharacterMana;
            _serverCharacterMpMax = result.CharacterMaxMana;
            _serverEnemyHp     = result.MonsterHp;

            if (result.Finished)
            {
                _serverFinished = true;
                _canActServer   = false;
                if (result.CharacterWon)
                {
                    GameHubService.CharacterRespawned -= OnCharacterRespawned;
                    // Class XP mirroring stays as-is (separate progression track, not covered
                    // by CharacterProgressResult) - only character Level/Experience now comes
                    // from the server instead of being replayed locally via GainXp/LevelUp.
                    ClassManager.GrantClassXp(character, result.XpGained);
                    _ = SyncCharacterProgressAsync();
                    _ = SyncQuestProgressAsync();

                    // Server already granted this loot to its own session Character (CombatEncounter.
                    // FinishCharacterWon) - mirror it onto the client's Character too, otherwise the
                    // drop never appears in the inventory UI or log until the next relog re-fetches
                    // the server's copy.
                    foreach (var itemId in result.LootItemIds)
                    {
                        var drop = ItemFactory.CreateItem(itemId);
                        if (drop != null)
                            character.Inventory.AddItem(drop, character, "combat");
                    }

                    ViewModel_PageRoom.WriteLog(
                        $"{LocalizationText.LocalizeMonsterName(_serverEnemyName)} {Localization.T("msg.fight.won")}");
                }
                Navigation.Current.SetFightState(false);
                Navigation.Current.Navigate(Nav.Room);
            }
            else
            {
                _canActServer = result.Phase == "CharacterTurn";
            }

            RaiseAll();
        }

        // ── Combat actions ───────────────────────────────────────────────────

        protected override async Task AttackAsync()
        {
            if (_isGroupCombat && _isLocalGroupCombat && _groupEncounter != null)
            {
                var lp = UserAccountService.CurrentCharacter;
                _groupEncounter.CharacterAttack(lp.Name, _selectedMonsterIndex);
                SyncFromGroupEncounter();
                FlushGroupLog();
                if (!_groupEncounter.IsFinished)
                {
                    CurrentTurnCharacterName = _groupEncounter.CurrentTurnCharacterName;
                    IsMyTurn      = string.Equals(CurrentTurnCharacterName, lp.Name, StringComparison.OrdinalIgnoreCase);
                    _canActServer = IsMyTurn;
                }
                RaiseAll();
                CheckLocalGroupFinished();
                return;
            }

            if (_isGroupCombat)
            {
                _canActServer = false;
                var snap = await GameHubService.GroupCharacterAttackAsync(_selectedMonsterIndex);
                if (snap == null || snap.Characters.Count == 0)
                    await ResyncGroupCombatStateAsync();
                return;
            }

            if (_encounter != null)
            {
                _encounter.CharacterAttack();
                FlushNewLogEntries();
                CheckLocalCombatFinished();
                RaiseAll();
            }
            else if (GameHubService.IsConnected)
            {
                _canActServer = false;
                var result = await GameHubService.CharacterAttackAsync();
                ApplyCombatTurnResult(result);
            }
        }

        protected override async Task CastSkillAsync(FightSkillVm? vm)
        {
            if (vm == null) return;

            if (_isGroupCombat && _isLocalGroupCombat && _groupEncounter != null)
            {
                int targetIdx = vm.Skill.Target == SkillTarget.SingleAlly ? 0 : _selectedMonsterIndex;
                var lp = UserAccountService.CurrentCharacter;
                _groupEncounter.CharacterCastSkill(lp.Name, vm.Skill, targetIdx);
                SyncFromGroupEncounter();
                FlushGroupLog();
                if (!_groupEncounter.IsFinished)
                {
                    CurrentTurnCharacterName = _groupEncounter.CurrentTurnCharacterName;
                    IsMyTurn      = string.Equals(CurrentTurnCharacterName, lp.Name, StringComparison.OrdinalIgnoreCase);
                    _canActServer = IsMyTurn;
                }
                RaiseAll();
                CheckLocalGroupFinished();
                return;
            }

            if (_isGroupCombat)
            {
                int targetIdx = vm.Skill.Target == SkillTarget.SingleAlly ? 0 : _selectedMonsterIndex;
                _canActServer = false;
                var snap = await GameHubService.GroupCharacterCastSkillAsync(vm.Skill.Id ?? vm.Skill.Name, targetIdx);
                if (snap == null || snap.Characters.Count == 0)
                    await ResyncGroupCombatStateAsync();
                return;
            }

            if (_encounter != null)
            {
                _encounter.CharacterBeginCast(vm.Skill);
                FlushNewLogEntries();
                CheckLocalCombatFinished();
                RaiseAll();
            }
            else if (GameHubService.IsConnected)
            {
                _canActServer = false;
                var result = await GameHubService.CharacterCastSkillAsync(vm.Skill.Id ?? vm.Skill.Name);
                ApplyCombatTurnResult(result);
            }
        }

        protected override void CheckLocalGroupFinished()
        {
            if (_groupEncounter?.IsFinished == true)
                _canActServer = false;
            base.CheckLocalGroupFinished();
        }

        /// <summary>
        /// Recovery path for when an attack/cast came back as an empty snapshot - rather than
        /// just re-enabling the action button based on the client's own (possibly wrong) belief
        /// about whose turn it is, ask the server what's actually true and apply it. This is what
        /// keeps a party fight from getting permanently stuck on one player after any missed
        /// broadcast, stale reconnect, or turn-order mixup: the very next failed attempt
        /// self-heals instead of repeating the same wrong guess forever.
        /// </summary>
        private async Task ResyncGroupCombatStateAsync()
        {
            var snap = await GameHubService.GetGroupCombatStateAsync();
            if (snap is null)
            {
                // Server doesn't think we're in a fight at all - nothing left to resync to, so
                // stop trying to act here instead of leaving the page stuck waiting forever.
                _canActServer = false;
                ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.desynced"));
                Navigation.Current.SetFightState(false);
                Navigation.Current.Navigate(Nav.Room);
                return;
            }

            if (snap.Finished)
                OnGroupCombatFinished(snap);
            else
                OnGroupCombatUpdated(snap);
        }
    }
}
