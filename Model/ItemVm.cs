using System.Windows.Media;
using Myria.Wpf.ViewModel;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Entities.Items;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Localization = Myria.Lib.Core.Systems.Localization;

namespace Myria.Wpf.Model
{
    public class ItemVm : BaseViewModel
    {
        public ItemVm() { }
        public ItemVm(Item item) 
        {
            Id = item.Id;
            Name = Localization.T(item.Name);
            if (item is EquipmentItem eq)
                Name += $" {eq.UpgradeLevel}";
            Type = item.Name;
            Rarity = item.Rarity;
            IsEquipable = item is EquipmentItem;
            Quantity = item.StackSize; // Initialize quantity from item
            
            LoadIcon(item.Id);
        }
        public ItemVm(string name, string type, string rarity)
        { Name = name; Type = type; Rarity = rarity; IsEquipable = type is "Weapon" or "Armor" or "Accessory"; LoadIcon(name); }
        public ItemVm(string name, string type, string rarity, SolidColorBrush color)
        { Name = name; Type = type; Rarity = rarity; IsEquipable = type is "Weapon" or "Armor" or "Accessory"; Color = color; LoadIcon(name); }
        
        private string _id;
        public string Id { get { return _id; } set { _id = value; } }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public SolidColorBrush Color { get; set; }

        public override string ToString()
        {
            return Localization.T(Name);
        }


        public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
        private string _icon = "📦";
        public int Quantity { get => _qty; set { _qty = value; OnPropertyChanged(); } }
        private int _qty = 1;
        public string Description { get => _desc; set { _desc = value; OnPropertyChanged(); } }
        private string _desc = string.Empty;
        public string StatsLine { get => _stats; set { _stats = value; OnPropertyChanged(); } }
        private string _stats = string.Empty;


        public bool IsUsable { get => _usable; set { _usable = value; OnPropertyChanged(); } }
        private bool _usable;
        public bool IsEquipable { get => _equip; private set { _equip = value; OnPropertyChanged(); } }
        private bool _equip;
        public bool IsEquipped { get => _equipped; set { _equipped = value; OnPropertyChanged(); } }
        private bool _equipped;

        public ImageSource IconSource { get; set; }

        private void LoadIcon(string id)
        {
            // Icon loading logic is handled by XAML resources now
        }

        public static ItemVm CreateEmptySlot()
        {
            return new ItemVm
            {
                Id = "empty_slot",
                Name = "",
                Icon = "",
                Quantity = 0,
                Color = Brushes.Transparent // Or a subtle border color for empty slots
            };
        }

        public bool IsEmpty => Id == "empty_slot";
        public Visibility IsEmptyVisibility => IsEmpty ? Visibility.Visible : Visibility.Hidden;
        public bool ShowQuantity => !IsEmpty && Quantity > 1;
        public Visibility ShowQuantityVisibility => ShowQuantity ? Visibility.Visible : Visibility.Hidden;
    }
}
