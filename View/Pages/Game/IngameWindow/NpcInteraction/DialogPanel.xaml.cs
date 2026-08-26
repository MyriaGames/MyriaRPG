using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Wpf.Services;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.UserControls;
using Myria.Wpf.ViewModel.UserControls.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class DialogPanel : Page
    {
        private readonly Npc _npc;

        private const double DefaultWidth  = 620;
        private const double DefaultHeight = 480;
        private const double ShopWidth     = 900;
        private const double ShopHeight    = 700;

        public DialogPanel(Npc npc)
        {
            _npc = npc;
            InitializeComponent();
            // Loaded fires both on first navigation and when GoBack() returns to this page
            Loaded += (_, _) => Rebuild();
        }

        private void Rebuild()
        {
            SetNpcWindowSize(DefaultWidth, DefaultHeight);
            DataContext = GameHubService.IsConnected
                ? new MultiplayerDialogPanelViewModel(_npc, UserAccountService.CurrentCharacter, NavigateToService)
                : new DialogPanelViewModel(_npc, UserAccountService.CurrentCharacter, NavigateToService);
        }

        private void NavigateToService(string serviceId)
        {
            if (serviceId is "shop" or "buy_items" or "shop_equipment" or "shop_general")
                SetNpcWindowSize(ShopWidth, ShopHeight);

            Page? page = serviceId switch
            {
                "shop" or "buy_items" or "shop_equipment" or "shop_general" => new ShopPanel(_npc),
                "upgrade"      => new UpgradePanel(_npc),
                "craft"        => new CraftPanel(_npc),
                "learn_job"    => new JobMasterPanel(_npc),
                "change_class" => new ClassPanel(),
                _ when serviceId.StartsWith("quest:") =>
                    QuestManager.GetQuestById(serviceId[6..]) is Quest q
                        ? new QuestDialogPanel(q, _npc, isReturn: false)
                        : null,
                _ when serviceId.StartsWith("quest_return:") =>
                    UserAccountService.CurrentCharacter.ActiveQuests
                        .FirstOrDefault(q => q.Id == serviceId[13..]) is Quest rq
                        ? new QuestDialogPanel(rq, _npc, isReturn: true)
                        : null,
                _ => null
            };

            if (page != null)
                Myria.Wpf.Services.Navigation.Current.Navigate(NavigationFrameType.NpcWindow, page);
        }

        private static void SetNpcWindowSize(double width, double height)
        {
            if (MainWindow.Instance.npcWindow.DataContext is ViewModel_NpcWindow vm)
            {
                vm.Width  = width;
                vm.Height = height;
            }
        }
    }
}
