using System;
using System.Collections.Generic;
using System.Linq;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;

namespace Myria.Wpf.Utils
{
    /// <summary>
    /// Utility class for inventory management helpers
    /// </summary>
    public static class InventoryHelpers
    {
        /// <summary>
        /// Sorts inventory items by specified criteria
        /// </summary>
        public enum SortType
        {
            Default,
            ByRarity,
            ByType,
            ByName,
            ByValue
        }

        /// <summary>
        /// Filters inventory items
        /// </summary>
        public class InventoryFilter
        {
            public string? RarityFilter { get; set; }
            public Type ItemTypeFilter { get; set; }
            public string NameFilter { get; set; }

            public bool Matches(Item item)
            {
                if (RarityFilter != null && item.Rarity != RarityFilter)
                    return false;

                if (ItemTypeFilter != null && !ItemTypeFilter.IsInstanceOfType(item))
                    return false;

                if (!string.IsNullOrEmpty(NameFilter) && 
                    !item.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Sorts a list of items by the specified type
        /// </summary>
        public static IEnumerable<Item> Sort(IEnumerable<Item> items, SortType sortType)
        {
            return sortType switch
            {
                // ItemRarity is now a plain string (see Myria.Lib.Core.Systems.Enums.ItemRarity) — sort by
                // ItemRarityRegistry.GetOrder, not the raw string, since string ordering is
                // alphabetical and wouldn't reproduce the old enum ordinal's "rarest first" order.
                SortType.ByRarity => items.OrderByDescending(i => ItemRarityRegistry.GetOrder(i.Rarity)),
                SortType.ByType => items.OrderBy(i => i.GetType().Name),
                SortType.ByName => items.OrderBy(i => i.Name),
                SortType.ByValue => items.OrderByDescending(i => i.SellValue),
                _ => items
            };
        }

        /// <summary>
        /// Filters items by the specified criteria
        /// </summary>
        public static IEnumerable<Item> Filter(IEnumerable<Item> items, InventoryFilter filter)
        {
            return items.Where(i => filter.Matches(i));
        }

        /// <summary>
        /// Gets total value of items in inventory
        /// </summary>
        public static int GetTotalValue(IEnumerable<Item> items)
        {
            return items.Sum(i => i.SellValue * i.StackSize);
        }

        /// <summary>
        /// Gets all unique item IDs with their counts
        /// </summary>
        public static Dictionary<string, int> GetItemCounts(IEnumerable<Item> items)
        {
            var counts = new Dictionary<string, int>();
            foreach (var item in items)
            {
                if (counts.ContainsKey(item.Id))
                    counts[item.Id] += item.StackSize;
                else
                    counts[item.Id] = item.StackSize;
            }
            return counts;
        }

        /// <summary>
        /// Finds first item matching the filter
        /// </summary>
        public static Item FindItem(IEnumerable<Item> items, InventoryFilter filter)
        {
            return items.FirstOrDefault(i => filter.Matches(i));
        }

        /// <summary>
        /// Checks if inventory has required items for a quest or craft
        /// </summary>
        public static bool HasRequiredItems(IEnumerable<Item> items, Dictionary<string, int> requirements)
        {
            var counts = GetItemCounts(items);

            foreach (var req in requirements)
            {
                if (!counts.ContainsKey(req.Key) || counts[req.Key] < req.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets space available in inventory
        /// </summary>
        public static int GetAvailableSpace(int capacity, IEnumerable<Item> items)
        {
            return Math.Max(0, capacity - items.Count());
        }

        /// <summary>
        /// Gets estimated weight of inventory (custom implementation)
        /// </summary>
        public static double GetTotalWeight(IEnumerable<Item> items, Dictionary<string, double> itemWeights = null)
        {
            double weight = 0;
            foreach (var item in items)
            {
                if (itemWeights != null && itemWeights.TryGetValue(item.Id, out double itemWeight))
                    weight += itemWeight * item.StackSize;
                else
                    weight += 0.5 * item.StackSize; // Default weight
            }
            return weight;
        }

        /// <summary>
        /// Gets rarity color in hex format
        /// </summary>
        public static string GetRarityColor(string rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "#A0A0A0",
                ItemRarity.Uncommon => "#1EFF00",
                ItemRarity.Rare => "#0070DD",
                ItemRarity.Epic => "#A335EE",
                ItemRarity.Unique => "#FFD700",
                ItemRarity.Legendary => "#FF8000",
                ItemRarity.Godly => "#FF0000",
                _ => "#A0A0A0"
            };
        }

        /// <summary>
        /// Gets rarity display name
        /// </summary>
        public static string GetRarityName(string rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "Common",
                ItemRarity.Uncommon => "Uncommon",
                ItemRarity.Rare => "Rare",
                ItemRarity.Epic => "Epic",
                ItemRarity.Unique => "Unique",
                ItemRarity.Legendary => "Legendary",
                ItemRarity.Godly => "Godly",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets item type display name
        /// </summary>
        public static string GetItemTypeName(Item item)
        {
            return item switch
            {
                EquipmentItem eq => $"{eq.SlotType} Equipment",
                ConsumableItem => "Consumable",
                MaterialItem => "Material",
                _ => "Item"
            };
        }

        /// <summary>
        /// Formats item description with color codes for UI display
        /// </summary>
        public static string FormatItemDescription(Item item)
        {
            var description = new System.Text.StringBuilder();

            description.AppendLine($"{item.Name}");
            description.AppendLine($"Type: {GetItemTypeName(item)}");
            description.AppendLine($"Rarity: {GetRarityName(item.Rarity)}");
            description.AppendLine($"Sell Value: {item.SellValue} Gold");

            if (item is EquipmentItem equip && equip.UpgradeLevel > 0)
            {
                description.AppendLine($"Upgrade Level: +{equip.UpgradeLevel}");
            }

            if (!string.IsNullOrEmpty(item.Description))
            {
                description.AppendLine();
                description.AppendLine(Localization.T(item.Description));
            }

            return description.ToString();
        }

        /// <summary>
        /// Gets equipment bonus summary as formatted string
        /// </summary>
        public static string GetEquipmentBonuses(EquipmentItem equip)
        {
            var bonuses = new System.Text.StringBuilder();

            if (equip.BonusATK > 0) bonuses.AppendLine($"+{equip.BonusATK} ATK");
            if (equip.BonusDEF > 0) bonuses.AppendLine($"+{equip.BonusDEF} DEF");
            if (equip.BonusMATK > 0) bonuses.AppendLine($"+{equip.BonusMATK} M.ATK");
            if (equip.BonusMDEF > 0) bonuses.AppendLine($"+{equip.BonusMDEF} M.DEF");

            if (equip.BonusSTR > 0) bonuses.AppendLine($"+{equip.BonusSTR} STR");
            if (equip.BonusDEX > 0) bonuses.AppendLine($"+{equip.BonusDEX} DEX");
            if (equip.BonusEND > 0) bonuses.AppendLine($"+{equip.BonusEND} END");
            if (equip.BonusINT > 0) bonuses.AppendLine($"+{equip.BonusINT} INT");
            if (equip.BonusSPR > 0) bonuses.AppendLine($"+{equip.BonusSPR} SPR");

            if (equip.BonusHP > 0) bonuses.AppendLine($"+{equip.BonusHP} HP");
            if (equip.BonusMP > 0) bonuses.AppendLine($"+{equip.BonusMP} MP");

            if (equip.BonusAim > 0) bonuses.AppendLine($"+{equip.BonusAim}% Aim");
            if (equip.BonusEvasion > 0) bonuses.AppendLine($"+{equip.BonusEvasion}% Evasion");
            if (equip.BonusCrit > 0) bonuses.AppendLine($"+{equip.BonusCrit}% Crit");
            if (equip.BonusBlock > 0) bonuses.AppendLine($"+{equip.BonusBlock}% Block");

            return bonuses.Length > 0 ? bonuses.ToString() : "No bonuses";
        }
    }

    /// <summary>
    /// Extension methods for inventory operations
    /// </summary>
    public static class InventoryExtensions
    {
        /// <summary>
        /// Gets item by ID from inventory
        /// </summary>
        public static Item GetItemById(this List<Item> items, string itemId)
        {
            return items.FirstOrDefault(i => i.Id == itemId);
        }

        /// <summary>
        /// Checks if inventory contains item
        /// </summary>
        public static bool ContainsItem(this List<Item> items, string itemId, int requiredCount = 1)
        {
            var item = items.GetItemById(itemId);
            return item != null && item.StackSize >= requiredCount;
        }

        /// <summary>
        /// Gets total count of specific item
        /// </summary>
        public static int GetItemCount(this List<Item> items, string itemId)
        {
            return items.GetItemById(itemId)?.StackSize ?? 0;
        }

        /// <summary>
        /// Gets all equipment items
        /// </summary>
        public static IEnumerable<EquipmentItem> GetEquipmentItems(this List<Item> items)
        {
            return items.OfType<EquipmentItem>();
        }

        /// <summary>
        /// Gets all consumable items
        /// </summary>
        public static IEnumerable<ConsumableItem> GetConsumableItems(this List<Item> items)
        {
            return items.OfType<ConsumableItem>();
        }

        /// <summary>
        /// Gets all material items
        /// </summary>
        public static IEnumerable<MaterialItem> GetMaterialItems(this List<Item> items)
        {
            return items.OfType<MaterialItem>();
        }

        /// <summary>
        /// Gets items of specific rarity
        /// </summary>
        public static IEnumerable<Item> GetByRarity(this List<Item> items, string rarity)
        {
            return items.Where(i => i.Rarity == rarity);
        }

        /// <summary>
        /// Gets items usable by a specific class
        /// </summary>
        public static IEnumerable<Item> GetForClass(this List<Item> items, string playerClass)
        {
            return items.Where(i => i.AllowedClasses.Count == 0
                || i.AllowedClasses.Any(c => c.Equals(playerClass, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
