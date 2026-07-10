using System.Collections.Generic; // Imports HashSet and IEnumerable collection types.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour and GameObject.

public class InventorySystem : MonoBehaviour // Declares the component that stores collected inventory item names.
{ // Starts the InventorySystem class body.
    public static InventorySystem Instance; // Stores the shared InventorySystem singleton instance.

    private HashSet<string> items = new HashSet<string>(); // Stores unique collected item names.

    private void Awake() // Runs before Start to initialize the singleton.
    { // Starts the Awake method body.
        if (Instance == null) // Checks whether no InventorySystem singleton exists yet.
            Instance = this; // Registers this component as the singleton instance.
        else // Runs when another InventorySystem already exists.
            Destroy(gameObject); // Destroys this duplicate inventory object.
    } // Ends the Awake method body.

    public void AddItem(string itemName) // Adds an item name to the inventory.
    { // Starts the AddItem method body.
        Debug.Log($"[InventorySystem] Add: {itemName}"); // Logs the item being added for debugging.
        items.Add(itemName); // Stores the item name if it is not already present.
    } // Ends the AddItem method body.


    public bool HasItem(string itemName) // Checks whether the inventory contains an item.
    { // Starts the HasItem method body.
        return items.Contains(itemName); // Returns true when the item name exists in the inventory set.
    } // Ends the HasItem method body.

    public IEnumerable<string> GetAllItems() // Returns all collected item names for UI or gameplay code.
    { // Starts the GetAllItems method body.
        return items; // Exposes the current inventory item collection to the caller.
    } // Ends the GetAllItems method body.
} // Ends the InventorySystem class body.
