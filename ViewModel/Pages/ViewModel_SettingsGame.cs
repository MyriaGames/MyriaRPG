using Myria.Lib.Core.Models.Settings;
using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsGame : BaseViewModel
    {
        private string _tblGameTitle;
        private string _tblGameDescription;
        private string _tblServerSection;
        private string _tblServerAddress;
        private string _tblServerHint;
        private string _tblUpdateSection;
        private string _tblAutoUpdate;
        private string _serverAddress;
        private bool _autoUpdateEnabled;

        [LocalizedKey("pg.settings.game.title")]
        public string TblGameTitle
        {
            get => _tblGameTitle;
            set { _tblGameTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.game.description")]
        public string TblGameDescription
        {
            get => _tblGameDescription;
            set { _tblGameDescription = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.game.server_section")]
        public string TblServerSection
        {
            get => _tblServerSection;
            set { _tblServerSection = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.server.address")]
        public string TblServerAddress
        {
            get => _tblServerAddress;
            set { _tblServerAddress = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.server.hint")]
        public string TblServerHint
        {
            get => _tblServerHint;
            set { _tblServerHint = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.game.update_section")]
        public string TblUpdateSection
        {
            get => _tblUpdateSection;
            set { _tblUpdateSection = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.game.autoupdate")]
        public string TblAutoUpdate
        {
            get => _tblAutoUpdate;
            set { _tblAutoUpdate = value; OnPropertyChanged(); }
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                var normalized = ServerApiService.NormalizeAddress(value ?? "");
                if (_serverAddress == normalized) return;
                _serverAddress = normalized;
                OnPropertyChanged();

                Settings.Current.ServerAddress = normalized;
                SettingsService.Save();

                if (!string.IsNullOrWhiteSpace(normalized))
                    ServerApiService.AuthBaseUrl = normalized;
            }
        }

        public bool AutoUpdateEnabled
        {
            get => _autoUpdateEnabled;
            set
            {
                if (_autoUpdateEnabled == value) return;
                _autoUpdateEnabled = value;
                OnPropertyChanged();

                Settings.Current.AutoUpdateEnabled = value;
                SettingsService.Save();
            }
        }

        public ViewModel_SettingsGame()
        {
            _serverAddress = ServerApiService.NormalizeAddress(Settings.Current.ServerAddress ?? "");
            _autoUpdateEnabled = Settings.Current.AutoUpdateEnabled;
            LocalizationAutoWire.Wire(this);
        }
    }
}
