using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Models.BaseModel;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class RuneLexicaViewModel : BaseViewModel
    {
        private readonly Character _character;
        private string _tblTitle = string.Empty;
        private string _tblKnown = string.Empty;
        private string _tblSaveLabel = string.Empty;
        private string _tblBack = string.Empty;
        private string _tblColWord = string.Empty;
        private string _tblColGuess = string.Empty;
        private string _tblColMeaning = string.Empty;

        public ObservableCollection<LexicaEntryVm> Entries { get; } = new();

        [LocalizedKey("pg.rune_lexica.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.known")]
        public string TblKnown
        {
            get => _tblKnown;
            set { _tblKnown = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.save_label")]
        public string TblSaveLabel
        {
            get => _tblSaveLabel;
            set { _tblSaveLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.col_word")]
        public string TblColWord
        {
            get => _tblColWord;
            set { _tblColWord = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.col_guess")]
        public string TblColGuess
        {
            get => _tblColGuess;
            set { _tblColGuess = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.rune_lexica.col_meaning")]
        public string TblColMeaning
        {
            get => _tblColMeaning;
            set { _tblColMeaning = value; OnPropertyChanged(); }
        }

        public string HeaderText => string.Format(Localization.T("pg.rune_lexica.header"), Entries.Count);

        public ICommand BackCommand { get; }

        public RuneLexicaViewModel()
        {
            _character = UserAccoundService.CurrentCharacter;

            BackCommand = new RelayCommand(() => Navigation.Current.Navigate(new View.Pages.Game.IngameWindow.Page_Runes()));

            Refresh();
        }

        public void SaveLabel(LexicaEntryVm entry)
        {
            RuneManager.SetCharacterLabel(_character, entry.WordId, entry.EditLabel ?? "");
            entry.RefreshDisplay(_character);
        }

        private void Refresh()
        {
            Entries.Clear();
            foreach (var dictEntry in _character.RuneDictionary)
            {
                var word = RuneWordService.GetWord(dictEntry.WordId);
                if (word is null) continue;
                Entries.Add(new LexicaEntryVm(dictEntry, word, _character));
            }
        }
    }

    public class LexicaEntryVm : BaseViewModel
    {
        private readonly CharacterRuneWordEntry _entry;
        private readonly RuneWord _word;

        public LexicaEntryVm(CharacterRuneWordEntry entry, RuneWord word, Character character)
        {
            _entry   = entry;
            _word    = word;
            WordId   = word.Id;
            Script   = word.RunicScript;
            EditLabel = entry.CharacterLabel ?? string.Empty;
            RefreshDisplay(character);
        }

        public string WordId { get; }
        public string Script { get; }

        public bool IsOfficiallyLearned => _entry.IsOfficiallyLearned;

        public string OfficialTranslation => _entry.IsOfficiallyLearned ? _word.EnglishName : string.Empty;

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            private set { _displayName = value; OnPropertyChanged(); }
        }

        private string _editLabel = string.Empty;
        public string EditLabel
        {
            get => _editLabel;
            set { _editLabel = value; OnPropertyChanged(); }
        }

        public void RefreshDisplay(Character player)
        {
            DisplayName = RuneManager.GetDisplayName(player, _word);
        }
    }
}
