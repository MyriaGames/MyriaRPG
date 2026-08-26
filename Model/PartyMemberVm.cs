using Myria.Wpf.ViewModel;

namespace Myria.Wpf.Model
{
    public class PartyMemberVm : BaseViewModel
    {
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        private bool _isLeader;
        public bool IsLeader
        {
            get => _isLeader;
            set { _isLeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string DisplayName => IsLeader ? $"{Username} [Leader]" : Username;

        private int _level;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        private int _hp;
        public int Hp
        {
            get => _hp;
            set { _hp = value; OnPropertyChanged(); OnPropertyChanged(nameof(HpDisplay)); }
        }

        private int _maxHp = 1;
        public int MaxHp
        {
            get => _maxHp;
            set { _maxHp = value; OnPropertyChanged(); OnPropertyChanged(nameof(HpDisplay)); }
        }

        private int _mana;
        public int Mana
        {
            get => _mana;
            set { _mana = value; OnPropertyChanged(); OnPropertyChanged(nameof(ManaDisplay)); }
        }

        private int _maxMana = 1;
        public int MaxMana
        {
            get => _maxMana;
            set { _maxMana = value; OnPropertyChanged(); OnPropertyChanged(nameof(ManaDisplay)); }
        }

        private bool _showLeaderActions;
        public bool ShowLeaderActions
        {
            get => _showLeaderActions;
            set { _showLeaderActions = value; OnPropertyChanged(); }
        }

        private bool _isCurrentTurn;
        public bool IsCurrentTurn
        {
            get => _isCurrentTurn;
            set { _isCurrentTurn = value; OnPropertyChanged(); }
        }

        public string HpDisplay   => $"{Hp}/{MaxHp}";
        public string ManaDisplay => $"{Mana}/{MaxMana}";
    }
}
