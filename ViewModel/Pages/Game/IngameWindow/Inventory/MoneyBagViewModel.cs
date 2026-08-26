using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Model;
using Myria.Wpf.ViewModel;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    /// <summary>
    /// ViewModel for the money bag display.
    /// Subscribes to player.Inventory.ItemReceived to refresh after buy/sell operations.
    /// </summary>
    public class MoneyBagViewModel : BaseViewModel
    {
        private readonly Character _character;
        private string _moneyBagTitle;
        private string _moneyDisplay;

        [LocalizedKey("pg.inventory.moneybag.title")]
        public string MoneyBagTitle { get => _moneyBagTitle; set => SetProperty(ref _moneyBagTitle, value); }

        public string MoneyDisplay { get => _moneyDisplay; set => SetProperty(ref _moneyDisplay, value); }

        public MoneyBagViewModel(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _character.Inventory.ItemReceived += (s, e) => UpdateMoneyDisplay();
            _character.Inventory.ItemSold += (s, e) => UpdateMoneyDisplay();
            UpdateMoneyDisplay();
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            UpdateMoneyDisplay();
        }

        public void UpdateMoneyDisplay()
        {
            MoneyDisplay = _character.Money.Coins.ToDisplayString();
        }
    }
}
