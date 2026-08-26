using System.Windows;

namespace Myria.Wpf.View.Windows
{
    public partial class Window_InitError : Window
    {
        public string FailedStep   { get; }
        public string ErrorMessage { get; }

        public Window_InitError(string failedStep, string errorMessage)
        {
            FailedStep   = failedStep;
            ErrorMessage = errorMessage;
            InitializeComponent();
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"Step:{Environment.NewLine}{FailedStep}{Environment.NewLine}{Environment.NewLine}Error:{Environment.NewLine}{ErrorMessage}");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current?.Shutdown(-1);
        }
    }
}
