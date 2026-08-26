using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    /// <summary>
    /// Interaktionslogik für Page_CharacterCreation.xaml
    /// </summary>
    public partial class Page_CharacterCreation : Page
    {
        public Page_CharacterCreation()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_CharacterCreationPage();
        }
    }
}
