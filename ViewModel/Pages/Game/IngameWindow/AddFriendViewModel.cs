using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class AddFriendViewModel : BaseViewModel
    {
        private string _tblInstruction = string.Empty;
        private string _tblSendRequest = string.Empty;
        [LocalizedKey("pg.friends.add.instruction")]
        public string TblInstruction
        {
            get { return _tblInstruction; }
            set
            {
                _tblInstruction = value;
                OnPropertyChanged();
            }
        }

        [LocalizedKey("pg.friends.send_request")]
        public string TblSendRequest
        {
            get { return _tblSendRequest; }
            set
            {
                _tblSendRequest = value;
                OnPropertyChanged();
            }
        }

        private string _characterName = string.Empty;
        public string CharacterName
        {
            get => _characterName;
            set { _characterName = value; OnPropertyChanged(); }
        }

        private string _feedback = string.Empty;
        public string Feedback
        {
            get => _feedback;
            set { _feedback = value; OnPropertyChanged(); }
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set { _isSuccess = value; OnPropertyChanged(); }
        }

        public ICommand SendCommand { get; }

        public AddFriendViewModel()
        {
            SendCommand = new RelayCommand(Send);
        }

        private async void Send()
        {
            var target = CharacterName.Trim();
            if (string.IsNullOrEmpty(target)) return;

            Feedback   = string.Empty;
            IsSuccess  = false;
            var error  = await ServerApiService.SendFriendRequestAsync(target);
            if (error is null)
            {
                CharacterName = string.Empty;
                Feedback      = string.Format(Localization.T("pg.friends.request_sent"), target);
                IsSuccess     = true;
            }
            else
            {
                Feedback = error;
            }
        }
    }
}
