using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ClueDef
{
    [Tooltip("Unique ID that matches PickupItem.itemName.")]
    public string id;

    [Tooltip("REQUIRED: Locked image for THIS clue.")]
    public Sprite lockedSprite;

    [Tooltip("Shown after the clue is collected.")]
    public Sprite revealedSprite;
}

public class ClueSystem : MonoBehaviour
{
    public static ClueSystem Instance;

    [Header("UI")]
    [SerializeField] private Transform gridParent;        // Parent with GridLayoutGroup
    [SerializeField] private GameObject clueSlotPrefab;   // Prefab containing a single Image

    [Header("All Clues (grid order)")]
    [SerializeField] private List<ClueDef> allClues = new();

    [Header("Auto-refresh (optional)")]
    [Tooltip("If true, will poll InventorySystem periodically in case you don't call RefreshUI() manually.")]
    [SerializeField] private bool enablePolling = false;
    [SerializeField] private float pollIntervalSeconds = 0.5f;

    // Runtime
    private readonly Dictionary<string, Slot> slots = new(); // id -> UI
    private readonly HashSet<string> lastSnapshot = new();   // for polling change-detect

    [Serializable]
    private class Slot
    {
        public Image icon;
        public Sprite lockedSprite;    // required
        public Sprite revealedSprite;  // optional but recommended
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        BuildGrid();
        RefreshUI();

        if (enablePolling)
            StartCoroutine(PollInventoryLoop());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildGrid()
    {
        slots.Clear();
        foreach (Transform c in gridParent) Destroy(c.gameObject);

        foreach (var def in allClues)
        {
            if (def == null)
            {
                Debug.LogError("[ClueSystem] Null ClueDef in list.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(def.id))
            {
                Debug.LogError("[ClueSystem] ClueDef has empty id.");
                continue;
            }
            if (def.lockedSprite == null)
            {
                Debug.LogError($"[ClueSystem] Clue '{def.id}' is missing a lockedSprite. Slot will not be created.");
                continue; // strict: no fallback
            }

            var go = Instantiate(clueSlotPrefab, gridParent);

            // Expect exactly one Image somewhere in the prefab
            var icon = go.GetComponentInChildren<Image>(true);
            if (icon == null)
            {
                Debug.LogError("[ClueSystem] clueSlotPrefab must contain an Image component.");
                Destroy(go);
                continue;
            }

            // Initialize to locked image
            icon.sprite = def.lockedSprite;

            slots[def.id] = new Slot
            {
                icon = icon,
                lockedSprite = def.lockedSprite,
                revealedSprite = def.revealedSprite
            };
        }
    }

    /// <summary>
    /// Sync clue icons with InventorySystem (call after inventory updates or on demand).
    /// </summary>
    public void RefreshUI()
    {
        foreach (var kvp in slots)
        {
            string id = kvp.Key;
            var s = kvp.Value;

            bool isCollected = InventorySystem.Instance != null &&
                               InventorySystem.Instance.HasItem(id);

            if (isCollected)
            {
                if (s.icon && s.revealedSprite) s.icon.sprite = s.revealedSprite;
            }
            else
            {
                if (s.icon && s.lockedSprite) s.icon.sprite = s.lockedSprite;
            }
        }
    }

    /// <summary>
    /// Optional polling if you don't want to manually call RefreshUI().
    /// </summary>
    private IEnumerator PollInventoryLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(pollIntervalSeconds);

            if (InventorySystem.Instance == null) continue;

            if (SnapshotChanged()) RefreshUI();
        }
    }

    private bool SnapshotChanged()
    {
        var temp = new HashSet<string>();
        foreach (var item in InventorySystem.Instance.GetAllItems())
            temp.Add(item);

        if (!temp.SetEquals(lastSnapshot))
        {
            lastSnapshot.Clear();
            foreach (var v in temp) lastSnapshot.Add(v);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (allClues == null) return;
        for (int i = 0; i < allClues.Count; i++)
        {
            var def = allClues[i];
            if (def == null) continue;

            if (string.IsNullOrWhiteSpace(def.id))
                Debug.LogWarning($"[ClueSystem] Clue at index {i} has empty id.");

            if (def.lockedSprite == null)
                Debug.LogWarning($"[ClueSystem] Clue '{def?.id}' has no lockedSprite assigned (required).");
        }
    }
#endif
}
