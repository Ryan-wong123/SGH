using System.Collections.Generic;
using UnityEngine;

public static class InventorySystem
{
    private static HashSet<string> items = new HashSet<string>();

    public static void AddItem(string itemName)
    {
        items.Add(itemName);
        UnityEngine.Debug.Log("Picked up: " + itemName);
    }

    public static bool HasItem(string itemName)
    {
        return items.Contains(itemName);
    }

    public static IEnumerable<string> GetAllItems()
    {
        return items;
    }
}