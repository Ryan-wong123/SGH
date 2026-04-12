using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ClueDef
{
    [Tooltip("Unique ID that matches PickupItem.itemName.")]
    public string id;

    [Tooltip("Locked image for this clue in the grid.")]
    public Sprite lockedSprite;

    [Tooltip("Revealed icon for this clue in the grid.")]
    public Sprite revealedSprite;

    [Tooltip("The full-size image shown when the player opens this clue.")]
    public Sprite fullImage;
}

public class ClueSystem : MonoBehaviour
{
    public static ClueSystem Instance;

    [Header("UI")]
    [SerializeField] private Transform gridParent;        // Parent with GridLayoutGroup
    [SerializeField] private GameObject clueSlotPrefab;   // Prefab containing an Image (and ideally a Button)

    [Header("All Clues (grid order)")]
    [SerializeField] private List<ClueDef> allClues = new();

    // Runtime
    private readonly Dictionary<string, Slot> slots = new(); // id -> UI
    private readonly HashSet<string> collected = new();      // collected clue ids

    [Serializable]
    private class Slot
    {
        public string id;
        public Image icon;
        public Button button;
        public Sprite lockedSprite;    // required
        public Sprite revealedSprite;  // required
        public Sprite fullImage;       // for detail view
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

            // Ensure there's an Image to show the icon
            var icon = go.GetComponentInChildren<Image>(true);
            if (icon == null)
            {
                // If the prefab itself doesn't have an Image, add one to root so it can receive raycasts.
                icon = go.AddComponent<Image>();
                icon.raycastTarget = true;
                icon.color = new Color(1f, 1f, 1f, 0f); // keep invisible if needed
            }

            // Ensure there's a Button so the tile is clickable
            var button = go.GetComponent<Button>();
            if (button == null)
            {
                button = go.AddComponent<Button>();
                // Ensure the root has an Image for raycast if icon is on a child
                var rootImg = go.GetComponent<Image>();
                if (rootImg == null) rootImg = go.AddComponent<Image>();
                rootImg.raycastTarget = true;
                if (rootImg.sprite == null) rootImg.color = new Color(1f, 1f, 1f, 0f);
            }

            // Initialize to locked image
            icon.sprite = def.lockedSprite;

            var slot = new Slot
            {
                id = def.id,
                icon = icon,
                button = button,
                lockedSprite = def.lockedSprite,
                revealedSprite = def.revealedSprite,
                fullImage = def.fullImage
            };

            // Wire up click handler
            button.onClick.RemoveAllListeners();
            var capturedId = def.id; // capture for closure
            button.onClick.AddListener(() => OnSlotClicked(capturedId));

            slots[def.id] = slot;
        }
    }

    private void OnSlotClicked(string id)
    {
        if (!collected.Contains(id))
        {
            // Optional: show "Locked" toast
            CluePopupUI.Instance?.Show("This clue is locked.");
            return;
        }

        if (!slots.TryGetValue(id, out var slot))
        {
            Debug.LogWarning($"[ClueSystem] Clicked unknown id '{id}'.");
            return;
        }

        if (slot.fullImage != null)
        {
            if (ClueUI.Instance != null)
            {
                ClueUI.Instance.ShowImage(slot.fullImage);
            }
            else
            {
                Debug.LogWarning("[ClueSystem] ClueUI not present in scene.");
            }
        }
        else
        {
            Debug.LogWarning($"[ClueSystem] No full image assigned for clue '{id}'.");
            CluePopupUI.Instance?.Show("No details available for this clue.");
        }
    }

    /// <summary>Add a clue by ID (call from PickupItem when a clue is collected).</summary>
    public bool AddClue(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        bool wasNew = collected.Add(id);
        RefreshUI();
        if (wasNew)
            ClueUI.Instance?.IncrementUnread();

        return wasNew;
    }

    /// <summary>Return whether a clue has been collected.</summary>
    public bool HasClue(string id) => collected.Contains(id);

    public bool TryShowClueImageById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (var def in allClues)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                continue;

            if (!string.Equals(def.id, id, StringComparison.OrdinalIgnoreCase))
                continue;

            if (def.fullImage == null)
            {
                Debug.LogWarning($"[ClueSystem] Clue '{id}' does not have a fullImage assigned.");
                return false;
            }

            if (ClueUI.Instance == null)
            {
                Debug.LogWarning("[ClueSystem] ClueUI not present in scene.");
                return false;
            }

            ClueUI.Instance.ShowImage(def.fullImage);
            return true;
        }

        return false;
    }

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

            if (s.button)
                s.button.interactable = true; // keep clickable; we handle locked message in OnSlotClicked
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

            // fullImage is optional, but recommended
        }
    }
#endif
}
