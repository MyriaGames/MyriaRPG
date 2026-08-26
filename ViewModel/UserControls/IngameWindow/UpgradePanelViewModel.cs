using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class UpgradePanelViewModel : BaseViewModel
    {
        protected readonly Npc _npc;
        protected readonly Character _character;
        private readonly Action _goBack;

        public string Title => Localization.T("npc.upgrade.title");
        public string BtnBack => Localization.T("app.general.UI.back");
        public string BtnUpgrade => Localization.T("npc.upgrade.button");
        public string CostLabel => Localization.T("npc.upgrade.cost");

        public ObservableCollection<EquipmentItemVm> Equipment { get; } = new();

        private EquipmentItemVm _selectedEquipment;
        public EquipmentItemVm SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                _selectedEquipment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedName));
                OnPropertyChanged(nameof(SelectedLevel));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(CanUpgrade));
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string SelectedName => SelectedEquipment?.DisplayName ?? "";
        public string SelectedLevel => SelectedEquipment != null ? $"{Localization.T("npc.upgrade.level")}: +{SelectedEquipment.UpgradeLevel}" : "";

        protected int KnowledgeMaxUpgradeLevel
        {
            get
            {
                string jobId = _npc.MasterJobId ?? "blacksmith";
                int knowledgeLevel = JobXpService.GetLevel(JobManager.GetOrAdd(_character, jobId).KnowledgeXp);
                return JobXpService.GetMaxUpgradeLevel(knowledgeLevel);
            }
        }

        public string UpgradeCost
        {
            get
            {
                if (SelectedEquipment == null) return "";
                if (SelectedEquipment.UpgradeLevel >= KnowledgeMaxUpgradeLevel) return Localization.T("npc.upgrade.maxLevel");
                int have = GetMaterialCount(SelectedEquipment.UpgradeMaterialId);
                string mat = Localization.T($"item.{SelectedEquipment.UpgradeMaterialId}");
                return $"{CostLabel}: {SelectedEquipment.UpgradeMaterialCount}× {mat}  ({Localization.T("npc.upgrade.youHave")}: {have})";
            }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanUpgrade => SelectedEquipment != null
            && SelectedEquipment.UpgradeLevel < KnowledgeMaxUpgradeLevel
            && GetMaterialCount(SelectedEquipment.UpgradeMaterialId) >= SelectedEquipment.UpgradeMaterialCount;

        private int GetMaterialCount(string materialId) =>
            _character.Inventory.Items.Where(i => i.Id == materialId).Sum(i => i.StackSize);

        /// <summary>Resolves a raw item id (as returned by NpcActionResult/server DTOs) to its localized display name.</summary>
        protected static string ResolveItemName(string itemId) => Localization.T($"item.{itemId}");

        private static string UpgradeMaterialFor(string? category) => category switch
        {
            "leathersmith" => "cured_leather",
            "tailor"       => "bolt_of_cloth",
            "artificer"    => "earth_essence",
            _              => "iron_ingot"
        };

        public ICommand BackCommand { get; }
        public ICommand UpgradeCommand { get; }

        public UpgradePanelViewModel(Npc npc, Character character, Action goBack)
        {
            _npc = npc;
            _character = character;
            _goBack = goBack;

            BackCommand = new RelayCommand(_goBack);
            UpgradeCommand = new RelayCommand(UpgradeSelected);

            LoadUpgradable();
        }

        protected void LoadUpgradable(string? keepSelectedId = null)
        {
            Equipment.Clear();
            StatusMessage = "";

            string mat = UpgradeMaterialFor(_npc.UpgradeCategory);
            foreach (var eq in _character.Inventory.Items.OfType<EquipmentItem>()
                         .Where(e => e.UpgradeCategory == _npc.UpgradeCategory))
            {
                Equipment.Add(new EquipmentItemVm(eq, mat));
            }

            if (Equipment.Count == 0)
            {
                StatusMessage = Localization.T("npc.upgrade.noEquipment");
                return;
            }

            SelectedEquipment = (keepSelectedId != null
                ? Equipment.FirstOrDefault(e => e.Id == keepSelectedId)
                : null) ?? Equipment.FirstOrDefault();
        }

        private void UpgradeSelected()
        {
            if (SelectedEquipment == null) return;
            _ = ExecuteUpgrade();
        }

        protected virtual Task ExecuteUpgrade()
        {
            UpgradeLocal();
            return Task.CompletedTask;
        }

        private void UpgradeLocal()
        {
            if (SelectedEquipment == null) return;
            string currentId = SelectedEquipment.Id;

            EquipmentItem eq = SelectedEquipment.Item;
            if (eq == null || !_character.Inventory.Items.Contains(eq))
            {
                StatusMessage = Localization.T("npc.upgrade.fail");
                return;
            }

            // Delegates to CraftExecutionService.Upgrade — the same method the multiplayer server
            // now calls (GameHub.Upgrade) — instead of this ViewModel's own independent copy, which
            // (unlike the server) never re-checked the knowledge gate before consuming materials,
            // relying entirely on the Upgrade button already being disabled at the knowledge cap.
            var outcome = CraftExecutionService.Upgrade(_character, _npc, eq);

            if (outcome.Success)
            {
                StatusMessage = Localization.T("npc.action.upgrade.ok", ResolveItemName(eq.Id), eq.UpgradeLevel);
                LoadUpgradable(currentId);
            }
            else
            {
                StatusMessage = Localization.T("npc.upgrade.fail");
            }
        }
    }

    public class EquipmentItemVm : BaseViewModel
    {
        private EquipmentItem _item;

        public EquipmentItem Item => _item;
        public string Id => _item.Id;
        public string Name => Localization.T($"item.{_item.Id}");
        public string DisplayName => UpgradeLevel >= 1 ? $"{Name} +{UpgradeLevel}" : Name;
        public int UpgradeLevel => _item.UpgradeLevel;

        public string UpgradeMaterialId { get; }
        public int UpgradeMaterialCount => _item.UpgradeLevel < 4 ? 1
                                         : _item.UpgradeLevel < 7 ? 2
                                                                   : 3;

        public EquipmentItemVm(EquipmentItem item, string upgradeMaterial)
        {
            _item = item;
            UpgradeMaterialId = upgradeMaterial;
        }

        public override string ToString() => DisplayName;
    }
}
