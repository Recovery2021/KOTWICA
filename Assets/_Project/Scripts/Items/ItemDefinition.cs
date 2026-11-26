using System;
using System.Collections.Generic;
using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Items
{
    public enum ItemCategory { Ladunek, Medykament, Narzedzie, Bron, Dokument, Inne }

    /// <summary>
    /// An item type (data asset in Resources/Items). "Legal" matters at port inspections:
    /// contraband found by customs raises the alert.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Przedmiot", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        public string id = "item";
        public string displayName = "Przedmiot";
        [TextArea] public string description;
        public ItemCategory category = ItemCategory.Inne;
        [Tooltip("Base price in credits.")]
        public int value = 10;
        public bool legal = true;
        public bool stackable = true;

        private static Dictionary<string, ItemDefinition> cache;

        public static ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (cache == null)
            {
                cache = new Dictionary<string, ItemDefinition>();
                foreach (ItemDefinition item in Resources.LoadAll<ItemDefinition>("Items")) cache[item.id] = item;
            }
            return cache.TryGetValue(itemId, out ItemDefinition found) ? found : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cache = null;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count = 1;
    }

    /// <summary>What the player carries (saved as JSON).</summary>
    [Serializable]
    public class Inventory
    {
        [SerializeField] private List<ItemStack> stacks = new List<ItemStack>();

        public IReadOnlyList<ItemStack> Stacks => stacks;

        public void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;
            ItemStack stack = stacks.Find(s => s.itemId == itemId);
            if (stack != null) stack.count += count;
            else stacks.Add(new ItemStack { itemId = itemId, count = count });
            GameEvents.RaiseItemAdded(itemId, count);
        }

        public bool Remove(string itemId, int count = 1)
        {
            ItemStack stack = stacks.Find(s => s.itemId == itemId);
            if (stack == null || stack.count < count) return false;
            stack.count -= count;
            if (stack.count == 0) stacks.Remove(stack);
            return true;
        }

        public int Count(string itemId)
        {
            ItemStack stack = stacks.Find(s => s.itemId == itemId);
            return stack != null ? stack.count : 0;
        }

        public bool Has(string itemId, int count = 1) => Count(itemId) >= count;

        /// <summary>True when carrying anything illegal (port inspections).</summary>
        public bool HasContraband()
        {
            foreach (ItemStack stack in stacks)
            {
                ItemDefinition item = ItemDefinition.Find(stack.itemId);
                if (item != null && !item.legal) return true;
            }
            return false;
        }
    }
}
