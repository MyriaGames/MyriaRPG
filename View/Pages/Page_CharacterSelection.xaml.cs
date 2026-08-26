using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    /// <summary>
    /// Interaktionslogik für Page_CharacterSelection.xaml
    /// </summary>
    public partial class Page_CharacterSelection : Page
    {
        public Page_CharacterSelection()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_CharacterSelectionPage();
        }

    }

}
