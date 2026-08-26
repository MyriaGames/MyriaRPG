using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    // A single item staged for deposit - picked from the inventory grid, not yet actually sent to
    // the server. Nothing about the player's inventory changes until DepositSelectedCommand runs;
    // removing a staged entry before then is a pure UI undo.
    public class DepositStagingItemVm
    {
        public InventoryItemViewModel InventoryItem { get; }
        public int Quantity { get; set; }
        public string ItemId => InventoryItem.Item.Id;
        public string DisplayName => Myria.Lib.Core.Systems.Localization.T(InventoryItem.Item.Name);

        public DepositStagingItemVm(InventoryItemViewModel item, int quantity)
        {
            InventoryItem = item;
            Quantity = quantity;
        }
    }

    // Owner view manages the shop's own storage (deposit/withdraw/price); buyer view only ever
    // sees priced items (see GameHub.BrowseShop). Both share one class since the two modes have
    // always looked and behaved similarly enough not to warrant splitting - same as before.
    public class CharacterShopViewModel : BaseViewModel
    {
        public bool IsOwner { get; }
        public string OwnerName { get; }
        public string Title => IsOwner ? "My Shop" : $"{OwnerName}'s Shop";

        // Owner: full storage (priced + unpriced, incl. the Merchant's Seal).
        // Buyer: priced items only.
        public ObservableCollection<ShopListingVm> Listings { get; } = new();

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private bool _shopOpen;
        public bool ShopOpen
        {
            get => _shopOpen;
            set { _shopOpen = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToggleShopLabel)); }
        }

        public string ToggleShopLabel => ShopOpen ? "Close Shop" : "Open Shop";

        // ── Owner: deposit picker (inventory grid -> staged items -> Deposit Selected) ─────────
        // Only constructed for the owner view - the buyer never needs their own inventory shown.
        public InventoryGridViewModel? InventoryVm { get; }
        public ObservableCollection<DepositStagingItemVm> StagedItems { get; } = new();
        public bool HasStagedItems => StagedItems.Count > 0;

        // Set while a stack (>1) is picked and waiting for the player to confirm how many -
        // null the rest of the time, which the view uses to show/hide the quantity stepper.
        private InventoryItemViewModel? _pendingItem;
        public InventoryItemViewModel? PendingItem
        {
            get => _pendingItem;
            set { _pendingItem = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPendingQuantityVisible)); }
        }
        public bool IsPendingQuantityVisible => PendingItem is not null;

        private int _pendingQuantity = 1;
        public int PendingQuantity
        {
            get => _pendingQuantity;
            set
            {
                int max = PendingItem?.Item?.StackSize ?? 1;
                _pendingQuantity = Math.Clamp(value, 1, Math.Max(1, max));
                OnPropertyChanged();
            }
        }

        // Owner: price/withdraw inputs for the selected storage row (Listings selection)
        private string _price = "";
        public string Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private string _withdrawQty = "1";
        public string WithdrawQty
        {
            get => _withdrawQty;
            set { _withdrawQty = value; OnPropertyChanged(); }
        }

        // Buyer qty input
        private string _buyQty = "1";
        public string BuyQty
        {
            get => _buyQty;
            set { _buyQty = value; OnPropertyChanged(); }
        }

        private ShopListingVm? _selectedListing;
        public ShopListingVm? SelectedListing
        {
            get => _selectedListing;
            set { _selectedListing = value; OnPropertyChanged(); }
        }

        public ICommand ToggleShopCommand         { get; }
        public ICommand SelectInventoryItemCommand { get; }
        public ICommand IncreasePendingQtyCommand  { get; }
        public ICommand DecreasePendingQtyCommand  { get; }
        public ICommand MaxPendingQtyCommand       { get; }
        public ICommand ConfirmPendingQtyCommand   { get; }
        public ICommand CancelPendingQtyCommand    { get; }
        public ICommand RemoveStagedItemCommand    { get; }
        public ICommand DepositSelectedCommand     { get; }
        public ICommand WithdrawCommand   { get; }
        public ICommand SetPriceCommand   { get; }
        public ICommand UnlistCommand     { get; }
        public ICommand BuyCommand        { get; }
        public ICommand CloseCommand      { get; }

        private CharacterShopViewModel(string ownerName, bool isOwner)
        {
            OwnerName = ownerName;
            IsOwner   = isOwner;

            if (IsOwner)
            {
                InventoryVm = new MultiplayerInventoryGridViewModel(UserAccountService.CurrentCharacter);
                // The plain OnPropertyChanged(nameof(StagedItems)) below looks redundant (the
                // collection instance never changes) but it's what makes the deposit tiles'
                // IsEnabled MultiBinding (bound to this same property, see
                // Page_PlayerShop.xaml's IsItemStagedConverter usage) actually re-evaluate on
                // Add/Remove - WPF bindings don't auto-refresh just because a bound collection's
                // *contents* changed, only when the property itself is reported changed.
                StagedItems.CollectionChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(HasStagedItems));
                    OnPropertyChanged(nameof(StagedItems));
                };
            }

            ToggleShopCommand          = new RelayCommand(ToggleShop);
            SelectInventoryItemCommand = new RelayCommand<InventoryItemViewModel>(SelectInventoryItem);
            IncreasePendingQtyCommand  = new RelayCommand(() => PendingQuantity++);
            DecreasePendingQtyCommand  = new RelayCommand(() => PendingQuantity--);
            MaxPendingQtyCommand       = new RelayCommand(() => PendingQuantity = PendingItem?.Item?.StackSize ?? 1);
            ConfirmPendingQtyCommand   = new RelayCommand(ConfirmPendingQuantity);
            CancelPendingQtyCommand    = new RelayCommand(() => PendingItem = null);
            RemoveStagedItemCommand    = new RelayCommand<DepositStagingItemVm>(item => { if (item is not null) StagedItems.Remove(item); });
            DepositSelectedCommand     = new RelayCommand(async () => await DepositSelectedAsync());
            WithdrawCommand   = new RelayCommand(Withdraw);
            SetPriceCommand   = new RelayCommand(SetPrice);
            UnlistCommand     = new RelayCommand(Unlist);
            BuyCommand        = new RelayCommand(Buy);
            CloseCommand      = new RelayCommand(Close);

            GameHubService.MyShopUpdated     += OnMyShopUpdated;
            GameHubService.ShopSale          += OnShopSale;
            GameHubService.ShopBuyResult     += OnBuyResult;
            GameHubService.ShopErrorReceived += OnShopError;
        }

        // NOTE: GetMyShop returns an empty list both when there's no shop yet and when the shop
        // exists but is completely empty (no items deposited at all, not even the Seal) - in
        // that second, unusual case this will show "Open Shop" even though one technically
        // already exists. Accepted gap: an open-but-fully-empty shop is a rare transient state
        // (right after OpenShop, before depositing anything) and MyShopUpdated corrects ShopOpen
        // to true the moment anything is deposited.
        public static async Task<CharacterShopViewModel> OpenOwnerView()
        {
            var vm = new CharacterShopViewModel("", isOwner: true);
            var items = await GameHubService.GetMyShopAsync();
            vm.ShopOpen = items.Count > 0;
            foreach (var i in items) vm.Listings.Add(i);
            return vm;
        }

        public static async Task<CharacterShopViewModel> OpenBuyerView(string ownerName)
        {
            var vm = new CharacterShopViewModel(ownerName, isOwner: false);
            var listings = await GameHubService.BrowseShopAsync(ownerName);
            foreach (var l in listings)
                vm.Listings.Add(l);
            if (listings.Count == 0)
                vm.Status = "This player's shop is currently empty.";
            return vm;
        }

        public void Unsubscribe()
        {
            GameHubService.MyShopUpdated     -= OnMyShopUpdated;
            GameHubService.ShopSale          -= OnShopSale;
            GameHubService.ShopBuyResult     -= OnBuyResult;
            GameHubService.ShopErrorReceived -= OnShopError;
        }

        private void OnMyShopUpdated(List<ShopListingVm> items)
        {
            if (!IsOwner) return;
            Listings.Clear();
            foreach (var i in items) Listings.Add(i);
            ShopOpen = true;
        }

        private void OnShopSale(string buyerName, string itemId, int qty, long paid, long fee)
        {
            if (!IsOwner) return;
            Status = fee > 0
                ? $"{buyerName} bought {qty}x {itemId} for {paid} Bronze (fee: {fee})."
                : $"{buyerName} bought {qty}x {itemId} for {paid} Bronze.";
        }

        private void OnShopError(string error)
        {
            Status = error switch
            {
                "already_open"     => "You already have a shop open.",
                "no_shop"          => "You don't have a shop open.",
                "not_owned"        => "You don't have that many of that item.",
                "not_in_storage"   => "That item isn't in your shop's storage.",
                "invalid_qty"      => "Enter a valid quantity.",
                "invalid_price"    => "Enter a valid price.",
                "seal_not_listable"=> "The Merchant's Seal can't be sold - just keep it in storage.",
                "storage_full"     => "Your shop's storage is full - withdraw something first.",
                "not_a_city_room"  => "You can only open a shop in a city.",
                _                  => "That didn't work."
            };
        }

        private void OnBuyResult(bool ok, string error, string itemId, int qty, long paid)
        {
            if (ok)
            {
                Status = $"Purchased {qty}x {itemId} for {paid} Bronze.";
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var fresh = await GameHubService.BrowseShopAsync(OwnerName);
                    Listings.Clear();
                    foreach (var l in fresh) Listings.Add(l);
                });
            }
            else
            {
                Status = error switch
                {
                    "insufficient_gold"  => "You don't have enough gold.",
                    "insufficient_stock" => "Not enough stock for that quantity.",
                    "shop_closed"        => "That shop is no longer open.",
                    "no_listing"         => "That item is no longer available.",
                    "item_unavailable"   => "That item can't be created right now.",
                    _                    => "Purchase failed."
                };
            }
        }

        private void ToggleShop()
        {
            if (ShopOpen)
                _ = GameHubService.CloseShopAsync();
            else
                _ = GameHubService.OpenShopAsync();
            ShopOpen = !ShopOpen;
        }

        // Non-stacked items (StackSize == 1, incl. equipment) stage immediately - nothing to ask.
        // A stack opens the inline quantity stepper instead of guessing how many the player wants.
        // A tile that's already staged is ignored on further clicks - otherwise the same physical
        // stack could be staged repeatedly, adding up to more than the player actually owns
        // (StagedItems tracks the exact InventoryItemViewModel reference, which stays stable
        // until the next RefreshInventory() - i.e. until an actual deposit goes through).
        private void SelectInventoryItem(InventoryItemViewModel? item)
        {
            if (item?.Item is null) return;
            if (StagedItems.Any(s => ReferenceEquals(s.InventoryItem, item))) return;

            if (item.Item.StackSize > 1)
            {
                PendingItem = item;
                PendingQuantity = item.Item.StackSize; // default to the whole stack, adjustable
            }
            else
            {
                StageItem(item, 1);
            }
        }

        private void ConfirmPendingQuantity()
        {
            if (PendingItem is null) return;
            StageItem(PendingItem, PendingQuantity);
            PendingItem = null;
        }

        // Deliberately doesn't merge with an existing staged entry for the same item id - each
        // inventory tile the player clicks becomes its own staged line, simplest to reason about
        // and to remove individually before committing.
        private void StageItem(InventoryItemViewModel item, int quantity) =>
            StagedItems.Add(new DepositStagingItemVm(item, quantity));

        private async Task DepositSelectedAsync()
        {
            if (StagedItems.Count == 0) return;

            var toProcess = StagedItems.ToList();
            int deposited = 0, failed = 0;

            foreach (var staged in toProcess)
            {
                bool ok = await GameHubService.DepositShopItemAsync(staged.ItemId, staged.Quantity);
                if (!ok)
                {
                    failed++;
                    continue;
                }

                deposited++;
                StagedItems.Remove(staged);

                // No local inventory mirroring needed here - a successful deposit makes the
                // server also push a generic CharacterUpdated(inventory) patch, which
                // Inventory.ApplySnapshot reconciles and InventoryGridViewModel picks up via its
                // existing ItemReceived/ItemRemoved subscriptions.
            }

            if (failed > 0)
                Status = deposited > 0
                    ? $"Deposited {deposited} item(s); {failed} failed (see above)."
                    : "Nothing could be deposited.";
        }

        private async void Withdraw()
        {
            if (SelectedListing is null) return;
            if (!int.TryParse(WithdrawQty, out int qty) || qty <= 0) return;

            string itemId = SelectedListing.ItemId;
            // No local inventory mirroring needed - a successful withdrawal makes the server
            // also push a generic CharacterUpdated(inventory) patch (see DepositSelectedAsync).
            await GameHubService.WithdrawShopItemAsync(itemId, qty);
        }

        private void SetPrice()
        {
            if (SelectedListing is null) return;
            if (!long.TryParse(Price, out long price) || price <= 0) return;
            _ = GameHubService.SetShopItemPriceAsync(SelectedListing.ItemId, price);
            Price = "";
        }

        private void Unlist()
        {
            if (SelectedListing is null) return;
            _ = GameHubService.UnlistShopItemAsync(SelectedListing.ItemId);
        }

        private void Buy()
        {
            if (SelectedListing is null) return;
            if (!int.TryParse(BuyQty, out int qty) || qty <= 0) return;
            _ = GameHubService.BuyFromShopAsync(OwnerName, SelectedListing.ItemId, qty);
        }

        private void Close()
        {
            Unsubscribe();
            Application.Current.Dispatcher.Invoke(() =>
                MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden);
        }
    }
}
