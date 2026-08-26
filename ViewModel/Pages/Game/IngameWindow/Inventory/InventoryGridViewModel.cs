using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    /// <summary>
    /// ViewModel for the inventory item grid (slots, drag/drop, tooltip).
    /// Reusable wherever the inventory grid is shown (standalone inventory page, shop sell panel, etc.)
    /// </summary>
    public class InventoryGridViewModel : BaseViewModel
    {
        protected Character _character;
        private Dictionary<string, int> _gridPositions = new();
        private string _inventoryTitle;
        private ItemTooltipViewModel _currentTooltip;
        private bool _isTooltipVisible;
        private string? _notificationMessage;
        private DispatcherTimer? _notificationTimer;

        private const string INVENTORY_LAYOUT_FILE = "Data/player_inventory_layout.json";

        public ObservableCollection<InventoryItemViewModel> InventoryItems { get; } = new();

        [LocalizedKey("app.general.UI.inventory")]
        public string InventoryTitle
        {
            get => _inventoryTitle;
            set => SetProperty(ref _inventoryTitle, value);
        }

        public ItemTooltipViewModel CurrentTooltip
        {
            get => _currentTooltip;
            set => SetProperty(ref _currentTooltip, value);
        }

        public bool IsTooltipVisible
        {
            get => _isTooltipVisible;
            set => SetProperty(ref _isTooltipVisible, value);
        }

        public string LblEquip     => Localization.T("pg.inventory.context.equip");
        public string LblUse       => Localization.T("pg.inventory.context.use");
        public string LblSellOne   => Localization.T("npc.shop.context.sell_one");
        public string LblSellStack => Localization.T("npc.shop.context.sell_stack");

        public string? NotificationMessage
        {
            get => _notificationMessage;
            private set
            {
                SetProperty(ref _notificationMessage, value);
                OnPropertyChanged(nameof(IsNotificationVisible));
            }
        }

        public bool IsNotificationVisible => _notificationMessage != null;

        public ICommand EquipItemCommand { get; }
        public ICommand UseItemCommand { get; }
        public ICommand SellItemCommand { get; }
        public ICommand SellOneCommand { get; }
        public ICommand SellStackCommand { get; }
        public ICommand ShowTooltipCommand { get; }
        public ICommand HideTooltipCommand { get; }
        public ICommand DismissNotificationCommand { get; }

        public InventoryGridViewModel(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _currentTooltip = new ItemTooltipViewModel();

            EquipItemCommand = new RelayCommand<InventoryItemViewModel>(EquipItem);
            UseItemCommand = new RelayCommand<InventoryItemViewModel>(UseItem);
            SellItemCommand = new RelayCommand<InventoryItemViewModel>(SellItem);
            SellOneCommand = new RelayCommand<InventoryItemViewModel>(vm => SellAmount(vm, 1));
            SellStackCommand = new RelayCommand<InventoryItemViewModel>(vm => SellAmount(vm, vm?.Item?.StackSize ?? 0));
            ShowTooltipCommand = new RelayCommand<InventoryItemViewModel>(ShowTooltip);
            HideTooltipCommand = new RelayCommand(HideTooltip);
            DismissNotificationCommand = new RelayCommand(() => NotificationMessage = null);

            _character.Inventory.ItemReceived += (s, e) => RefreshInventory();
            _character.Inventory.ItemRemoved += (s, e) => RefreshInventory();

            LoadInventoryLayout();
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            InventoryItems.Clear();
            for (int i = 0; i < _character.Inventory.Items.Count; i++)
                InventoryItems.Add(new InventoryItemViewModel(_character.Inventory.Items[i], i));
        }

        public void HandleItemDrop(InventoryItemViewModel draggedItem, int targetSlotIndex)
        {
            if (draggedItem?.Item == null) return;

            if (draggedItem.Item is EquipmentItem equipment)
            {
                bool wasEquipped = false;
                if (_character.WeaponSlot == equipment)        { _character.WeaponSlot = null;    wasEquipped = true; }
                else if (_character.ArmorSlot == equipment)    { _character.ArmorSlot = null;     wasEquipped = true; }
                else if (_character.AccessorySlot == equipment){ _character.AccessorySlot = null; wasEquipped = true; }

                if (wasEquipped)
                {
                    _character.Inventory.AddItem(equipment, _character, "unequip");
                    RefreshInventory();
                    return;
                }
                // Equipment is already in inventory — fall through to normal repositioning
            }

            _gridPositions[draggedItem.Item.Id] = targetSlotIndex;
            SaveInventoryLayout();
            RefreshInventory();
        }

        private void EquipItem(InventoryItemViewModel itemViewModel)
        {
            if (itemViewModel?.Item is not EquipmentItem equipment) return;
            if (!equipment.IsUsableBy(_character))
            {
                ShowNotification(Localization.T("pg.inventory.wrong_class"));
                return;
            }
            ExecuteEquip(equipment);
        }

        protected virtual void ExecuteEquip(EquipmentItem equipment)
        {
            _character.Inventory.SwapEquipment(equipment.Id, _character);
            RefreshInventory();
        }

        private void UseItem(InventoryItemViewModel itemViewModel)
        {
            if (itemViewModel?.Item is not ConsumableItem consumable) return;
            ExecuteUse(consumable);
        }

        protected virtual void ExecuteUse(ConsumableItem consumable)
        {
            _character.Inventory.UseItem(consumable.Id, _character);
            RefreshInventory();
        }

        private void SellItem(InventoryItemViewModel itemViewModel) =>
            SellAmount(itemViewModel, itemViewModel?.Item?.StackSize ?? 0);

        protected virtual void SellAmount(InventoryItemViewModel itemViewModel, int amount)
        {
            if (itemViewModel?.Item == null || amount <= 0) return;
            if (_character.Inventory.SellItem(itemViewModel.Item.Name, amount, ref _character))
                RefreshInventory();
        }

        private void ShowTooltip(InventoryItemViewModel itemViewModel)
        {
            if (itemViewModel?.Item == null) return;
            CurrentTooltip.SetItem(itemViewModel.Item, _character);
            IsTooltipVisible = true;
        }

        private void HideTooltip() => IsTooltipVisible = false;

        protected void ShowNotification(string message)
        {
            NotificationMessage = message;
            _notificationTimer?.Stop();
            _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _notificationTimer.Tick += (s, e) =>
            {
                NotificationMessage = null;
                _notificationTimer?.Stop();
            };
            _notificationTimer.Start();
        }

        private void LoadInventoryLayout()
        {
            try
            {
                if (File.Exists(INVENTORY_LAYOUT_FILE))
                    _gridPositions = JsonSerializer.Deserialize<Dictionary<string, int>>(
                        File.ReadAllText(INVENTORY_LAYOUT_FILE)) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load inventory layout: {ex.Message}");
            }
        }

        private void SaveInventoryLayout()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(INVENTORY_LAYOUT_FILE) ?? "Data");
                File.WriteAllText(INVENTORY_LAYOUT_FILE, JsonSerializer.Serialize(_gridPositions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save inventory layout: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ViewModel for a single inventory slot.
    /// </summary>
    public class InventoryItemViewModel : BaseViewModel
    {
        private Item _item;
        private int _index;
        private Brush _rarityBrush;

        public Item Item { get => _item; set => SetProperty(ref _item, value); }
        public int Index { get => _index; set => SetProperty(ref _index, value); }
        public Brush RarityBrush { get => _rarityBrush; set => SetProperty(ref _rarityBrush, value); }
        public bool IsStack     => (_item?.StackSize ?? 0) > 1;
        public bool IsEquipment => _item is EquipmentItem;
        public bool IsConsumable => _item is ConsumableItem;
        public string SellOneText   => $"{Localization.T("npc.shop.context.sell_one")} ({_item?.SellValue ?? 0})";
        public string SellStackText => $"{Localization.T("npc.shop.context.sell_stack")} ({(_item?.SellValue ?? 0) * (_item?.StackSize ?? 1)})";
        public int GridColumn => _index % 7;
        public int GridRow => _index / 7;

        public InventoryItemViewModel(Item item, int index)
        {
            _item = item;
            _index = index;
            RarityBrush = GetRarityBrush(item.Rarity);
        }

        private static Brush GetRarityBrush(string rarity) => rarity switch
        {
            ItemRarity.Common    => new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            ItemRarity.Uncommon  => new SolidColorBrush(Color.FromRgb(30, 255, 0)),
            ItemRarity.Rare      => new SolidColorBrush(Color.FromRgb(0, 112, 221)),
            ItemRarity.Epic      => new SolidColorBrush(Color.FromRgb(163, 53, 238)),
            ItemRarity.Unique    => new SolidColorBrush(Color.FromRgb(170, 100, 100)),
            ItemRarity.Legendary => new SolidColorBrush(Color.FromRgb(255, 128, 0)),
            ItemRarity.Godly     => new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            _                    => new SolidColorBrush(Color.FromRgb(160, 160, 160))
        };
    }

    /// <summary>
    /// ViewModel for the item hover tooltip. Shared by both InventoryGridViewModel and EquipmentViewModel.
    /// </summary>
    public class ItemTooltipViewModel : BaseViewModel
    {
        private string _itemNameKey;
        private string _itemNameSuffix = "";
        private string _itemType;
        private string _itemRarity;
        private Brush _rarityColor;
        private string _itemStats;
        private string _classText = "";
        private Brush _classColor = s_classMuted;
        private bool _hasClassInfo;

        private static readonly Brush s_classCompatible   = MakeBrush(0x6F, 0xCF, 0x97);
        private static readonly Brush s_classIncompatible = MakeBrush(0xCF, 0x66, 0x79);
        private static readonly Brush s_classMuted        = MakeBrush(0x9A, 0x8A, 0x68);

        private static Brush MakeBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        // Lazy-translates the stored key so language switches work without re-calling SetItem.
        // The suffix (e.g. " +3") is appended after translation.
        public string ItemName => Localization.T(_itemNameKey) + _itemNameSuffix;
        public string ItemType { get => _itemType; set => SetProperty(ref _itemType, value); }
        public string ItemRarity { get => _itemRarity; set => SetProperty(ref _itemRarity, value); }
        public Brush RarityColor { get => _rarityColor; set => SetProperty(ref _rarityColor, value); }
        public string ItemStats { get => _itemStats; set => SetProperty(ref _itemStats, value); }
        public string ClassText { get => _classText; private set => SetProperty(ref _classText, value); }
        public Brush ClassColor { get => _classColor; private set => SetProperty(ref _classColor, value); }
        public bool HasClassInfo { get => _hasClassInfo; private set => SetProperty(ref _hasClassInfo, value); }

        public void SetItem(Item item, Myria.Lib.Core.Entities.Characters.Character? character = null)
        {
            _itemNameKey = item.Name;
            _itemNameSuffix = item is EquipmentItem eq && eq.UpgradeLevel >= 1
                ? $" +{eq.UpgradeLevel}"
                : "";
            OnPropertyChanged(nameof(ItemName));
            ItemType = $"{Localization.T("pg.inventory.tooltip.type")}: {item.GetType().Name}";
            ItemRarity = $"{Localization.T("pg.inventory.tooltip.rarity")}: {item.Rarity}";
            RarityColor = GetRarityBrush(item.Rarity);
            ItemStats = BuildStatsString(item);

            if (item is EquipmentItem equip && equip.AllowedClasses.Count > 0)
            {
                ClassText = string.Join(", ", equip.AllowedClasses.Select(c => Localization.T($"class.{c}")));
                ClassColor = character != null
                    ? (equip.IsUsableBy(character) ? s_classCompatible : s_classIncompatible)
                    : s_classMuted;
                HasClassInfo = true;
            }
            else
            {
                HasClassInfo = false;
            }
        }

        private static string BuildStatsString(Item item)
        {
            if (item is EquipmentItem equip)
            {
                var lines = new List<string>();
                if (equip.BonusATK > 0)     lines.Add($"{Localization.T("pg.inventory.stat.atk")}: +{equip.BonusATK}");
                if (equip.BonusDEF > 0)     lines.Add($"{Localization.T("pg.inventory.stat.def")}: +{equip.BonusDEF}");
                if (equip.BonusMATK > 0)    lines.Add($"{Localization.T("pg.inventory.stat.matk")}: +{equip.BonusMATK}");
                if (equip.BonusMDEF > 0)    lines.Add($"{Localization.T("pg.inventory.stat.mdef")}: +{equip.BonusMDEF}");
                if (equip.BonusSTR > 0)     lines.Add($"{Localization.T("pg.inventory.stat.str")}: +{equip.BonusSTR}");
                if (equip.BonusDEX > 0)     lines.Add($"{Localization.T("pg.inventory.stat.dex")}: +{equip.BonusDEX}");
                if (equip.BonusEND > 0)     lines.Add($"{Localization.T("pg.inventory.stat.end")}: +{equip.BonusEND}");
                if (equip.BonusINT > 0)     lines.Add($"{Localization.T("pg.inventory.stat.int")}: +{equip.BonusINT}");
                if (equip.BonusSPR > 0)     lines.Add($"{Localization.T("pg.inventory.stat.spr")}: +{equip.BonusSPR}");
                if (equip.BonusHP > 0)      lines.Add($"{Localization.T("pg.inventory.stat.hp")}: +{equip.BonusHP}");
                if (equip.BonusMP > 0)      lines.Add($"{Localization.T("pg.inventory.stat.mp")}: +{equip.BonusMP}");
                if (equip.BonusAim > 0)     lines.Add($"{Localization.T("pg.inventory.stat.aim")}: +{equip.BonusAim}%");
                if (equip.BonusEvasion > 0) lines.Add($"{Localization.T("pg.inventory.stat.evasion")}: +{equip.BonusEvasion}%");
                if (equip.BonusCrit > 0)    lines.Add($"{Localization.T("pg.inventory.stat.crit")}: +{equip.BonusCrit}%");
                if (equip.BonusBlock > 0)   lines.Add($"{Localization.T("pg.inventory.stat.block")}: +{equip.BonusBlock}%");
                return lines.Count > 0 ? string.Join("\n", lines) : Localization.T("pg.inventory.tooltip.no_bonuses");
            }

            if (item is ConsumableItem consumable)
            {
                var lines = new List<string>();
                if (consumable.HealAmount > 0)  lines.Add($"{Localization.T("pg.inventory.stat.heal")}: {consumable.HealAmount}");
                if (consumable.ManaRestore > 0) lines.Add($"{Localization.T("pg.inventory.stat.mana")}: {consumable.ManaRestore}");
                return lines.Count > 0 ? string.Join("\n", lines) : Localization.T("pg.inventory.tooltip.no_effects");
            }

            return string.Empty;
        }

        private static Brush GetRarityBrush(string rarity) => rarity switch
        {
            // Was "case 0:" — a hardcoded match on ItemRarity.Common's old enum ordinal, which
            // only worked because Common happened to be declared first. Now that ItemRarity is a
            // string, that shortcut is gone — name it explicitly like the other cases.
            Myria.Lib.Core.Systems.Enums.ItemRarity.Common    => new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Uncommon  => new SolidColorBrush(Color.FromRgb(30, 255, 0)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Rare => new SolidColorBrush(Color.FromRgb(0, 112, 221)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Epic      => new SolidColorBrush(Color.FromRgb(163, 53, 238)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Unique    => new SolidColorBrush(Color.FromRgb(170, 100, 100)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Legendary => new SolidColorBrush(Color.FromRgb(255, 128, 0)),
            Myria.Lib.Core.Systems.Enums.ItemRarity.Godly     => new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            _                    => new SolidColorBrush(Color.FromRgb(160, 160, 160))
        };
    }
}
