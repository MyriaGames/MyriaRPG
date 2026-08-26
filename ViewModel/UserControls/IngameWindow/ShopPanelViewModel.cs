using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Events;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public enum ShopFilter { Weapons, Armor, Accessories, Utilities, Buyback }

    public class ShopPanelViewModel : BaseViewModel
    {
        protected readonly Npc _npc;
        protected readonly Character _character;
        private readonly Action _goBack;
        private ShopFilter _activeFilter = ShopFilter.Weapons;
        private bool IsGeneralTrader => _npc.Services.Contains("shop_general") && !_npc.Services.Contains("shop_equipment");

        public ShopFilter ActiveFilter
        {
            get => _activeFilter;
            set
            {
                _activeFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredStock));
                OnPropertyChanged(nameof(BtnBuy));
                OnPropertyChanged(nameof(IsQuantityControlVisible));
                OnPropertyChanged(nameof(CanBuy));
                OnPropertyChanged(nameof(IsFilter_Weapons));
                OnPropertyChanged(nameof(IsFilter_Armor));
                OnPropertyChanged(nameof(IsFilter_Accessories));
                OnPropertyChanged(nameof(IsFilter_Utilities));
                OnPropertyChanged(nameof(IsFilter_Buyback));
                SelectedStock = FilteredStock.FirstOrDefault();
            }
        }

        public bool IsFilter_Weapons     => _activeFilter == ShopFilter.Weapons;
        public bool IsFilter_Armor       => _activeFilter == ShopFilter.Armor;
        public bool IsFilter_Accessories => _activeFilter == ShopFilter.Accessories;
        public bool IsFilter_Utilities   => _activeFilter == ShopFilter.Utilities;
        public bool IsFilter_Buyback     => _activeFilter == ShopFilter.Buyback;

        public IEnumerable<ShopItemVm> FilteredStock => _activeFilter switch
        {
            ShopFilter.Weapons when IsGeneralTrader => Stock,
            ShopFilter.Armor when IsGeneralTrader => Stock.Where(i => i.ItemKind == ShopItemKind.Consumable),
            ShopFilter.Accessories when IsGeneralTrader => Stock.Where(i => i.ItemKind == ShopItemKind.Material),
            ShopFilter.Utilities when IsGeneralTrader => Stock.Where(i => i.ItemKind == ShopItemKind.Tool
                                                                       || i.ItemKind == ShopItemKind.Other
                                                                       || i.ItemKind == ShopItemKind.Equipment),
            ShopFilter.Weapons     => Stock.OfType<ShopEquipmentItemVm>()
                                          .Where(i => !i.IsTool
                                                   && i.SlotType == EquipmentType.Weapon
                                                   && IsAllowedForCharacter(i)),
            ShopFilter.Armor       => Stock.OfType<ShopEquipmentItemVm>()
                                          .Where(i => !i.IsTool
                                                   && i.SlotType == EquipmentType.Armor
                                                   && IsAllowedForCharacter(i)),
            ShopFilter.Accessories => Stock.OfType<ShopEquipmentItemVm>()
                                          .Where(i => !i.IsTool
                                                   && i.SlotType == EquipmentType.Accessory
                                                   && IsAllowedForCharacter(i)),
            ShopFilter.Utilities   => Stock.Where(i => i is not ShopEquipmentItemVm
                                                    || (i is ShopEquipmentItemVm se && se.IsTool)),
            ShopFilter.Buyback     => BuybackStock.Cast<ShopItemVm>(),
            _                      => Stock
        };

        // Buyback list – filled when player sells via inventory context menu
        public ObservableCollection<BuybackItemVm> BuybackStock { get; } = new();

        public string Title    => IsGeneralTrader
                                    ? Localization.T("npc.shop.title.general")
                                    : Localization.T("npc.shop.title.equipment");
        public string BtnBack  => Localization.T("app.general.UI.back");
        public string BtnBuy   => _activeFilter == ShopFilter.Buyback
                                    ? Localization.T("npc.shop.rebuy")
                                    : Localization.T("npc.shop.buy");
        public string BtnSell  => Localization.T("npc.shop.sell");
        public string StockLabel         => Localization.T("npc.shop.stock");
        public string CharacterInventoryLabel => Localization.T("npc.shop.inventory");

        // Filter button labels (localized)
        public string LblWeapons     => IsGeneralTrader ? Localization.T("npc.shop.filter.all") : Localization.T("npc.shop.filter.weapons");
        public string LblArmor       => IsGeneralTrader ? Localization.T("npc.shop.filter.consumables") : Localization.T("npc.shop.filter.armor");
        public string LblAccessories => IsGeneralTrader ? Localization.T("npc.shop.filter.materials") : Localization.T("npc.shop.filter.accessories");
        public string LblUtilities   => IsGeneralTrader ? Localization.T("npc.shop.filter.tools") : Localization.T("npc.shop.filter.utilities");
        public string LblBuyback     => Localization.T("npc.shop.filter.buyback");

        public bool IsQuantityControlVisible => _activeFilter != ShopFilter.Buyback;

        private int _characterMoney;
        public int CharacterMoney
        {
            get => _characterMoney;
            set { _characterMoney = value; OnPropertyChanged(); OnPropertyChanged(nameof(CharacterMoneyText)); }
        }

        public string CharacterMoneyText => $"💰 {CharacterMoney}";

        // Shop Stock
        public ObservableCollection<ShopItemVm> Stock { get; } = new();

        private ShopItemVm _selectedStock;
        public ShopItemVm SelectedStock
        {
            get => _selectedStock;
            set
            {
                _selectedStock = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedStockName));
                OnPropertyChanged(nameof(SelectedStockPrice));
                OnPropertyChanged(nameof(BuyQuantityMax));
                OnPropertyChanged(nameof(BuyTotalPrice));
                OnPropertyChanged(nameof(CanBuy));
            }
        }

        public string SelectedStockName => SelectedStock?.Name ?? "";

        public string SelectedStockPrice
        {
            get
            {
                if (SelectedStock is BuybackItemVm bb)
                    return $"{StockPriceLabel}: {bb.TotalRebuyPrice}";
                if (SelectedStock != null)
                    return $"{StockPriceLabel}: {SelectedStock.BuyPrice}";
                return "";
            }
        }

        public string StockPriceLabel  => Localization.T("npc.shop.price");
        public string TotalLabel       => Localization.T("npc.shop.total");
        public string SellPriceLabel   => Localization.T("npc.shop.sellPrice");
        public string QuantityLabel    => Localization.T("npc.shop.quantity");
        public string MaxLabel         => Localization.T("app.general.UI.max");

        public int BuyQuantityMax => _selectedStock is BuybackItemVm ? 1
            : _selectedStock == null ? 0
            : _selectedStock.BuyPrice <= 0 ? _selectedStock.MaxStackSize
            : (int)(_character.Money.Balance.BronzeTotal / _selectedStock.BuyPrice);

        // Quantity Control
        private int _buyQuantity = 1;
        public int BuyQuantity
        {
            get => _buyQuantity;
            set
            {
                if (value < 1) value = 1;
                if (value > BuyQuantityMax && BuyQuantityMax > 0) value = BuyQuantityMax;
                _buyQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanBuy));
                OnPropertyChanged(nameof(BuyTotalPrice));
            }
        }

        public int BuyTotalPrice => _selectedStock is BuybackItemVm bb
            ? bb.TotalRebuyPrice
            : _selectedStock != null ? _selectedStock.BuyPrice * _buyQuantity : 0;

        // Status
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanBuy
        {
            get
            {
                if (_selectedStock == null) return false;
                if (_selectedStock is BuybackItemVm bb)
                    return _character.Money.Balance.BronzeTotal >= bb.TotalRebuyPrice;
                return _buyQuantity > 0 && _character.Money.Balance.BronzeTotal >= BuyTotalPrice;
            }
        }

        // Shop item tooltip
        private ItemTooltipViewModel _currentShopTooltip;
        private bool _isShopTooltipVisible;

        public ItemTooltipViewModel CurrentShopTooltip
        {
            get => _currentShopTooltip;
            set => SetProperty(ref _currentShopTooltip, value);
        }

        public bool IsShopTooltipVisible
        {
            get => _isShopTooltipVisible;
            set => SetProperty(ref _isShopTooltipVisible, value);
        }

        // Commands
        public ICommand FilterWeaponsCommand     { get; }
        public ICommand FilterArmorCommand       { get; }
        public ICommand FilterAccessoriesCommand { get; }
        public ICommand FilterUtilitiesCommand   { get; }
        public ICommand FilterBuybackCommand     { get; }
        public ICommand BackCommand              { get; }
        public ICommand BuyCommand               { get; }
        public ICommand IncreaseBuyQtyCommand    { get; }
        public ICommand DecreaseBuyQtyCommand    { get; }
        public ICommand MaxBuyQtyCommand         { get; }
        public ICommand ShowShopTooltipCommand   { get; }
        public ICommand HideShopTooltipCommand   { get; }

        public ShopPanelViewModel(Npc npc, Character character, Action goBack)
        {
            _npc    = npc;
            _character = character;
            _goBack = goBack;

            CharacterMoney = (int)_character.Money.Balance.BronzeTotal;

            _currentShopTooltip = new ItemTooltipViewModel();

            BackCommand           = new RelayCommand(_goBack);
            BuyCommand            = new RelayCommand(BuySelected);
            IncreaseBuyQtyCommand = new RelayCommand(() => BuyQuantity++);
            DecreaseBuyQtyCommand = new RelayCommand(() => BuyQuantity--);
            MaxBuyQtyCommand      = new RelayCommand(() => BuyQuantity = BuyQuantityMax);
            FilterWeaponsCommand     = new RelayCommand(() => ActiveFilter = ShopFilter.Weapons);
            FilterArmorCommand       = new RelayCommand(() => ActiveFilter = ShopFilter.Armor);
            FilterAccessoriesCommand = new RelayCommand(() => ActiveFilter = ShopFilter.Accessories);
            FilterUtilitiesCommand   = new RelayCommand(() => ActiveFilter = ShopFilter.Utilities);
            FilterBuybackCommand     = new RelayCommand(() => ActiveFilter = ShopFilter.Buyback);
            ShowShopTooltipCommand   = new RelayCommand<ShopItemVm>(ShowShopTooltip);
            HideShopTooltipCommand   = new RelayCommand(HideShopTooltip);

            _character.Inventory.ItemSold += OnItemSold;

            LoadShopStock();
        }

        private void OnItemSold(object? sender, ItemReceivedEventArgs e)
        {
            CharacterMoney = (int)_character.Money.Balance.BronzeTotal;

            // Add to buyback list so player can repurchase accidentally sold items
            BuybackStock.Add(new BuybackItemVm
            {
                Id       = e.Item.Id,
                Name     = Localization.T($"item.{e.Item.Id}"),
                BuyPrice = e.Item.BuyPrice,
                Quantity = e.Amount
            });

            if (_activeFilter == ShopFilter.Buyback)
                OnPropertyChanged(nameof(FilteredStock));
        }

        protected virtual void LoadShopStock()
        {
            Stock.Clear();

            foreach (var itemId in NpcService.GetEffectiveShopItemNames(_npc, _character.CurrentRoom))
            {
                if (!ItemFactory.TryCreateItem(itemId, out var item))
                    continue;

                if (item is EquipmentItem eq)
                {
                    // Tools always appear regardless of player class
                    if (eq.IsTool || IsAllowedForCharacter(eq))
                        Stock.Add(ShopEquipmentItemVm.FromEquipment(eq));
                }
                else
                {
                    if (item.AllowedClasses.Count == 0 || item.AllowedClasses.Contains(_character.Class))
                        Stock.Add(ShopItemVm.FromItem(item));
                }
            }

            SelectedStock = FilteredStock.FirstOrDefault();
            if (SelectedStock == null)
                StatusMessage = Localization.T("npc.shop.noStock");
        }

        private bool IsAllowedForCharacter(ShopEquipmentItemVm item)
            => item.AllowedClasses.Count == 0 || item.AllowedClasses.Contains(_character.Class);

        private bool IsAllowedForCharacter(EquipmentItem item)
            => item.AllowedClasses.Count == 0 || item.AllowedClasses.Contains(_character.Class);

        private void BuySelected()
        {
            if (SelectedStock is BuybackItemVm buyback)
            {
                RebuySelected(buyback);
                return;
            }
            if (SelectedStock == null || BuyQuantity <= 0) return;
            _ = ExecuteBuy(SelectedStock, BuyQuantity);
        }

        protected virtual Task ExecuteBuy(ShopItemVm item, int quantity)
        {
            int totalCost = item.BuyPrice * quantity;

            if (_character.Money.Balance.BronzeTotal < totalCost)
            {
                StatusMessage = Localization.T("npc.shop.notEnoughMoney");
                return Task.CompletedTask;
            }

            if (ItemFactory.TryCreateItem(item.Id, out var purchased))
            {
                purchased.StackSize = quantity;

                if (_character.Inventory.AddItem(purchased, _character))
                {
                    _character.Money.TrySpend(totalCost);
                    CharacterMoney = (int)_character.Money.Balance.BronzeTotal;
                    StatusMessage = Localization.T("npc.shop.buySuccess", quantity, item.Name);
                    BuyQuantity = 1;
                    OnPropertyChanged(nameof(CanBuy));
                }
                else
                {
                    StatusMessage = Localization.T("npc.shop.inventoryFull");
                }
            }
            else
            {
                StatusMessage = Localization.T("npc.shop.buyFailed");
            }
            return Task.CompletedTask;
        }

        private void ShowShopTooltip(ShopItemVm shopItem)
        {
            if (shopItem == null) return;
            if (ItemFactory.TryCreateItem(shopItem.Id, out var item))
            {
                CurrentShopTooltip.SetItem(item, _character);
                IsShopTooltipVisible = true;
            }
        }

        private void HideShopTooltip() => IsShopTooltipVisible = false;

        private void RebuySelected(BuybackItemVm buyback)
        {
            int totalCost = buyback.TotalRebuyPrice;

            if (_character.Money.Balance.BronzeTotal < totalCost)
            {
                StatusMessage = Localization.T("npc.shop.notEnoughMoney");
                return;
            }

            if (ItemFactory.TryCreateItem(buyback.Id, out var item))
            {
                item.StackSize = buyback.Quantity;

                if (_character.Inventory.AddItem(item, _character))
                {
                    _character.Money.TrySpend(totalCost);
                    CharacterMoney = (int)_character.Money.Balance.BronzeTotal;
                    BuybackStock.Remove(buyback);
                    StatusMessage = Localization.T("npc.shop.buySuccess", buyback.Quantity, buyback.Name);
                    OnPropertyChanged(nameof(FilteredStock));
                    SelectedStock = FilteredStock.FirstOrDefault();
                }
                else
                {
                    StatusMessage = Localization.T("npc.shop.inventoryFull");
                }
            }
        }
    }

    public class ShopItemVm : BaseViewModel
    {
        public string Id       { get; set; }
        public string Name     { get; set; }
        public int    BuyPrice { get; set; }
        public int    MaxStackSize { get; set; } = 1;
        public ShopItemKind ItemKind { get; set; } = ShopItemKind.Other;

        public static ShopItemVm FromItem(Item item)
        {
            return new ShopItemVm
            {
                Id       = item.Id,
                Name     = Localization.T($"item.{item.Id}"),
                BuyPrice = item.BuyPrice,
                MaxStackSize = item.MaxStackSize,
                ItemKind = item switch
                {
                    ConsumableItem => ShopItemKind.Consumable,
                    MaterialItem => ShopItemKind.Material,
                    _ => ShopItemKind.Other
                }
            };
        }

        public override string ToString() => Name;
    }

    public class BuybackItemVm : ShopItemVm
    {
        public int Quantity { get; set; }
        public int TotalRebuyPrice => BuyPrice * Quantity;
    }

    public enum ShopItemKind
    {
        Equipment,
        Tool,
        Consumable,
        Material,
        Other
    }
}
