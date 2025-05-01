using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    private HashSet<string> items = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddItem(string itemName)
    {
        Debug.Log($"[InventorySystem] Add: {itemName}");
        items.Add(itemName);
    }


    public bool HasItem(string itemName)
    {
        return items.Contains(itemName);
    }

    public IEnumerable<string> GetAllItems()
    {
        return items;
    }
}
