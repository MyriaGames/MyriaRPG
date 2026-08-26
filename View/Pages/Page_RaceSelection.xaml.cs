using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_RaceSelection : Page
    {
        public Page_RaceSelection(Character character)
        {
            InitializeComponent();
            DataContext = new ViewModel_RaceSelectionPage(
                character,
                () => Myria.Wpf.Services.Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection()));
        }
    }
}
