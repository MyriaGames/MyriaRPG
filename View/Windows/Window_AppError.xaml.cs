using System.Windows;

namespace Myria.Wpf.View.Windows
{
    public partial class Window_AppError : Window
    {
        public string TitleText    { get; }
        public string SubtitleText { get; }
        public string StepLabel    { get; }
        public string DetailsLabel { get; }
        public string CopyLabel    { get; }
        public string CloseLabel   { get; }
        public string FailedStep   { get; }
        public string ErrorMessage { get; }

        public Window_AppError(string failedStep, string errorMessage)
        {
            FailedStep   = failedStep;
            ErrorMessage = errorMessage;
            TitleText    = L("app.error.title",       "An Error Occurred");
            SubtitleText = L("app.error.subtitle",    "Something went wrong. Please try again or report this issue.");
            StepLabel    = L("app.error.label.step",  "Step");
            DetailsLabel = L("app.error.label.details","Details");
            CopyLabel    = L("app.general.UI.copy",   "Copy");
            CloseLabel   = L("app.general.UI.close",  "Close");

            InitializeComponent();
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"{StepLabel}:{Environment.NewLine}{FailedStep}{Environment.NewLine}{Environment.NewLine}{DetailsLabel}:{Environment.NewLine}{ErrorMessage}");
        }

        // Falls back to the hardcoded English string if localization hasn't loaded yet
        // (which in practice doesn't happen — Localization.Load() runs before any window opens).
        private static string L(string key, string fallback)
        {
            try
            {
                var result = Myria.Lib.Core.Systems.Localization.T(key);
                return result.StartsWith('[') ? fallback : result;

            }
            catch
            {
                return fallback;
            }
            return fallback;
        }
    }
}
