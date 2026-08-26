using System.Windows;
using System.Windows.Controls;
using Myria.Wpf.Services;

namespace Myria.Wpf.View.UserControls
{
    public partial class TradeProposalWindow : UserControl
    {
        private string _tradeId = string.Empty;

        public TradeProposalWindow()
        {
            InitializeComponent();
        }

        public void Show(string fromName, string tradeId)
        {
            _tradeId = tradeId;
            tbl_Message.Text = $"{fromName} wants to trade with you.";
            Visibility = Visibility.Visible;
            // int.MaxValue — no other window ZIndex call can exceed this
            Panel.SetZIndex(this, int.MaxValue);
        }

        public void ForceClose()
        {
            Visibility = Visibility.Hidden;
        }

        private async void OnAccept(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            await GameHubService.AcceptTradeAsync(_tradeId);
        }

        private async void OnDecline(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
            await GameHubService.DeclineTradeAsync(_tradeId);
        }
    }
}
