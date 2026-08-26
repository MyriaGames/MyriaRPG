using System.Globalization;
using System.Windows.Data;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;

namespace Myria.Wpf.View.Converters
{
    // Bound to a deposit tile's IsEnabled: false (disabled) once that exact tile is already
    // staged for deposit on the shop page - dims it so a repeat click isn't a silent no-op with
    // no visible reason why (see CharacterShopViewModel.SelectInventoryItem).
    public class IsItemStagedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not InventoryItemViewModel item ||
                values[1] is not IEnumerable<DepositStagingItemVm> staged)
                return true; // enabled by default

            return !staged.Any(s => ReferenceEquals(s.InventoryItem, item));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
