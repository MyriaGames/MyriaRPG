using Myria.Lib.Core.Models.Settings;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsLanguage : BaseViewModel
    {
        private string _tblLanguage;
        private LanguageOption _selectedLanguage;

        [LocalizedKey("pg.settings.language")]
        public string TblLanguage
        {
            get => _tblLanguage;
            set { _tblLanguage = value; OnPropertyChanged(); }
        }

        public List<LanguageOption> Languages { get; }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();

                // Persist selection.
                if (value.IsBuiltIn)
                {
                    Settings.Current.LanguageSettings.Local        = value.Id == "de" ? GameLanguage.De : GameLanguage.En;
                    Settings.Current.LanguageSettings.ModLanguageId = null;
                }
                else
                {
                    Settings.Current.LanguageSettings.ModLanguageId = value.Id;
                }
                SettingsService.Save();

                Localization.Load(value.Id);
                ViewModel_PageRoom.RefreshLocalisation();
            }
        }

        public ViewModel_SettingsLanguage()
        {
            // Build the combined list: built-in languages first, then mod languages.
            Languages = Enum.GetValues<GameLanguage>()
                .Select(LanguageOption.ForBuiltIn)
                .Concat(ModLoader.GetModLanguages()
                    .Select(ml => LanguageOption.ForMod(ml.Def)))
                .ToList();

            var currentId = Settings.Current.LanguageSettings.EffectiveLanguageId;
            _selectedLanguage = Languages.FirstOrDefault(l =>
                    l.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                ?? Languages.First();

            LocalizationAutoWire.Wire(this);
        }
    }

    /// <summary>Represents a selectable language in the settings UI, built-in or mod-provided.</summary>
    public sealed class LanguageOption
    {
        public string Id          { get; }
        public string DisplayName { get; }
        public bool   IsBuiltIn   { get; }

        private LanguageOption(string id, string displayName, bool isBuiltIn)
        {
            Id          = id;
            DisplayName = displayName;
            IsBuiltIn   = isBuiltIn;
        }

        public static LanguageOption ForBuiltIn(GameLanguage lang) => new(
            lang == GameLanguage.De ? "de" : "en",
            lang == GameLanguage.De ? "Deutsch" : "English",
            isBuiltIn: true);

        public static LanguageOption ForMod(ModLanguageDefinition def) => new(
            def.Id,
            string.IsNullOrWhiteSpace(def.DisplayName) ? def.Id : def.DisplayName,
            isBuiltIn: false);

        public override string ToString() => DisplayName;
    }
}
