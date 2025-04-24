using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private Transform contentParent; // Content under Scroll View
    [SerializeField] private GameObject itemPrefab;

    public void ToggleInventory()
    {
        if (!contentParent.gameObject.activeSelf)
        {
            ClearInventoryUI();
        }
        else
        {
            UpdateInventoryUI();
        }
    }


    void UpdateInventoryUI()
    {
        ClearInventoryUI();

        foreach (string itemName in InventorySystem.GetAllItems())
        {
            GameObject newItem = Instantiate(itemPrefab, contentParent);
            newItem.SetActive(true);
            newItem.GetComponentInChildren<Text>().text = itemName;
        }
    }

    void ClearInventoryUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
}
