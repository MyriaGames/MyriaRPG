using System;
using System.Threading.Tasks;
using System.Windows;
using Myria.Wpf.Services;

namespace Myria.Wpf.View.Windows
{
    /// <summary>
    /// Small pre-launch window shown while the startup update check runs. Reused unchanged for
    /// both the leader (real progress) and a follower (just shows "checking" while it waits for
    /// the leader to finish - see UpdateCoordinator) so the user sees consistent behavior
    /// regardless of which instance actually does the work.
    /// </summary>
    public partial class UpdateCheckWindow : Window
    {
        private TaskCompletionSource<bool>? _confirmTcs;

        public UpdateCheckWindow()
        {
            InitializeComponent();
        }

        public void Apply(UpdateProgress progress)
        {
            switch (progress.Status)
            {
                case UpdateStatus.Checking:
                    ShowProgressState();
                    StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.checking");
                    Progress.IsIndeterminate = true;
                    break;
                case UpdateStatus.Downloading:
                    ShowProgressState();
                    if (progress.PercentComplete is { } percent)
                    {
                        StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.downloading", (int)percent);
                        Progress.IsIndeterminate = false;
                        Progress.Value = percent;
                    }
                    else
                    {
                        StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.downloading", 0);
                        Progress.IsIndeterminate = true;
                    }
                    break;
                case UpdateStatus.LaunchingInstaller:
                    ShowProgressState();
                    StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.restarting");
                    Progress.IsIndeterminate = true;
                    break;
                case UpdateStatus.PendingConfirmation:
                    ShowConfirmState(progress.PendingVersion, progress.PendingNotes);
                    break;
                case UpdateStatus.UpToDate:
                case UpdateStatus.Failed:
                case UpdateStatus.Declined:
                    // Window is about to close either way - no further UI update needed.
                    break;
            }
        }

        /// <summary>
        /// Swaps in the confirm/decline UI and returns a task that completes when the user picks
        /// one - passed as UpdateService.CheckForUpdatesAsync's confirmCompatBreakUpdate callback.
        /// The version/notes arguments here are redundant with what Apply(PendingConfirmation)
        /// already received (same call site reports progress right before awaiting this), but the
        /// method takes them directly too so it stands alone as a callback signature.
        /// </summary>
        public Task<bool> AskConfirmUpdateAsync(string version, string? notes)
        {
            ShowConfirmState(version, notes);
            _confirmTcs = new TaskCompletionSource<bool>();
            return _confirmTcs.Task;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e) =>
            _confirmTcs?.TrySetResult(true);

        private void DeclineButton_Click(object sender, RoutedEventArgs e) =>
            _confirmTcs?.TrySetResult(false);

        private void ShowProgressState()
        {
            Progress.Visibility = Visibility.Visible;
            NotesPanel.Visibility = Visibility.Collapsed;
            ConfirmPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowConfirmState(string? version, string? notes)
        {
            Progress.Visibility = Visibility.Collapsed;
            StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.compat_break.body", version ?? "?");

            bool hasNotes = !string.IsNullOrWhiteSpace(notes);
            NotesPanel.Visibility = hasNotes ? Visibility.Visible : Visibility.Collapsed;
            if (hasNotes)
            {
                NotesLabel.Text = Myria.Lib.Core.Systems.Localization.T("update.compat_break.notes_label");
                NotesText.Text = notes;
            }

            ConfirmButton.Content = Myria.Lib.Core.Systems.Localization.T("update.compat_break.confirm");
            DeclineButton.Content = Myria.Lib.Core.Systems.Localization.T("update.compat_break.decline");
            ConfirmPanel.Visibility = Visibility.Visible;
        }
    }
}
