using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Loc = Myria.Lib.Core.Systems.Localization;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class TradeItemVm : BaseViewModel
    {
        public string ItemId   { get; init; } = "";
        public string Name     { get; init; } = "";
        public int    Quantity { get; init; }
    }

    public class TradeViewModel : BaseViewModel
    {
        public string PartnerName       { get; private set; } = "";
        public string Title             => Loc.T("pg.trade.title");
        public string HeaderText        => Loc.T("pg.trade.header", PartnerName);
        public string MyOfferLabel      => Loc.T("pg.trade.my_offer");
        public string PartnerOfferLabel => Loc.T("pg.trade.partner_offer", PartnerName);
        public string MyGoldText        => Loc.T("pg.trade.gold_bronze", MyGold);
        public string TheirGoldText     => Loc.T("pg.trade.gold_bronze", TheirGold);
        public string ImReadyText       => Loc.T("pg.trade.ready.self");
        public string PartnerReadyText  => Loc.T("pg.trade.ready.partner");
        public string PartnerNotReadyText => Loc.T("pg.trade.ready.partner_not");
        public string GoldOfferLabel    => Loc.T("pg.trade.gold_offer");
        public string SetLabel          => Loc.T("pg.trade.set");
        public string CancelTradeLabel  => Loc.T("pg.trade.cancel");

        public ObservableCollection<TradeItemVm> MyItems    { get; } = new();
        public ObservableCollection<TradeItemVm> TheirItems { get; } = new();

        private long _myGold;
        public long MyGold
        {
            get => _myGold;
            set { _myGold = value; OnPropertyChanged(); OnPropertyChanged(nameof(MyGoldText)); }
        }

        private long _theirGold;
        public long TheirGold
        {
            get => _theirGold;
            set { _theirGold = value; OnPropertyChanged(); OnPropertyChanged(nameof(TheirGoldText)); }
        }

        private bool _imReady;
        public bool ImReady
        {
            get => _imReady;
            set
            {
                _imReady = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConfirmLabel));
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _theyAreReady;
        public bool TheyAreReady
        {
            get => _theyAreReady;
            set { _theyAreReady = value; OnPropertyChanged(); OnPropertyChanged(nameof(TheirReadyText)); }
        }

        public string ConfirmLabel  => ImReady ? Loc.T("pg.trade.waiting") : Loc.T("pg.trade.confirm");
        public string TheirReadyText => TheyAreReady ? Loc.T("pg.trade.ready") : Loc.T("pg.trade.not_ready");

        private string _goldInput = "0";
        public string GoldInput
        {
            get => _goldInput;
            set { _goldInput = value; OnPropertyChanged(); }
        }

        private string? _resultMessage;
        public string? ResultMessage
        {
            get => _resultMessage;
            private set { SetProperty(ref _resultMessage, value); OnPropertyChanged(nameof(IsResultVisible)); }
        }

        public bool IsResultVisible => _resultMessage != null;

        private DispatcherTimer? _resultTimer;

        public ICommand ConfirmCommand      { get; }
        public ICommand CancelCommand       { get; }
        public ICommand SetGoldCommand      { get; }
        public ICommand RemoveItemCommand   { get; }
        public ICommand DismissResultCommand { get; }

        public InventoryGridViewModel InventoryGrid { get; }

        // Called by ViewModel_PageGame when TradeStarted event fires.
        public static TradeViewModel? Current { get; private set; }

        public TradeViewModel(string partnerName)
        {
            PartnerName  = partnerName;
            Current      = this;
            InventoryGrid = new InventoryGridViewModel(UserAccountService.CurrentCharacter);

            ConfirmCommand    = new RelayCommand(Confirm, () => !ImReady);
            CancelCommand     = new RelayCommand(Cancel);
            SetGoldCommand    = new RelayCommand(SetGold);
            RemoveItemCommand = new RelayCommand<string>(itemId =>
                _ = GameHubService.RemoveTradeItemAsync(itemId));
            DismissResultCommand = new RelayCommand(() => ResultMessage = null);

            GameHubService.TradeUpdated   += OnUpdated;
            GameHubService.TradeCompleted += OnCompleted;
            GameHubService.TradeCancelled += OnCancelled;
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(MyOfferLabel));
            OnPropertyChanged(nameof(PartnerOfferLabel));
            OnPropertyChanged(nameof(MyGoldText));
            OnPropertyChanged(nameof(TheirGoldText));
            OnPropertyChanged(nameof(ImReadyText));
            OnPropertyChanged(nameof(PartnerReadyText));
            OnPropertyChanged(nameof(PartnerNotReadyText));
            OnPropertyChanged(nameof(GoldOfferLabel));
            OnPropertyChanged(nameof(SetLabel));
            OnPropertyChanged(nameof(ConfirmLabel));
            OnPropertyChanged(nameof(TheirReadyText));
            OnPropertyChanged(nameof(CancelTradeLabel));
        }

        public void Unsubscribe()
        {
            GameHubService.TradeUpdated   -= OnUpdated;
            GameHubService.TradeCompleted -= OnCompleted;
            GameHubService.TradeCancelled -= OnCancelled;
            if (Current == this) Current = null;
        }

        private void OnUpdated(TradeSnapshot snap)
        {
            MyItems.Clear();
            TheirItems.Clear();
            foreach (var i in snap.MyItems)
                MyItems.Add(new TradeItemVm { ItemId = i.ItemId, Name = i.Name, Quantity = i.Quantity });
            foreach (var i in snap.TheirItems)
                TheirItems.Add(new TradeItemVm { ItemId = i.ItemId, Name = i.Name, Quantity = i.Quantity });
            MyGold        = snap.MyGold;
            TheirGold     = snap.TheirGold;
            ImReady       = snap.ImReady;
            TheyAreReady  = snap.TheyAreReady;
        }

        private void OnCompleted(TradeCompletedResult result)
        {
            Unsubscribe();
            ApplyResult(result);
            Application.Current.Dispatcher.Invoke(() => ShowResult(Loc.T("pg.trade.completed"), closeWindow: true));
        }

        // Shows the outcome (completion or cancellation) inline in the trade window
        // instead of a native MessageBox, then auto-closes shortly after.
        private void ShowResult(string message, bool closeWindow)
        {
            ResultMessage = message;
            _resultTimer?.Stop();
            _resultTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _resultTimer.Tick += (s, e) =>
            {
                _resultTimer?.Stop();
                ResultMessage = null;
                if (closeWindow)
                    MainWindow.Instance.tradeWindow.Visibility = Visibility.Hidden;
            };
            _resultTimer.Start();
        }

        // Mirrors what the server already did to its own copy of this character — the
        // "TradeCompleted" event previously carried no data, so the client never reflected
        // the trade in the player's actual inventory/gold even though the server had.
        private static void ApplyResult(TradeCompletedResult result)
        {
            var character = UserAccountService.CurrentCharacter;

            foreach (var given in result.GivenItems)
            {
                int remaining = given.Quantity;
                foreach (var stack in character.Inventory.Items.Where(i => i.Id == given.ItemId).ToList())
                {
                    if (remaining <= 0) break;
                    int take = Math.Min(stack.StackSize, remaining);
                    stack.StackSize -= take;
                    remaining -= take;
                    if (stack.StackSize <= 0) character.Inventory.RemoveItem(stack);
                }
            }

            foreach (var received in result.ReceivedItems)
            {
                if (ItemFactory.TryCreateItem(received.ItemId, out var item) && item != null)
                {
                    item.StackSize = received.Quantity;
                    character.Inventory.AddItem(item, character);
                }
            }

            if (result.GoldSpent > 0)    character.Money.TrySpend(result.GoldSpent);
            if (result.GoldReceived > 0) character.Money.TryAdd(result.GoldReceived);
        }

        private void OnCancelled(string reason)
        {
            Unsubscribe();
            string msg = reason switch
            {
                "partner_offline"    => Loc.T("pg.trade.cancelled.partner_offline"),
                "item_not_available" => Loc.T("pg.trade.cancelled.item_not_available"),
                "gold_not_available" => Loc.T("pg.trade.cancelled.gold_not_available"),
                _                    => Loc.T("pg.trade.cancelled")
            };
            Application.Current.Dispatcher.Invoke(() => ShowResult(msg, closeWindow: true));
        }

        public void AddItemByDrop(InventoryItemViewModel item)
        {
            if (item?.Item != null)
                _ = GameHubService.AddTradeItemAsync(item.Item.Id, 1);
        }

        // Set optimistically so the button disables immediately — if both players are
        // already ready, the server jumps straight to TradeCompleted with no snapshot in
        // between, so waiting for a server round-trip would leave a window where Confirm
        // (or another action) could still be pressed while the trade is being finalized.
        private void Confirm()
        {
            ImReady = true;
            _ = GameHubService.ConfirmTradeAsync();
        }
        private void Cancel()  => _ = GameHubService.CancelTradeAsync();

        private void SetGold()
        {
            if (long.TryParse(GoldInput, out long amount))
                _ = GameHubService.SetTradeGoldAsync(amount);
        }
    }
}
