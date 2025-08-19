using System;
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

    // Runtime
    private readonly Dictionary<string, Slot> slots = new(); // id -> UI
    private readonly HashSet<string> collected = new();      // collected clue ids

    [Serializable]
    private class Slot
    {
        public Image icon;
        public Sprite lockedSprite;    // required
        public Sprite revealedSprite;  // required
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
            if (def == null) { Debug.LogError("[ClueSystem] Null ClueDef in list."); continue; }
            if (string.IsNullOrWhiteSpace(def.id)) { Debug.LogError("[ClueSystem] ClueDef has empty id."); continue; }
            if (def.lockedSprite == null) { Debug.LogError($"[ClueSystem] '{def.id}' missing lockedSprite."); continue; }
            if (def.revealedSprite == null) { Debug.LogError($"[ClueSystem] '{def.id}' missing revealedSprite."); continue; }

            var go = Instantiate(clueSlotPrefab, gridParent);

            // Expect exactly one Image somewhere in the prefab
            var icon = go.GetComponentInChildren<Image>(true);
            if (icon == null)
            {
                Debug.LogError("[ClueSystem] clueSlotPrefab must contain an Image.");
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

    /// <summary>Add a clue by ID (call from PickupItem when a clue is collected).</summary>
    public void AddClue(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        collected.Add(id);
        RefreshUI();
    }

    /// <summary>Return whether a clue has been collected.</summary>
    public bool HasClue(string id) => collected.Contains(id);

    /// <summary>Clear all collected clues (e.g., on level reset).</summary>
    public void ClearAllClues()
    {
        collected.Clear();
        RefreshUI();
    }

    /// <summary>Update icons based on collected set.</summary>
    public void RefreshUI()
    {
        foreach (var kvp in slots)
        {
            string id = kvp.Key;
            var s = kvp.Value;
            bool isCollected = collected.Contains(id);

            if (s.icon)
                s.icon.sprite = isCollected ? s.revealedSprite : s.lockedSprite;
        }
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

            if (def.revealedSprite == null)
                Debug.LogWarning($"[ClueSystem] Clue '{def?.id}' has no revealedSprite assigned (required).");
        }
    }
#endif
}
