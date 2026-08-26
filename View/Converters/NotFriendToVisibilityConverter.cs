using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Myria.Wpf.ViewModel.Pages.Game;

namespace Myria.Wpf.View.Converters
{
    /// <summary>
    /// Hides "Send Friend Request" menu items for a target that's already a friend.
    /// Expects [0] = ViewModel_PageGame, [1] = the target character's name.
    /// </summary>
    public class NotFriendToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not ViewModel_PageGame vm || values[1] is not string name)
                return Visibility.Visible;

            return vm.IsFriend(name) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
