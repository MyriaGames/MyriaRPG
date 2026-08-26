using Myria.Wpf.Services;
using Myria.Wpf.Services.Rune;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Diagnostics;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_RuneDrawing : Page
    {
        public Page_RuneDrawing()
        {
            InitializeComponent();
            DataContext = GameHubService.IsConnected
                ? (RuneDrawingViewModel)new MultiplayerRuneDrawingViewModel()
                : new RuneDrawingViewModel();
        }

        private RuneDrawingViewModel Vm => (RuneDrawingViewModel)DataContext;

        private void BtnConfirmLetter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Vm.ConfirmLetter(DrawingCanvas.Strokes);
            DrawingCanvas.Strokes.Clear();
        }

        private void BtnClearCanvas_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DrawingCanvas.Strokes.Clear();
        }

        private void BtnSubmitWord_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Vm.SubmitWord();
        }

        /// <summary>
        /// Debug helper — call from the immediate window or a temporary debug button.
        /// Logs all Jaccard scores to the Output / Debug window so the threshold can be tuned.
        /// </summary>
        public void DebugPrintScores()
        {
            var recognizer = Vm.GetRecognizer();
            var scores = recognizer.DebugScores(DrawingCanvas.Strokes);
            Debug.WriteLine("=== Recognition scores ===");
            foreach (var (letter, score) in scores)
                Debug.WriteLine($"  {letter,-6} {score:P1}");
        }
    }
}
