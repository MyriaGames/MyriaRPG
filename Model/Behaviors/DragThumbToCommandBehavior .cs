using Myria.Wpf.ViewModel.UserControls;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Myria.Wpf.Model.Behaviors
{
    public class DragThumbToCommandBehavior : Behavior<Thumb>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DragThumbToCommandBehavior));

        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        // Optional: let behavior probe the host size for clamping
        public FrameworkElement? HostElement { get; set; }

        protected override void OnAttached()
        {
            AssociatedObject.DragDelta += OnDragDelta;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragDelta -= OnDragDelta;
            base.OnDetaching();
        }

        private void OnDragDelta(object s, DragDeltaEventArgs e)
        {
            if (Command == null) return;
            double? w = HostElement?.ActualWidth;
            double? h = HostElement?.ActualHeight;
            var args = new DragDeltaArgs(e.HorizontalChange, e.VerticalChange, w, h);
            if (Command.CanExecute(args)) Command.Execute(args);
        }
    }

    public class ResizeThumbToCommandBehavior : Behavior<Thumb>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ResizeThumbToCommandBehavior));

        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        protected override void OnAttached()
        {
            AssociatedObject.DragDelta += (s, e) =>
            {
                var args = new ResizeDeltaArgs(e.HorizontalChange, e.VerticalChange);
                if (Command?.CanExecute(args) == true) Command.Execute(args);
            };
            base.OnAttached();
        }

    }

}
