using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Transform, and GameObject.
using TMPro; // Imports TextMeshPro types if this UI is later switched to TMP text.
using UnityEngine.UI; // Imports Unity UI types such as Text.

public class InventoryUIManager : MonoBehaviour // Declares the component that renders inventory items in the UI.
{ // Starts the InventoryUIManager class body.
    public static InventoryUIManager Instance; // Stores the shared InventoryUIManager singleton instance.

    [Header("Inventory UI")] // Groups inventory UI references in the Unity inspector.
    [SerializeField] private Transform contentParent; // Content under Scroll View // References the scroll-view content container for item rows.
    [SerializeField] private GameObject itemPrefab; // References the prefab used to display one inventory item.

    private void Awake() // Runs before Start to initialize the singleton.
    { // Starts the Awake method body.
        if (Instance == null) // Checks whether no InventoryUIManager singleton exists yet.
            Instance = this; // Registers this component as the singleton instance.
        else // Runs when another InventoryUIManager already exists.
            Destroy(gameObject); // Destroys this duplicate inventory UI manager.
    } // Ends the Awake method body.

    public void ToggleInventory() // Refreshes inventory UI based on the content container's active state.
    { // Starts the ToggleInventory method body.
        if (!contentParent.gameObject.activeSelf) // Checks whether the inventory content is currently hidden.
        { // Starts the hidden-content block.
            ClearInventoryUI(); // Clears item rows when the inventory content is not visible.
        } // Ends the hidden-content block.
        else // Runs when the inventory content is visible.
        { // Starts the visible-content block.
            UpdateInventoryUI(); // Rebuilds the visible inventory item list.
        } // Ends the visible-content block.
    } // Ends the ToggleInventory method body.

    public void UpdateInventoryUI() // Rebuilds the visible inventory list from InventorySystem.
    { // Starts the UpdateInventoryUI method body.
        if (!contentParent.gameObject.activeSelf) // Checks whether the inventory UI is hidden.
            return; // Don't update hidden UI // Leaves early so hidden UI is not rebuilt.

        ClearInventoryUI(); // Removes old item rows before adding current inventory items.

        foreach (string itemName in InventorySystem.Instance.GetAllItems()) // Loops through every collected inventory item name.
        { // Starts the inventory item loop body.
            GameObject newItem = Instantiate(itemPrefab, contentParent); // Creates a UI row for this inventory item.
            newItem.SetActive(true); // Shows the newly created inventory item row.
            newItem.GetComponentInChildren<Text>().text = itemName; // Displays the item name in the row's text component.
        } // Ends the inventory item loop body.
    } // Ends the UpdateInventoryUI method body.

    void ClearInventoryUI() // Removes all generated inventory item rows.
    { // Starts the ClearInventoryUI method body.
        foreach (Transform child in contentParent) // Loops through every child row in the inventory content container.
        { // Starts the child cleanup loop body.
            Destroy(child.gameObject); // Destroys this generated inventory item row.
        } // Ends the child cleanup loop body.
    } // Ends the ClearInventoryUI method body.
} // Ends the InventoryUIManager class body.
