using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.UserControls
{
    public class ViewModel_SkillDetailWindow : BaseViewModel
    {
        private readonly Action<Thickness> _setMarginAction;
        private double _relLeft = 0.3;
        private double _relTop  = 0.2;

        private string _title = "";
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

        private double _left;
        private double _top;
        private double _width  = 580;
        private double _height = 460;
        private int    _zIndex;

        public double Left   { get => _left;   set { _left   = value; OnPropertyChanged(); } }
        public double Top    { get => _top;    set { _top    = value; OnPropertyChanged(); } }
        public double Width  { get => _width;  set { _width  = value; OnPropertyChanged(); } }
        public double Height { get => _height; set { _height = value; OnPropertyChanged(); } }
        public int    ZIndex { get => _zIndex; set { _zIndex = value; OnPropertyChanged(); } }

        public ICommand CloseCommand      { get; }
        public ICommand FocusCommand      { get; }
        public ICommand DragDeltaCommand  { get; }
        public ICommand ResizeDeltaCommand{ get; }

        public ViewModel_SkillDetailWindow()
        {
            _setMarginAction = m => MainWindow.Instance.skillDetailWindow.Margin = m;

            CloseCommand       = new RelayCommand(Close);
            FocusCommand       = new RelayCommand(BringToFront);
            DragDeltaCommand   = new RelayCommand<DragDeltaArgs>(OnDragDelta);
            ResizeDeltaCommand = new RelayCommand<ResizeDeltaArgs>(OnResizeDelta);

            var host = MainWindow.Instance.WindowGrid;
            if (host.ActualWidth > 0)
                ApplyRelativePosition(host.ActualWidth, host.ActualHeight);
            else
                host.Loaded += (_, _) => ApplyRelativePosition(host.ActualWidth, host.ActualHeight);

            host.SizeChanged += (_, e) => ApplyRelativePosition(e.NewSize.Width, e.NewSize.Height);
        }

        public void SetTitle(string title) => Title = title;

        private void BringToFront() => ZIndex = WindowManager.NextZIndex();

        private void Close() => MainWindow.Instance.skillDetailWindow.Visibility = Visibility.Hidden;

        private void ApplyRelativePosition(double hostW, double hostH)
        {
            if (hostW <= 0 || hostH <= 0) return;
            Left = _relLeft * hostW;
            Top  = _relTop  * hostH;
            ClampAndSync();
        }

        private void ClampAndSync()
        {
            var host = MainWindow.Instance.WindowGrid;
            if (Left < -40) Left = -40;
            if (Top  <   0) Top  =   0;
            if (Left > host.ActualWidth  - 20) Left = host.ActualWidth  - 20;
            if (Top  > host.ActualHeight - 20) Top  = host.ActualHeight - 20;
            _setMarginAction(new Thickness(Left, Top, 0, 0));
        }

        private void OnDragDelta(DragDeltaArgs a)
        {
            Left += a.HorizontalChange;
            Top  += a.VerticalChange;
            ClampAndSync();
        }

        private void OnResizeDelta(ResizeDeltaArgs a)
        {
            Width  = Math.Max(Width  + a.HorizontalChange, 200);
            Height = Math.Max(Height + a.VerticalChange,   120);
        }
    }
}
