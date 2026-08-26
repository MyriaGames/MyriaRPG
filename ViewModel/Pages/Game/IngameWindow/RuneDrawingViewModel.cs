using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Services.Rune;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Ink;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class RuneDrawingViewModel : BaseViewModel
    {
        protected readonly Character _character;
        private readonly RuneLetterRecognizer _recognizer;

        // ── Letter buffer ─────────────────────────────────────────────────────────

        private readonly List<string> _letterBuffer = new();

        public ObservableCollection<string> RecognizedLetters { get; } = new();

        public string CurrentWord => string.Join("", _letterBuffer);

        public bool CanSubmit => _letterBuffer.Count >= 2;
        public bool CanUndo   => _letterBuffer.Count > 0;

        // ── Recognition feedback ──────────────────────────────────────────────────

        private string _lastRecognized = string.Empty;
        public string LastRecognized
        {
            get => _lastRecognized;
            private set { _lastRecognized = value; OnPropertyChanged(); }
        }

        private float _lastConfidence;
        public float LastConfidence
        {
            get => _lastConfidence;
            private set { _lastConfidence = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConfidenceText)); }
        }

        public string ConfidenceText => _lastConfidence > 0
            ? $"{(int)(_lastConfidence * 100)}%"
            : string.Empty;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _hasResult;
        public bool HasResult
        {
            get => _hasResult;
            private set { _hasResult = value; OnPropertyChanged(); }
        }

        private RuneDrawResultType _resultType;
        public RuneDrawResultType ResultType
        {
            get => _resultType;
            private set { _resultType = value; OnPropertyChanged(); }
        }
        private string _tblTitle = string.Empty;
        private string _tblConfirmLetter = string.Empty;
        private string _tblClearCanvas = string.Empty;
        private string _tblUndo = string.Empty;
        private string _tblSubmit = string.Empty;
        private string _tblClearWord = string.Empty;
        private string _tblBack = string.Empty;
        // ── Localized labels ──────────────────────────────────────────────────────

        [LocalizedKey("pg.rune_draw.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.confirm_letter")]
        public string TblConfirmLetter
        {
            get => _tblConfirmLetter;
            set { _tblConfirmLetter = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.clear_canvas")]
        public string TblClearCanvas
        {
            get => _tblClearCanvas;
            set { _tblClearCanvas = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.undo")]
        public string TblUndo
        {
            get => _tblUndo;
            set { _tblUndo = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.submit")]
        public string TblSubmit
        {
            get => _tblSubmit;
            set { _tblSubmit = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.clear_word")]
        public string TblClearWord
        {
            get => _tblClearWord;
            set { _tblClearWord = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_draw.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────────────

        public ICommand UndoLetterCommand  { get; }
        public ICommand ClearWordCommand   { get; }
        public ICommand BackCommand        { get; }

        // ConfirmLetterCommand and SubmitWordCommand are called from code-behind with StrokeCollection

        public RuneDrawingViewModel()
        {
            _character     = UserAccoundService.CurrentCharacter;
            _recognizer = new RuneLetterRecognizer();

            UndoLetterCommand  = new RelayCommand(UndoLetter,  () => CanUndo);
            ClearWordCommand   = new RelayCommand(ClearWord);
            BackCommand        = new RelayCommand(() => Navigation.Current.Navigate(new View.Pages.Game.IngameWindow.Page_Runes()));
        }

        // ── Debug helpers ─────────────────────────────────────────────────────────

        /// <summary>Exposes the recognizer for debug score inspection from the code-behind.</summary>
        public RuneLetterRecognizer GetRecognizer() => _recognizer;

        // ── Called from code-behind ───────────────────────────────────────────────

        public void ConfirmLetter(StrokeCollection strokes)
        {
            HasResult = false;
            StatusMessage = string.Empty;

            var result = _recognizer.RecognizeLetter(strokes);
            if (result is null)
            {
                StatusMessage = Localization.T("pg.rune_draw.status.unrecognized");
                LastRecognized = string.Empty;
                LastConfidence = 0f;
                return;
            }

            var (letter, confidence) = result.Value;
            LastRecognized = letter;
            LastConfidence = confidence;

            _letterBuffer.Add(letter);
            RecognizedLetters.Add(letter);
            OnPropertyChanged(nameof(CurrentWord));
            OnPropertyChanged(nameof(CanSubmit));
            OnPropertyChanged(nameof(CanUndo));
            ((RelayCommand)UndoLetterCommand).RaiseCanExecuteChanged();
        }

        public void SubmitWord()
        {
            if (_letterBuffer.Count < 2) return;

            HasResult = true;
            var player = _character;

            // Check if the spelled letters form a known base rune
            var baseDef = RuneLetterRecognizer.FindBaseRuneBySpelling(_letterBuffer);

            if (baseDef is null)
            {
                ResultType    = RuneDrawResultType.NoMatch;
                StatusMessage = Localization.T("pg.rune_draw.status.no_match");
                return;
            }

            // Word matched a rune — check if player has the word in their lexica
            var coreWord = RuneWordService.GetWord(baseDef.CoreWordId);
            bool wordKnown = coreWord is not null
                && player.RuneDictionary.Any(e => e.WordId == coreWord.Id && e.IsOfficiallyLearned);

            if (wordKnown)
            {
                ResultType    = RuneDrawResultType.KnownRune;
                StatusMessage = string.Format(Localization.T("pg.rune_draw.status.known_rune"), baseDef.Name);

                // Grant the rune if the player doesn't have it yet
                var alreadyHas = player.KnownRunes.Any(r => r.BaseRuneId == baseDef.Id && r.AddedWordIds.Count == 0);
                if (!alreadyHas)
                    ExecuteGrantRune(player, baseDef.Id);
            }
            else
            {
                ResultType    = RuneDrawResultType.UnknownRune;
                StatusMessage = Localization.T("pg.rune_draw.status.unknown_rune");
                // Rune fires (in a real combat context) but no lexica entry is created
            }

            ClearWord();
        }

        protected virtual void ExecuteGrantRune(Character player, string baseRuneId)
        {
            BaseRuneService.GrantBaseRune(player, baseRuneId);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UndoLetter()
        {
            if (_letterBuffer.Count == 0) return;
            _letterBuffer.RemoveAt(_letterBuffer.Count - 1);
            if (RecognizedLetters.Count > 0)
                RecognizedLetters.RemoveAt(RecognizedLetters.Count - 1);
            OnPropertyChanged(nameof(CurrentWord));
            OnPropertyChanged(nameof(CanSubmit));
            OnPropertyChanged(nameof(CanUndo));
            ((RelayCommand)UndoLetterCommand).RaiseCanExecuteChanged();
        }

        private void ClearWord()
        {
            _letterBuffer.Clear();
            RecognizedLetters.Clear();
            LastRecognized = string.Empty;
            LastConfidence = 0f;
            OnPropertyChanged(nameof(CurrentWord));
            OnPropertyChanged(nameof(CanSubmit));
            OnPropertyChanged(nameof(CanUndo));
            ((RelayCommand)UndoLetterCommand).RaiseCanExecuteChanged();
        }
    }
}
