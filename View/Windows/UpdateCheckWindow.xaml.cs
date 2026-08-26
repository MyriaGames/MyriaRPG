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
        public UpdateCheckWindow()
        {
            InitializeComponent();
        }

        public void Apply(UpdateProgress progress)
        {
            switch (progress.Status)
            {
                case UpdateStatus.Checking:
                    StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.checking");
                    Progress.IsIndeterminate = true;
                    break;
                case UpdateStatus.Downloading:
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
                    StatusText.Text = Myria.Lib.Core.Systems.Localization.T("update.restarting");
                    Progress.IsIndeterminate = true;
                    break;
                case UpdateStatus.UpToDate:
                case UpdateStatus.Failed:
                    // Window is about to close either way - no further UI update needed.
                    break;
            }
        }
    }
}
