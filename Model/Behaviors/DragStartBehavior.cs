// Myria.Wpf.Model.Behaviors (or wherever you prefer)
// Requires: Microsoft.Xaml.Behaviors.Wpf
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Myria.Wpf.Model.Behaviors
{
    public class DragStartBehavior : Behavior<Border>
    {
        public static readonly DependencyProperty DragCommandProperty =
            DependencyProperty.Register(nameof(DragCommand), typeof(ICommand), typeof(DragStartBehavior));
        public ICommand DragCommand { get => (ICommand)GetValue(DragCommandProperty); set => SetValue(DragCommandProperty, value); }

        // Shared data format to avoid type-name issues in XAML
        public const string DataFormat = "Myria.Wpf.InventoryItem";

        private Point _down;
        protected override void OnAttached()
        {
            AssociatedObject.MouseLeftButtonDown += OnDown;
            AssociatedObject.MouseMove += OnMove;
        }
        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= OnDown;
            AssociatedObject.MouseMove -= OnMove;
        }
        private void OnDown(object s, MouseButtonEventArgs e) => _down = e.GetPosition(null);

        private void OnMove(object s, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _down.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(pos.Y - _down.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var payload = AssociatedObject.DataContext; // your ItemVm
                var data = new DataObject(DataFormat, payload);
                DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
                DragCommand?.Execute(payload);
            }
        }
    }

    public class DropToCommandBehavior : Behavior<Border>
    {
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DropToCommandBehavior));
        public ICommand DropCommand { get => (ICommand)GetValue(DropCommandProperty); set => SetValue(DropCommandProperty, value); }

        public static readonly DependencyProperty SlotProperty =
            DependencyProperty.Register(nameof(Slot), typeof(string), typeof(DropToCommandBehavior));
        public string Slot { get => (string)GetValue(SlotProperty); set => SetValue(SlotProperty, value); }

        protected override void OnAttached()
        {
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.Drop += OnDrop;
        }
        protected override void OnDetaching()
        {
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
        }
        private void OnDragOver(object s, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DragStartBehavior.DataFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
        private void OnDrop(object s, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DragStartBehavior.DataFormat)) return;
            var item = e.Data.GetData(DragStartBehavior.DataFormat); // your ItemVm instance
            DropCommand?.Execute(new EquipDropArgs(Slot, item));
            e.Handled = true;
        }
    }

    // DTO lives here or in your ViewModels namespace
    public record EquipDropArgs(string Slot, object Item);
}
