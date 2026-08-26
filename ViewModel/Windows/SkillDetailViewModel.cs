using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;

namespace Myria.Wpf.ViewModel.Windows
{
    public class SkillDetailViewModel : BaseViewModel
    {
        private readonly SkillVm _skill;
        public SkillDetailViewModel(SkillVm skill) { _skill = skill; }

        public string Name => _skill.Name;
        public string ClassName => _skill.ClassName;
        public string TypeText => _skill.TypeText;
        public string TargetText => _skill.TargetText;
        public string Description => _skill.Description;
        public int ManaCost => _skill.ManaCost;
        public int MinLevel => _skill.MinLevel;
        public bool IsHealing => _skill.IsHealing;
        public string TimingText => _skill.TimingText;
        public string ScalingText => $"{_skill.ScalingFactor:0.##} × {_skill.StatToScaleFrom}";
    }
}