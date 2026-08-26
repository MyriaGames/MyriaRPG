using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_Loading : Page
    {
        public Page_Loading()
        {
            InitializeComponent();
            DataContext = new ViewModel_LoadingPage();
        }
    }
}
