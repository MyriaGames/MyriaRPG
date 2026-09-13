using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Services;
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
        public int ManaCost => _skill.ManaCost;
        public int MinLevel => _skill.MinLevel;
        public bool IsHealing => _skill.IsHealing;
        public string TimingText => _skill.TimingText;
        public string ScalingText => $"{_skill.ScalingFactor:0.##} × {_skill.StatToScaleFrom}";

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
            UpgradeOptions.Clear();
            if (_baseSkill == null) return;

            SkillLevelingService.RecalculateLevelAndPoints(_character, _baseSkill.Id);
            _progress = SkillLevelingService.GetOrCreate(_character, _baseSkill.Id);

            foreach (var option in _baseSkill.UpgradeOptions)
            {
                bool purchased = _progress.PurchasedUpgradeIds.Contains(option.Id);
                UpgradeOptions.Add(new UpgradeOptionVm(option, purchased, !purchased && _progress.UnspentPoints > 0));
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
        public bool IsPurchased { get; }
        public bool NotPurchased => !IsPurchased;
        public bool CanPurchase { get; }

        public UpgradeOptionVm(SkillUpgradeOption option, bool isPurchased, bool canPurchase)
        {
            Id = option.Id;
            Description = option.Description;
            IsPurchased = isPurchased;
            CanPurchase = canPurchase;
        }
    }
}
