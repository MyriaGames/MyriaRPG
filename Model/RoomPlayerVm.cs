using Myria.Wpf.ViewModel;

namespace Myria.Wpf.Model
{
    public class RoomCharacterVm : BaseViewModel
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
    }
}
