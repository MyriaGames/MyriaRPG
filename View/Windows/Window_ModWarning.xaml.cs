using System.Windows;

namespace Myria.Wpf.View.Windows
{
    public partial class Window_ModWarning : Window
    {
        public string TitleText    { get; }
        public string SubtitleText { get; }
        public string DetailsLabel { get; }
        public string DetailsText  { get; }
        public string ContinueLabel { get; }
        public string CancelLabel   { get; }

        public bool HasDetails => !string.IsNullOrWhiteSpace(DetailsText);

        /// <param name="title">Short heading, e.g. "Mod Configuration Changed".</param>
        /// <param name="subtitle">One-sentence explanation shown below the heading.</param>
        /// <param name="detailsLabel">Small caption above the detail box, e.g. "Changes".</param>
        /// <param name="details">Formatted list of changes. Pass null or empty to hide the panel.</param>
        public Window_ModWarning(
            string title,
            string subtitle,
            string detailsLabel = "",
            string? details = null)
        {
            TitleText    = title;
            SubtitleText = subtitle;
            DetailsLabel = detailsLabel;
            DetailsText  = details ?? "";
            ContinueLabel = L("app.general.UI.continue", "Continue");
            CancelLabel   = L("app.general.UI.cancel",   "Cancel");

            InitializeComponent();
            DataContext = this;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string L(string key, string fallback)
        {
            try
            {
                var result = Myria.Lib.Core.Systems.Localization.T(key);
                return result.StartsWith('[') ? fallback : result;
            }
            catch { return fallback; }
        }
    }

}
