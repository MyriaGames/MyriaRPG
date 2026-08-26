using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class SkillSlotViewModel : BaseViewModel
    {
        private string _tblTitle = string.Empty;
        private string _tblAvailable = string.Empty;
        private string _tblActiveSlots = string.Empty;
        private string _tblSlot = string.Empty;
        private string _tblUnslot = string.Empty;
        private string _tblRegular = string.Empty;
        private string _tblCombined = string.Empty;
        private string _tblFusion = string.Empty;
        private string _tblBack = string.Empty;
        [LocalizedKey("pg.skill_slots.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.available")]
        public string TblAvailable
        {
            get => _tblAvailable;
            set { _tblAvailable = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.active")]
        public string TblActiveSlots
        {
            get => _tblActiveSlots;
            set { _tblActiveSlots = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.slot")]
        public string TblSlot
        {
            get => _tblSlot;
            set { _tblSlot = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.unslot")]
        public string TblUnslot
        {
            get => _tblUnslot;
            set { _tblUnslot = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.regular")]
        public string TblRegular
        {
            get => _tblRegular;
            set { _tblRegular = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.combined")]
        public string TblCombined
        {
            get => _tblCombined;
            set { _tblCombined = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skill_slots.fusion")]
        public string TblFusion
        {
            get => _tblFusion;
            set { _tblFusion = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(); }
        }

        public string SlotCountText => $"{_player.SkillSlots.Count} / {_player.SkillSlotCount} slots";

        public bool HasCombinedSkills => AvailableCombinedSkills.Count > 0;
        public bool HasFusionSkills => AvailableFusionSkills.Count > 0;

        public ObservableCollection<SlottableSkillVm> AvailableRegularSkills { get; } = new();
        public ObservableCollection<SlottableSkillVm> AvailableCombinedSkills { get; } = new();
        public ObservableCollection<SlottableSkillVm> AvailableFusionSkills { get; } = new();
        public ObservableCollection<ActiveSlotVm> ActiveSlots { get; } = new();

        public ICommand SlotSkillCommand { get; }
        public ICommand UnslotSkillCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand GoBackCommand { get; }

        protected readonly Character _player;

        public SkillSlotViewModel()
        {
            _player = UserAccountService.CurrentCharacter;

            SlotSkillCommand = new RelayCommand<SlottableSkillVm?>(SlotSkill);
            UnslotSkillCommand = new RelayCommand<ActiveSlotVm?>(UnslotSkill);
            MoveUpCommand = new RelayCommand<ActiveSlotVm?>(MoveUp);
            MoveDownCommand = new RelayCommand<ActiveSlotVm?>(MoveDown);
            GoBackCommand = new RelayCommand(() => Navigation.Current.Navigate(new Page_Skills()));

            Refresh();
        }

        protected void Refresh()
        {
            AvailableRegularSkills.Clear();
            AvailableCombinedSkills.Clear();
            AvailableFusionSkills.Clear();
            ActiveSlots.Clear();

            bool atCap = _player.SkillSlots.Count >= _player.SkillSlotCount;

            foreach (var s in _player.Skills)
            {
                bool slotted = _player.SkillSlots.Any(sl => sl.Source == SlottedSkillSource.Regular && sl.SkillId == s.Id);
                AvailableRegularSkills.Add(new SlottableSkillVm(s.Id, s.Name, s.Type.ToString(), s.Target.ToString(),
                    SlottedSkillSource.Regular, slotted, atCap));
            }

            foreach (var c in _player.CombinedSkills.Where(c => c.ResolvedSkill != null))
            {
                bool slotted = _player.SkillSlots.Any(sl => sl.Source == SlottedSkillSource.Combined && sl.SkillId == c.Id);
                var sk = c.ResolvedSkill!;
                AvailableCombinedSkills.Add(new SlottableSkillVm(c.Id, c.DisplayName, sk.Type.ToString(), sk.Target.ToString(),
                    SlottedSkillSource.Combined, slotted, atCap));
            }

            foreach (var f in _player.CompositeSkills.Where(f => f.ResolvedSkill != null))
            {
                bool slotted = _player.SkillSlots.Any(sl => sl.Source == SlottedSkillSource.CompositeFusion && sl.SkillId == f.Id);
                var sk = f.ResolvedSkill!;
                AvailableFusionSkills.Add(new SlottableSkillVm(f.Id, f.DisplayName, sk.Type.ToString(), sk.Target.ToString(),
                    SlottedSkillSource.CompositeFusion, slotted, atCap));
            }

            int idx = 1;
            foreach (var slot in _player.SkillSlots)
                ActiveSlots.Add(new ActiveSlotVm(idx++, slot));

            OnPropertyChanged(nameof(SlotCountText));
            OnPropertyChanged(nameof(HasCombinedSkills));
            OnPropertyChanged(nameof(HasFusionSkills));
        }

        protected virtual void SlotSkill(SlottableSkillVm? vm)
        {
            if (vm == null) return;
            SkillSlotService.TryAddSlot(_player, vm.Source, vm.SkillId);
            CharacterService.SaveCharacter(UserAccountService.CurrentUser, _player);
            Refresh();
        }

        protected virtual void UnslotSkill(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            SkillSlotService.RemoveSlot(_player, vm.Source, vm.SkillId);
            CharacterService.SaveCharacter(UserAccountService.CurrentUser, _player);
            Refresh();
        }

        protected virtual void MoveUp(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            int idx = _player.SkillSlots.FindIndex(s => s.Source == vm.Source && s.SkillId == vm.SkillId);
            SkillSlotService.ReorderSlots(_player, idx, idx - 1);
            CharacterService.SaveCharacter(UserAccountService.CurrentUser, _player);
            Refresh();
        }

        protected virtual void MoveDown(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            int idx = _player.SkillSlots.FindIndex(s => s.Source == vm.Source && s.SkillId == vm.SkillId);
            SkillSlotService.ReorderSlots(_player, idx, idx + 1);
            CharacterService.SaveCharacter(UserAccountService.CurrentUser, _player);
            Refresh();
        }
    }

    public class SlottableSkillVm
    {
        public string SkillId { get; }
        public string Name { get; }
        public string TypeAndTarget { get; }
        public SlottedSkillSource Source { get; }
        public bool IsSlotted { get; }
        public bool CanSlot { get; }

        public SlottableSkillVm(string id, string name, string type, string target,
            SlottedSkillSource source, bool slotted, bool atCap)
        {
            SkillId = id;
            Name = name;
            TypeAndTarget = $"{type} · {target}";
            Source = source;
            IsSlotted = slotted;
            CanSlot = !slotted && !atCap;
        }
    }

    public class ActiveSlotVm
    {
        private readonly SkillSlot _slot;

        public int SlotNumber { get; }
        public string SkillId => _slot.SkillId;
        public SlottedSkillSource Source => _slot.Source;
        public string SkillName => _slot.ResolvedSkill?.Name ?? _slot.SkillId;
        public string SourceTag => _slot.Source switch
        {
            SlottedSkillSource.Combined => "Combined",
            SlottedSkillSource.CompositeFusion => "Fusion",
            _ => ""
        };
        public bool HasSourceTag => !string.IsNullOrEmpty(SourceTag);

        public ActiveSlotVm(int number, SkillSlot slot)
        {
            SlotNumber = number;
            _slot = slot;
        }
    }
}
