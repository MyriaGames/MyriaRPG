using Myria.Wpf.ViewModel;
using Myria.Wpf.Utils;

namespace Myria.Wpf.Model
{
    public class GroupCombatantVm : BaseViewModel
    {
        private readonly string _rawName;
        private int  _hp;
        private bool _isAlive;

        public string RawName => _rawName;
        public string Name   => LocalizationText.LocalizeMonsterName(_rawName);
        public int    MaxHp  { get; }
        public int    Level  { get; }

        public int  Hp
        {
            get => _hp;
            set { _hp = value; OnPropertyChanged(); }
        }

        public bool IsAlive
        {
            get => _isAlive;
            set { _isAlive = value; OnPropertyChanged(); }
        }

        public string HpText => $"{Hp}/{MaxHp}";

        public GroupCombatantVm(string name, int hp, int maxHp, bool isAlive, int level = 0)
        {
            _rawName = name;
            MaxHp    = maxHp;
            Level    = level;
            _hp      = hp;
            _isAlive = isAlive;
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(Name));
        }
    }
}
