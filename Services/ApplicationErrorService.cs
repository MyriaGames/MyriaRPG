using System.Windows;
using System.Windows.Threading;
using Myria.Wpf.View.Windows;

namespace Myria.Wpf.Services
{
    public static class ApplicationErrorService
    {
        private static bool _initializationComplete;
        private static bool _showingError;

        public static bool InitializationComplete => _initializationComplete;

        public static void MarkInitializationComplete() => _initializationComplete = true;

        public static void ShowInitializationError(string step, Exception exception)
            => ShowInitializationError(step, FormatException(exception));

        public static void ShowInitializationError(string step, string message)
        {
            try
            {
                new Window_InitError(step, message).ShowDialog();
            }
            catch (Exception fallbackException)
            {
                MessageBox.Show(
                    $"{message}{Environment.NewLine}{Environment.NewLine}The initialization error window also failed:{Environment.NewLine}{fallbackException}",
                    $"Myria.Wpf initialization failed: {step}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public static void ShowRuntimeError(string step, Exception exception)
            => ShowRuntimeError(step, FormatException(exception));

        public static void ShowRuntimeError(string step, string message)
        {
            if (_showingError) return;
            _showingError = true;

            void ShowWindow()
            {
                try
                {
                    var win = new Window_AppError(step, message);
                    // Only assign Owner when MainWindow is actually loaded — assigning an
                    // uninitialised or closed window as Owner causes a WPF exception that
                    // would send us into the catch and hide the real error in a MessageBox.
                    var main = Application.Current?.MainWindow;
                    if (main is { IsLoaded: true })
                    {
                        win.Owner = main;
                        win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    }
                    win.ShowDialog();
                }
                catch (Exception displayEx)
                {
                    // Last-resort fallback: at least let the user see and copy the original message.
                    MessageBox.Show(
                        $"{message}{Environment.NewLine}{Environment.NewLine}" +
                        $"(Error window also failed: {displayEx.Message})",
                        $"Myria.Wpf error: {step}",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    _showingError = false;
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                // Already on the UI thread (e.g. called from DispatcherUnhandledException) —
                // show synchronously so we don't rely on BeginInvoke timing.
                ShowWindow();
            }
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)ShowWindow);
            }
        }

        public static void ShowUnhandledError(string step, Exception exception)
        {
            void Show()
            {
                if (_initializationComplete)
                    ShowRuntimeError(step, exception);
                else
                    ShowInitializationError(step, exception);
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                Show();
                return;
            }

            dispatcher.Invoke(Show, DispatcherPriority.Send);
        }

        public static string FormatException(Exception exception) => exception.ToString();
    }
}
