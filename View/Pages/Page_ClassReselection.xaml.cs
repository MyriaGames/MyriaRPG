using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_ClassReselection : Page
    {
        public Page_ClassReselection(Character character)
        {
            InitializeComponent();
            DataContext = new ViewModel_ClassReselectionPage(
                character,
                () => Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection()));
        }
    }
}
