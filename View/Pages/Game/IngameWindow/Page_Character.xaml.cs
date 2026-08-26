using Myria.Lib.Core.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Character : Page
    {
        private CharacterPageViewModel _vm = null!;

        public Page_Character()
        {
            InitializeComponent();
            _vm = new CharacterPageViewModel(UserAccountService.CurrentCharacter);
            DataContext = _vm;

            // Disable journal navigation on the sub-frame
            TabFrame.Navigating += (s, e) =>
            {
                if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
                    e.Cancel = true;
            };

            _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CharacterPageViewModel.SelectedTab)) return;
            switch (_vm.SelectedTab)
            {
                case CharacterPageViewModel.CharacterTab.Skills:
                    TabFrame.Navigate(new Page_Skills { DataContext = new SkillPageViewModel() });
                    break;
                case CharacterPageViewModel.CharacterTab.Jobs:
                    TabFrame.Navigate(new Page_Jobs());
                    break;
                case CharacterPageViewModel.CharacterTab.Quests:
                    TabFrame.Navigate(new Page_QuestList());
                    break;
            }
        }
    }
}
