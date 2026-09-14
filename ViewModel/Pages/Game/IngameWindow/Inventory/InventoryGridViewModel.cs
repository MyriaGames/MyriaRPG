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
        private string _classText = "";
        private Brush _classColor = s_classMuted;
        private bool _hasClassInfo;

        private static readonly Brush s_classCompatible   = MakeBrush(0x6F, 0xCF, 0x97);
        private static readonly Brush s_classIncompatible = MakeBrush(0xCF, 0x66, 0x79);
        private static readonly Brush s_classMuted        = MakeBrush(0x9A, 0x8A, 0x68);

        // Reused for stat comparison deltas/loss lines - same colors as the class-compatibility text
        // above (green = better/gained, red = worse/lost), so the tooltip only has one "good/bad"
        // color language rather than two.
        private static Brush StatGainBrush => s_classCompatible;
        private static Brush StatLossBrush => s_classIncompatible;

        // Resolved fresh per SetItem call (not cached) so it tracks the current Light/Dark theme,
        // same as every other color in this class - matches how RarityColor/ClassColor already work.
        private static Brush DefaultStatBrush =>
            System.Windows.Application.Current?.TryFindResource("Brush.Foreground") as Brush ?? Brushes.White;

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

        /// <summary>One line per stat, optionally colored - see PopulateStatLines. Replaces the old
        /// single plain string so mixed green/red/default lines can render in one list.</summary>
        public ObservableCollection<StatLineViewModel> ItemStats { get; } = new();
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
            PopulateStatLines(item, character);

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

        /// <summary>One (stat, hovered-value, isPercent) descriptor per equipment bonus, in the same
        /// order the tooltip has always shown them.</summary>
        private static readonly (string LabelKey, Func<EquipmentItem, float> Value, bool IsPercent)[] s_equipStats =
        {
            ("atk",      i => i.BonusATK,      false),
            ("def",      i => i.BonusDEF,      false),
            ("matk",     i => i.BonusMATK,     false),
            ("mdef",     i => i.BonusMDEF,     false),
            ("str",      i => i.BonusSTR,      false),
            ("dex",      i => i.BonusDEX,      false),
            ("end",      i => i.BonusEND,      false),
            ("int",      i => i.BonusINT,      false),
            ("spr",      i => i.BonusSPR,      false),
            ("hp",       i => i.BonusHP,       false),
            ("mp",       i => i.BonusMP,       false),
            ("aim",      i => i.BonusAim,      true),
            ("evasion",  i => i.BonusEvasion,  true),
            ("crit",     i => i.BonusCrit,     true),
            ("block",    i => i.BonusBlock,    true),
        };

        /// <summary>
        /// Rebuilds ItemStats for the hovered item. For equipment, compares against whatever is
        /// currently equipped in the *same slot* (skipped entirely if there's nothing equipped
        /// there, or the hovered item IS that equipped item - reference-equal, e.g. hovering your
        /// own worn gear on the Equipment page): a stat both items have gets a green/red (+N)/(-N)
        /// suffix when they differ, a stat only the equipped item has gets a full red loss line
        /// ("+N STAT"), everything else is a plain line exactly like before this comparison existed.
        /// </summary>
        private void PopulateStatLines(Item item, Myria.Lib.Core.Entities.Characters.Character? character)
        {
            ItemStats.Clear();

            if (item is EquipmentItem equip)
            {
                var equipped = character?.Equipped.GetValueOrDefault(equip.SlotType);
                bool skipComparison = equipped == null || ReferenceEquals(equipped, equip);

                foreach (var (labelKey, getValue, isPercent) in s_equipStats)
                {
                    float hoveredVal = getValue(equip);
                    float equippedVal = skipComparison ? 0f : getValue(equipped!);
                    if (hoveredVal <= 0 && equippedVal <= 0) continue;

                    string label = Localization.T($"pg.inventory.stat.{labelKey}");
                    string suffix = isPercent ? "%" : "";

                    if (hoveredVal > 0)
                    {
                        string baseLine = $"{label}: +{FormatStatNumber(hoveredVal)}{suffix}";
                        // Epsilon, not exact equality - Crit/Block are floats scaled by upgrade/craft
                        // multipliers, so two stats that display identically can differ by a fraction
                        // of a rounding error; a literal "(+0)" on a tied stat is exactly the noise
                        // the no-suffix-when-equal rule exists to avoid.
                        if (!skipComparison && equippedVal > 0 && MathF.Abs(hoveredVal - equippedVal) >= 0.01f)
                        {
                            float delta = hoveredVal - equippedVal;
                            var brush = delta > 0 ? StatGainBrush : StatLossBrush;
                            string sign = delta > 0 ? "+" : "";
                            ItemStats.Add(new StatLineViewModel($"{baseLine} ({sign}{FormatStatNumber(delta)}{suffix})", brush));
                        }
                        else
                        {
                            ItemStats.Add(new StatLineViewModel(baseLine, DefaultStatBrush));
                        }
                    }
                    else // equippedVal > 0, hoveredVal <= 0 - a stat you'd give up by switching
                    {
                        ItemStats.Add(new StatLineViewModel($"+{FormatStatNumber(equippedVal)}{suffix} {label}", StatLossBrush));
                    }
                }

                if (ItemStats.Count == 0)
                    ItemStats.Add(new StatLineViewModel(Localization.T("pg.inventory.tooltip.no_bonuses"), DefaultStatBrush));
                return;
            }

            if (item is ConsumableItem consumable)
            {
                if (consumable.HealAmount > 0)
                    ItemStats.Add(new StatLineViewModel($"{Localization.T("pg.inventory.stat.heal")}: {consumable.HealAmount}", DefaultStatBrush));
                if (consumable.ManaRestore > 0)
                    ItemStats.Add(new StatLineViewModel($"{Localization.T("pg.inventory.stat.mana")}: {consumable.ManaRestore}", DefaultStatBrush));
                if (ItemStats.Count == 0)
                    ItemStats.Add(new StatLineViewModel(Localization.T("pg.inventory.tooltip.no_effects"), DefaultStatBrush));
            }
        }

        private static string FormatStatNumber(float value) =>
            value == MathF.Truncate(value) ? value.ToString("0") : value.ToString("0.##");

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

    /// <summary>One line of ItemTooltipViewModel.ItemStats - text plus its own color, so a stat
    /// comparison can mix plain/green/red lines in a single list.</summary>
    public class StatLineViewModel
    {
        public string Text { get; }
        public Brush Foreground { get; }

        public StatLineViewModel(string text, Brush foreground)
        {
            Text = text;
            Foreground = foreground;
        }
    }
}
