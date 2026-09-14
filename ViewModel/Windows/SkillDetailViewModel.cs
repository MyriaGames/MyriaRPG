using Myria.Lib.Core.Entities.Effects;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Windows
{
    /// <summary>
    /// Where a skill's leveling progress is actually spent - mirrors CharacterPageViewModel's
    /// stat-point flow (optimistic local mutation, synced to the server when connected) but per
    /// skill instead of per character. See SkillLevelingService/v0.3.md's skill leveling design.
    /// </summary>
    public class SkillDetailViewModel : BaseViewModel
    {
        private readonly SkillVm _skill;
        private readonly Myria.Lib.Core.Entities.Characters.Character _character;
        private readonly Skill? _baseSkill;
        private SkillProgress? _progress;

        /// <summary>The skill's current, character-specific numbers - base-nerf fade and purchased
        /// upgrades applied - re-resolved every Refresh() so it never goes stale after a purchase/
        /// respec on this same screen. Falls back to the raw _skill for non-base entries (the Rune
        /// tab) that leveling doesn't apply to. Never null after the constructor runs Refresh().</summary>
        private SkillVm _effectiveSkill = null!;

        public SkillDetailViewModel(SkillVm skill)
        {
            _skill = skill;
            _character = UserAccountService.CurrentCharacter;
            _baseSkill = _character.Skills.FirstOrDefault(s => s.Id == skill.Id);

            SpendPointCommand = new RelayCommand<UpgradeOptionVm?>(SpendPoint);
            RespecCommand = new RelayCommand(Respec);

            Refresh();
        }

        public string Name => _skill.Name;
        public string ClassName => _skill.ClassName;
        public string TypeText => _skill.TypeText;
        public string TargetText => _skill.TargetText;
        public string Description => _skill.Description;
        public int ManaCost => _effectiveSkill.ManaCost;
        public int MinLevel => _skill.MinLevel;
        public bool IsHealing => _skill.IsHealing;
        public string TimingText => _effectiveSkill.TimingText;
        public string ScalingText => $"{_effectiveSkill.ScalingFactor:0.##} × {_effectiveSkill.StatToScaleFrom}";

        /// <summary>What this skill actually *does* beyond its raw numbers - poison, stun,
        /// lifesteal, etc, including any effect a purchased upgrade has newly granted. Rebuilt every
        /// Refresh() from the current effective skill's Effects (see _effectiveSkill), not just once
        /// at construction, so a newly-bought "adds an effect" upgrade shows up immediately - the
        /// eventual magnitude still isn't shown here, this is deliberately just the qualitative
        /// "what happens" text.</summary>
        public ObservableCollection<SkillEffectVm> Effects { get; } = new();
        public bool HasEffects => Effects.Count > 0;

        private void PopulateEffects()
        {
            Effects.Clear();
            foreach (var entry in _effectiveSkill.RawEffects)
            {
                var def = EffectFactory.GetDefinition(entry.EffectId);
                if (def == null) continue; // unknown/removed effect id - skip rather than show garbage
                Effects.Add(new SkillEffectVm(def.Name, def.Description, EffectTargetText(entry.ApplyTo)));
            }
        }

        private static string EffectTargetText(EffectTarget target) => target switch
        {
            EffectTarget.Caster    => Localization.T("pg.skills.effect_target.caster"),
            EffectTarget.AllAllies => Localization.T("pg.skills.effect_target.all_allies"),
            _                      => Localization.T("pg.skills.effect_target.target")
        };

        /// <summary>False for a rune/other non-base skill this character's Skills list doesn't
        /// contain by id (e.g. the Rune tab's entries) - leveling only applies to base skills.</summary>
        public bool HasProgress => _baseSkill != null;

        public int Level => _progress?.Level ?? 1;
        public int UsageCount => _progress?.UsageCount ?? 0;
        public int UnspentPoints => _progress?.UnspentPoints ?? 0;
        public bool HasUnspentPoints => UnspentPoints > 0;
        public bool HasPurchasedUpgrades => _progress?.PurchasedUpgradeIds.Count > 0;
        public long RespecCost => SkillLevelingService.RespecCostGold;

        public ObservableCollection<UpgradeOptionVm> UpgradeOptions { get; } = new();
        public bool HasUpgradeOptions => UpgradeOptions.Count > 0;
        public bool NoUpgradeOptions => !HasUpgradeOptions;

        public ICommand SpendPointCommand { get; }
        public ICommand RespecCommand { get; }

        private void Refresh()
        {
            _effectiveSkill = _baseSkill != null
                ? new SkillVm(SkillLevelingService.ResolveEffectiveSkill(_character, _baseSkill), _skill.Tag)
                : _skill; // rune/non-base entry - leveling doesn't apply, show its own raw numbers

            PopulateEffects();
            OnPropertyChanged(nameof(ManaCost));
            OnPropertyChanged(nameof(TimingText));
            OnPropertyChanged(nameof(ScalingText));
            OnPropertyChanged(nameof(HasEffects));

            UpgradeOptions.Clear();
            if (_baseSkill == null) return;

            SkillLevelingService.RecalculateLevelAndPoints(_character, _baseSkill.Id);
            _progress = SkillLevelingService.GetOrCreate(_character, _baseSkill.Id);

            foreach (var option in _baseSkill.UpgradeOptions)
            {
                int timesPurchased = _progress.PurchasedUpgradeIds.Count(id => id == option.Id);
                int maxPurchases = SkillLevelingService.GetEffectiveMaxPurchases(option);
                bool isMaxed = maxPurchases > 0 && timesPurchased >= maxPurchases;
                bool isLocked = _progress.Level < option.RequiredLevel;
                bool canPurchase = !isLocked && !isMaxed && _progress.UnspentPoints > 0;
                UpgradeOptions.Add(new UpgradeOptionVm(option, timesPurchased, maxPurchases, isLocked, canPurchase));
            }

            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(UsageCount));
            OnPropertyChanged(nameof(UnspentPoints));
            OnPropertyChanged(nameof(HasUnspentPoints));
            OnPropertyChanged(nameof(HasPurchasedUpgrades));
            OnPropertyChanged(nameof(HasUpgradeOptions));
            OnPropertyChanged(nameof(NoUpgradeOptions));
        }

        private async void SpendPoint(UpgradeOptionVm? vm)
        {
            if (vm == null || _baseSkill == null || !vm.CanPurchase) return;

            bool ok;
            if (GameHubService.IsConnected)
            {
                ok = await GameHubService.SpendSkillPointAsync(_baseSkill.Id, vm.Id);
            }
            else
            {
                ok = SkillLevelingService.TrySpendPoint(_character, _baseSkill, vm.Id, out _);
                if (ok) CharacterService.SaveCharacter(UserAccountService.CurrentUser, _character);
            }
            if (ok) Refresh();
        }

        private async void Respec()
        {
            if (_baseSkill == null || !HasPurchasedUpgrades) return;

            bool ok;
            if (GameHubService.IsConnected)
            {
                ok = await GameHubService.RespecSkillAsync(_baseSkill.Id);
            }
            else
            {
                ok = _character.Money.TrySpend(SkillLevelingService.RespecCostGold);
                if (ok)
                {
                    SkillLevelingService.Respec(_character, _baseSkill.Id);
                    CharacterService.SaveCharacter(UserAccountService.CurrentUser, _character);
                }
            }
            if (ok) Refresh();
        }
    }

    public class UpgradeOptionVm
    {
        public string Id { get; }
        public string Description { get; }
        public int TimesPurchased { get; }
        public bool HasPurchases => TimesPurchased > 0;

        /// <summary>0 means this upgrade can be bought an unlimited number of times, stacking its
        /// deltas again each time - see SkillLevelingService.GetEffectiveMaxPurchases.</summary>
        public int MaxPurchases { get; }
        public bool IsUnlimited => MaxPurchases <= 0;
        public bool IsMaxed => !IsUnlimited && TimesPurchased >= MaxPurchases;

        public int RequiredLevel { get; }
        public bool IsLocked { get; }

        public bool CanPurchase { get; }

        /// <summary>True once this upgrade can never be bought right now for a reason other than
        /// points (locked behind a level, or already at its purchase cap) - drives which of the Buy
        /// button / UnavailableReasonText shows, mutually exclusively. A purely points-blocked
        /// option (still available, just no points yet) still shows the (disabled) Buy button.</summary>
        public bool IsUnavailable => IsLocked || IsMaxed;
        public bool IsAvailable => !IsUnavailable;

        /// <summary>What the Buy button's row shows instead of the button once this upgrade is no
        /// longer purchasable for a reason other than "not enough points" - e.g. "Maxed (2/2)" or
        /// "Unlocks at level 11". Empty while CanPurchase is true or the only blocker is points.</summary>
        public string UnavailableReasonText { get; }
        public bool HasUnavailableReasonText => !string.IsNullOrEmpty(UnavailableReasonText);

        /// <summary>e.g. "Also adds: Weak Poison" - empty when this upgrade doesn't grant a new
        /// effect (most don't; they just adjust the skill's existing numbers).</summary>
        public string AddsEffectText { get; }
        public bool HasAddsEffectText => !string.IsNullOrEmpty(AddsEffectText);

        public UpgradeOptionVm(SkillUpgradeOption option, int timesPurchased, int maxPurchases, bool isLocked, bool canPurchase)
        {
            Id = option.Id;
            Description = option.Description;
            TimesPurchased = timesPurchased;
            MaxPurchases = maxPurchases;
            RequiredLevel = option.RequiredLevel;
            IsLocked = isLocked;
            CanPurchase = canPurchase;

            UnavailableReasonText = isLocked
                ? Localization.T("pg.skills.details.unlocks_at_level", option.RequiredLevel)
                : (!IsUnlimited && TimesPurchased >= maxPurchases
                    ? Localization.T("pg.skills.details.maxed", TimesPurchased, maxPurchases)
                    : "");

            var names = option.AddedEffects
                .Select(e => EffectFactory.GetDefinition(e.EffectId)?.Name)
                .Where(n => !string.IsNullOrEmpty(n));
            var joined = string.Join(", ", names);
            AddsEffectText = string.IsNullOrEmpty(joined) ? "" : Localization.T("pg.skills.details.adds_effect", joined);
        }
    }

    public class SkillEffectVm
    {
        public string Name { get; }
        public string Description { get; }
        public string TargetText { get; }

        public SkillEffectVm(string name, string description, string targetText)
        {
            Name = name;
            Description = description;
            TargetText = targetText;
        }
    }
}
