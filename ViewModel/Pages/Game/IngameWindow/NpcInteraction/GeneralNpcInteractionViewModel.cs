using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction
{
    public class GeneralNpcInteractionViewModel : BaseViewModel
    {
        private string _npcName;
        private string _btnClose;

        public string NpcName
        {
            get => _npcName;
            set { _npcName = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.close")]
        public string BtnClose
        {
            get => _btnClose;
            set { _btnClose = value; OnPropertyChanged(); }
        }

        public ICommand CloseCommand { get; }

        public GeneralNpcInteractionViewModel(Npc npc)
        {
            NpcName = Myria.Lib.Core.Systems.Localization.T(npc.NameKey);
            CloseCommand = new RelayCommand(() => MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden);
        }
    }

    public class ServiceOption : BaseViewModel
    {
        public string Text { get; set; }
        public string Description { get; set; }
        public ICommand Command { get; set; }
    }
}
