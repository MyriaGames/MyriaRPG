using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    /// <summary>
    /// ViewModel for the equipment slot panel (weapon, armor, accessory).
    /// Handles equip/unequip and drag-drop onto equipment slots.
    /// Subscribes to player.Inventory.ItemReceived so it stays in sync when
    /// InventoryGridViewModel makes changes to the shared player state.
    /// </summary>
    public class EquipmentViewModel : BaseViewModel
    {
        protected readonly Character _character;
        private string _equipmentTitle;
        private string _weaponHint;
        private string _armorHint;
        private string _accessoryHint;
        private EquipmentSlotViewModel _weaponSlot;
        private EquipmentSlotViewModel _armorSlot;
        private EquipmentSlotViewModel _accessorySlot;
        private ItemTooltipViewModel _currentTooltip;
        private bool _isTooltipVisible;
        private string? _notificationMessage;
        private DispatcherTimer? _notificationTimer;

        [LocalizedKey("pg.inventory.equipment.title")]
        public string EquipmentTitle { get => _equipmentTitle; set => SetProperty(ref _equipmentTitle, value); }

        [LocalizedKey("pg.inventory.slot.weapon")]
        public string WeaponHint { get => _weaponHint; set => SetProperty(ref _weaponHint, value); }

        [LocalizedKey("pg.inventory.slot.armor")]
        public string ArmorHint { get => _armorHint; set => SetProperty(ref _armorHint, value); }

        [LocalizedKey("pg.inventory.slot.accessory")]
        public string AccessoryHint { get => _accessoryHint; set => SetProperty(ref _accessoryHint, value); }

        public EquipmentSlotViewModel WeaponSlot { get => _weaponSlot; set => SetProperty(ref _weaponSlot, value); }
        public EquipmentSlotViewModel ArmorSlot { get => _armorSlot; set => SetProperty(ref _armorSlot, value); }
        public EquipmentSlotViewModel AccessorySlot { get => _accessorySlot; set => SetProperty(ref _accessorySlot, value); }

        public ItemTooltipViewModel CurrentTooltip { get => _currentTooltip; set => SetProperty(ref _currentTooltip, value); }
        public bool IsTooltipVisible { get => _isTooltipVisible; set => SetProperty(ref _isTooltipVisible, value); }

        public string? NotificationMessage
        {
            get => _notificationMessage;
            private set
            {
                SetProperty(ref _notificationMessage, value);
                OnPropertyChanged(nameof(IsNotificationVisible));
            }
        }

        public bool IsNotificationVisible => _notificationMessage != null;

        public ICommand UnequipItemCommand { get; }
        public ICommand ShowTooltipCommand { get; }
        public ICommand HideTooltipCommand { get; }
        public ICommand DismissNotificationCommand { get; }

        public EquipmentViewModel(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _weaponSlot = new EquipmentSlotViewModel(EquipmentType.Weapon);
            _armorSlot = new EquipmentSlotViewModel(EquipmentType.Armor);
            _accessorySlot = new EquipmentSlotViewModel(EquipmentType.Accessory);
            _currentTooltip = new ItemTooltipViewModel();

            UnequipItemCommand = new RelayCommand<EquipmentSlotViewModel>(UnequipItem);
            ShowTooltipCommand = new RelayCommand<InventoryItemViewModel>(ShowTooltip);
            HideTooltipCommand = new RelayCommand(HideTooltip);
            DismissNotificationCommand = new RelayCommand(() => NotificationMessage = null);

            // Stay in sync with inventory changes triggered by InventoryGridViewModel
            _character.Inventory.ItemReceived += (s, e) => RefreshEquipmentSlots();
            _character.Inventory.ItemRemoved  += (s, e) => RefreshEquipmentSlots();

            RefreshEquipmentSlots();
        }

        public void RefreshEquipmentSlots()
        {
            WeaponSlot.Item = _character.WeaponSlot;
            ArmorSlot.Item = _character.ArmorSlot;
            AccessorySlot.Item = _character.AccessorySlot;
        }

        public void HandleEquipmentDrop(InventoryItemViewModel draggedItem, string slotType)
        {
            if (draggedItem?.Item is not EquipmentItem equipment) return;
            if (!equipment.IsUsableBy(_character))
            {
                ShowNotification(Myria.Lib.Core.Systems.Localization.T("pg.inventory.wrong_class"));
                return;
            }
            ExecuteEquip(equipment);
        }

        protected virtual void ExecuteEquip(EquipmentItem equipment)
        {
            _character.Inventory.SwapEquipment(equipment.Id, _character);
            RefreshEquipmentSlots();
        }

        private void UnequipItem(EquipmentSlotViewModel slot)
        {
            if (slot?.Item == null) return;
            ExecuteUnequip(slot.Item, slot.SlotType);
        }

        protected virtual void ExecuteUnequip(EquipmentItem item, string slotType)
        {
            // Delegates to Inventory.UnequipSlot — the same method the multiplayer server now
            // calls (GameHub.UnequipItem) — instead of this ViewModel's own independent copy of
            // the same by-slot-type logic.
            _character.Inventory.UnequipSlot(slotType, _character);
            RefreshEquipmentSlots();
        }

        private void ShowTooltip(InventoryItemViewModel itemViewModel)
        {
            if (itemViewModel?.Item == null) return;
            CurrentTooltip.SetItem(itemViewModel.Item, _character);
            IsTooltipVisible = true;
        }

        private void HideTooltip() => IsTooltipVisible = false;

        protected void ShowNotification(string message)
        {
            NotificationMessage = message;
            _notificationTimer?.Stop();
            _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _notificationTimer.Tick += (s, e) =>
            {
                NotificationMessage = null;
                _notificationTimer?.Stop();
            };
            _notificationTimer.Start();
        }
    }

    /// <summary>
    /// ViewModel for a single equipment slot.
    /// </summary>
    public class EquipmentSlotViewModel : BaseViewModel
    {
        private string _slotType;
        private EquipmentItem _item;
        private Brush _borderColor;

        public string SlotType { get => _slotType; set => SetProperty(ref _slotType, value); }
        public EquipmentItem Item { get => _item; set => SetProperty(ref _item, value); }
        public Brush BorderColor { get => _borderColor; set => SetProperty(ref _borderColor, value); }

        public EquipmentSlotViewModel(string slotType)
        {
            _slotType = slotType;
            BorderColor = slotType switch
            {
                EquipmentType.Weapon    => new SolidColorBrush(Color.FromRgb(255, 183, 0)),
                EquipmentType.Armor     => new SolidColorBrush(Color.FromRgb(30, 255, 0)),
                EquipmentType.Accessory => new SolidColorBrush(Color.FromRgb(0, 112, 221)),
                _                       => new SolidColorBrush(Colors.Gray)
            };
        }
    }
}
