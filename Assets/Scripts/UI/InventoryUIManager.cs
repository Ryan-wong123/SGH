using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance;

    [Header("Inventory UI")]
    [SerializeField] private Transform contentParent; // Content under Scroll View
    [SerializeField] private GameObject itemPrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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

    public void UpdateInventoryUI()
    {
        if (!contentParent.gameObject.activeSelf)
            return; // Don't update hidden UI

        ClearInventoryUI();

        foreach (string itemName in InventorySystem.Instance.GetAllItems())
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
