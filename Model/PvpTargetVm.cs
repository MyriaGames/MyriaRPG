using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using System.Windows.Input;

namespace Myria.Wpf.Model
{
    /// <summary>One challengeable player in an arena's challenge list.</summary>
    public class PvpTargetVm
    {
        public string Name { get; }
        public string Line { get; }
        public string BtnDuel => Localization.T("pvp.duel");
        public string BtnPartyFight => Localization.T("pvp.party_fight");
        public ICommand DuelCommand { get; }
        public ICommand PartyFightCommand { get; }

        public PvpTargetVm(PvpTargetInfo info, Action<string, bool> challenge)
        {
            Name = info.Name;
            string line = Localization.T("pvp.target_line", info.Name, info.Level, info.Class);
            if (info.PartySizeHere > 1)
                line += "  ·  " + Localization.T("pvp.party_tag", info.PartySizeHere);
            Line = line;
            DuelCommand       = new RelayCommand(() => challenge(Name, false));
            PartyFightCommand = new RelayCommand(() => challenge(Name, true));
        }
    }
}
