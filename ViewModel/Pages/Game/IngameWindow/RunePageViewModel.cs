using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class RunePageViewModel : BaseViewModel
    {
        private const int PageSize = 5;

        private readonly List<RuneVm> _allRunes = new();
        private int _currentPage = 1;
        private string _tblTitle = string.Empty;
        private string _tblDraw = string.Empty;
        private string _tblLexica = string.Empty;
        private string _tblDetails = string.Empty;
        private string _tblMp = string.Empty;
        public ObservableCollection<RuneVm> PagedRunes { get; } = new();

        [LocalizedKey("pg.runes.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.runes.draw")]
        public string TblDraw
        {
            get => _tblDraw;
            set { _tblDraw = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.runes.lexica")]
        public string TblLexica
        {
            get => _tblLexica;
            set { _tblLexica = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.details")]
        public string TblDetails
        {
            get => _tblDetails;
            set { _tblDetails = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.skills.mp")]
        public string TblMp
        {
            get => _tblMp;
            set { _tblMp = value; OnPropertyChanged(); }
        }

        public string HeaderText => string.Format(Localization.T("pg.runes.header"), _allRunes.Count);

        public string WindowTitle => Localization.T("pg.runes.title");

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = Math.Clamp(value, 1, TotalPages);
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageText));
                RefreshPage();
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(_allRunes.Count / (double)PageSize));
        public string PageText => $"{_currentPage} / {TotalPages}";

        private RelayCommand _prevPageCommand = null!;
        private RelayCommand _nextPageCommand = null!;
        public ICommand PrevPageCommand => _prevPageCommand;
        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand OpenDrawCommand { get; }
        public ICommand OpenLexicaCommand { get; }

        public RunePageViewModel()
        {
            var player = UserAccoundService.CurrentCharacter;

            foreach (var rune in player.KnownRunes)
            {
                var baseDef = BaseRuneService.Get(rune.BaseRuneId);
                _allRunes.Add(new RuneVm(rune, baseDef, player));
            }

            OpenDrawCommand   = new RelayCommand(() => Navigation.Current.Navigate(new Page_RuneDrawing()));
            OpenLexicaCommand = new RelayCommand(() => Navigation.Current.Navigate(new Page_RuneLexica()));

            _prevPageCommand = new RelayCommand(() => CurrentPage--, () => _currentPage > 1);
            _nextPageCommand = new RelayCommand(() => CurrentPage++, () => _currentPage < TotalPages);
            RefreshPage();
        }

        private void RefreshPage()
        {
            PagedRunes.Clear();
            foreach (var r in _allRunes.Skip((_currentPage - 1) * PageSize).Take(PageSize))
                PagedRunes.Add(r);
            _prevPageCommand?.RaiseCanExecuteChanged();
            _nextPageCommand?.RaiseCanExecuteChanged();
        }
    }

    public class RuneVm : BaseViewModel
    {
        private readonly CompositeRune _rune;
        private readonly Character _character;

        public RuneVm(CompositeRune rune, Myria.Lib.Core.Models.BaseRuneData? baseDef, Character character)
        {
            _rune   = rune;
            _character = character;
            BaseName        = baseDef?.Name ?? rune.BaseRuneId;
            Description     = rune.ResolvedSkill?.Description ?? baseDef?.Description ?? string.Empty;
            ManaCost        = rune.ResolvedSkill?.ManaCost ?? baseDef?.BaseManaCost ?? 0;
            ScalingFactor   = rune.ResolvedSkill?.ScalingFactor ?? baseDef?.BaseScalingFactor ?? 0f;
            StatToScaleFrom = rune.ResolvedSkill?.StatToScaleFrom ?? baseDef?.StatToScaleFrom ?? "MATK";
            IsHealing       = rune.ResolvedSkill?.IsHealing ?? baseDef?.IsHealing ?? false;
            WordCount       = rune.AddedWordIds.Count;

            AddedWordDisplays = rune.AddedWordIds
                .Select(id =>
                {
                    var w = Myria.Lib.Core.Services.RuneWordService.GetWord(id);
                    return w is null ? id : RuneManager.GetDisplayName(character, w);
                })
                .ToList();
        }

        public string BaseName { get; }
        public string Description { get; }
        public int ManaCost { get; }
        public float ScalingFactor { get; }
        public string StatToScaleFrom { get; }
        public bool IsHealing { get; }
        public int WordCount { get; }
        public List<string> AddedWordDisplays { get; }

        public string WordsText => AddedWordDisplays.Count > 0
            ? string.Join(" · ", AddedWordDisplays)
            : string.Empty;

        public string ShortDescription =>
            string.IsNullOrWhiteSpace(Description) ? string.Empty
            : Description.Length > 110 ? Description[..110] + "…"
            : Description;
    }
}
