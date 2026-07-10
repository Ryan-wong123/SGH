using System; // Imports Serializable and StringComparison support.
using System.Collections.Generic; // Imports List, Dictionary, and HashSet collection types.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Sprite, and GameObject.
using UnityEngine.UI; // Imports Unity UI types such as Image and Button.

[Serializable] // Allows Unity to serialize clue definitions in the inspector.
public class ClueDef // Defines the data needed for one clue entry.
{ // Starts the ClueDef class body.
    [Tooltip("Unique ID that matches PickupItem.itemName.")] // Shows inspector guidance for the clue ID.
    public string id; // Stores the unique clue ID used by pickups and lookup code.

    [Tooltip("Locked image for this clue in the grid.")] // Shows inspector guidance for the locked clue icon.
    public Sprite lockedSprite; // Stores the sprite shown before the clue is collected.

    [Tooltip("Revealed icon for this clue in the grid.")] // Shows inspector guidance for the revealed clue icon.
    public Sprite revealedSprite; // Stores the sprite shown after the clue is collected.

    [Tooltip("The full-size image shown when the player opens this clue.")] // Shows inspector guidance for the clue detail image.
    public Sprite fullImage; // Stores the full-size clue image used by the detail viewer.
} // Ends the ClueDef class body.

public class ClueSystem : MonoBehaviour // Declares the component that manages clue collection and clue UI.
{ // Starts the ClueSystem class body.
    public static ClueSystem Instance; // Stores the shared ClueSystem singleton instance.

    [Header("UI")] // Groups clue UI references in the Unity inspector.
    [SerializeField] private Transform gridParent;        // Parent with GridLayoutGroup // References the grid container that holds clue slots.
    [SerializeField] private GameObject clueSlotPrefab;   // Prefab containing an Image (and ideally a Button) // References the prefab used to create each clue slot.

    [Header("All Clues (grid order)")] // Groups configured clue definitions in the Unity inspector.
    [SerializeField] private List<ClueDef> allClues = new(); // Stores every clue definition in the order shown in the grid.

    // Runtime
    private readonly Dictionary<string, Slot> slots = new(); // id -> UI // Maps clue IDs to their generated UI slot data.
    private readonly HashSet<string> collected = new();      // collected clue ids // Stores clue IDs the player has collected.

    [Serializable] // Allows Unity to serialize this nested slot type if needed.
    private class Slot // Stores generated UI references for one clue slot.
    { // Starts the Slot class body.
        public string id; // Stores the clue ID represented by this slot.
        public Image icon; // References the image component displaying the slot icon.
        public Button button; // References the button component used to click the slot.
        public Sprite lockedSprite;    // required // Stores the locked-state sprite for this slot.
        public Sprite revealedSprite;  // required // Stores the revealed-state sprite for this slot.
        public Sprite fullImage;       // for detail view // Stores the full-size image opened from this slot.
    } // Ends the Slot class body.

    private void Awake() // Runs before Start to initialize the singleton.
    { // Starts the Awake method body.
        if (Instance == null) Instance = this; // Registers this component as the singleton if none exists.
        else { Destroy(gameObject); return; } // Destroys duplicates and exits setup.
    } // Ends the Awake method body.

    private void Start() // Runs after Awake to build and refresh the clue UI.
    { // Starts the Start method body.
        BuildGrid(); // Creates clue slot UI objects from the configured clue definitions.
        RefreshUI(); // Applies locked or revealed sprites based on collected clues.
    } // Ends the Start method body.

    private void OnDestroy() // Runs when Unity destroys this clue system object.
    { // Starts the OnDestroy method body.
        if (Instance == this) Instance = null; // Clears the singleton only if this object owns it.
    } // Ends the OnDestroy method body.

    private void BuildGrid() // Rebuilds the clue grid UI from configured clue definitions.
    { // Starts the BuildGrid method body.
        slots.Clear(); // Clears the previous clue ID to slot mapping.
        foreach (Transform c in gridParent) Destroy(c.gameObject); // Removes any existing generated clue slot children.

        foreach (var def in allClues) // Loops through each configured clue definition.
        { // Starts the clue definition loop body.
            if (def == null) { Debug.LogError("[ClueSystem] Null ClueDef in list."); continue; } // Skips null clue definitions and logs the setup error.
            if (string.IsNullOrWhiteSpace(def.id)) { Debug.LogError("[ClueSystem] ClueDef has empty id."); continue; } // Skips clues that do not have a usable ID.
            if (def.lockedSprite == null) { Debug.LogError($"[ClueSystem] '{def.id}' missing lockedSprite."); continue; } // Skips clues missing the required locked sprite.
            if (def.revealedSprite == null) { Debug.LogError($"[ClueSystem] '{def.id}' missing revealedSprite."); continue; } // Skips clues missing the required revealed sprite.

            var go = Instantiate(clueSlotPrefab, gridParent); // Creates a new clue slot under the grid container.

            // Ensure there's an Image to show the icon
            var icon = go.GetComponentInChildren<Image>(true); // Finds an Image component in the slot prefab hierarchy.
            if (icon == null) // Checks whether the prefab lacks an image for the clue icon.
            { // Starts the missing-icon setup block.
                // If the prefab itself doesn't have an Image, add one to root so it can receive raycasts.
                icon = go.AddComponent<Image>(); // Adds an Image component to the slot root.
                icon.raycastTarget = true; // Allows the generated image to receive UI raycasts.
                icon.color = new Color(1f, 1f, 1f, 0f); // keep invisible if needed // Makes the generated image transparent by default.
            } // Ends the missing-icon setup block.

            // Ensure there's a Button so the tile is clickable
            var button = go.GetComponent<Button>(); // Finds a Button component on the slot root.
            if (button == null) // Checks whether the slot root lacks a Button.
            { // Starts the missing-button setup block.
                button = go.AddComponent<Button>(); // Adds a Button so the clue slot can be clicked.
                // Ensure the root has an Image for raycast if icon is on a child
                var rootImg = go.GetComponent<Image>(); // Finds an Image on the root for button raycasts.
                if (rootImg == null) rootImg = go.AddComponent<Image>(); // Adds a root Image if the Button has no raycast target.
                rootImg.raycastTarget = true; // Allows the root image to receive button clicks.
                if (rootImg.sprite == null) rootImg.color = new Color(1f, 1f, 1f, 0f); // Makes the root image transparent when it has no visible sprite.
            } // Ends the missing-button setup block.

            // Initialize to locked image
            icon.sprite = def.lockedSprite; // Shows the locked sprite before this clue is collected.

            var slot = new Slot // Creates the runtime slot data for this clue.
            { // Starts the Slot initializer.
                id = def.id, // Copies the clue ID into the slot.
                icon = icon, // Stores the icon image reference.
                button = button, // Stores the button reference.
                lockedSprite = def.lockedSprite, // Stores the locked sprite reference.
                revealedSprite = def.revealedSprite, // Stores the revealed sprite reference.
                fullImage = def.fullImage // Stores the clue detail image reference.
            }; // Ends the Slot initializer.

            // Wire up click handler
            button.onClick.RemoveAllListeners(); // Removes any old click handlers from the generated slot button.
            var capturedId = def.id; // capture for closure // Captures this clue ID for the click listener.
            button.onClick.AddListener(() => OnSlotClicked(capturedId)); // Opens or reports this clue when its slot is clicked.

            slots[def.id] = slot; // Registers this generated slot by clue ID.
        } // Ends the clue definition loop body.
    } // Ends the BuildGrid method body.

    private void OnSlotClicked(string id) // Handles clicks on clue slots.
    { // Starts the OnSlotClicked method body.
        if (!collected.Contains(id)) // Checks whether the clicked clue is still locked.
        { // Starts the locked-clue click block.
            // Optional: show "Locked" toast
            CluePopupUI.Instance?.Show("This clue is locked."); // Shows a locked message when the popup UI exists.
            return; // Leaves because locked clues cannot open details.
        } // Ends the locked-clue click block.

        if (!slots.TryGetValue(id, out var slot)) // Looks up the generated slot for the clicked clue ID.
        { // Starts the missing-slot block.
            Debug.LogWarning($"[ClueSystem] Clicked unknown id '{id}'."); // Logs that the clicked clue ID has no slot.
            return; // Leaves because there is no slot data to open.
        } // Ends the missing-slot block.

        if (slot.fullImage != null) // Checks whether this clue has a full-size detail image.
        { // Starts the full-image-present block.
            if (ClueUI.Instance != null) // Checks whether the clue detail UI exists.
            { // Starts the clue UI present block.
                ClueUI.Instance.ShowImage(slot.fullImage); // Opens the full-size clue image in the clue UI.
            } // Ends the clue UI present block.
            else // Runs when the clue detail UI is missing.
            { // Starts the clue UI missing block.
                Debug.LogWarning("[ClueSystem] ClueUI not present in scene."); // Logs that clue details cannot be shown.
            } // Ends the clue UI missing block.
        } // Ends the full-image-present block.
        else // Runs when this clue has no detail image assigned.
        { // Starts the missing-full-image block.
            Debug.LogWarning($"[ClueSystem] No full image assigned for clue '{id}'."); // Logs the missing detail image assignment.
            CluePopupUI.Instance?.Show("No details available for this clue."); // Shows a user-facing fallback message when possible.
        } // Ends the missing-full-image block.
    } // Ends the OnSlotClicked method body.

    /// <summary>Add a clue by ID (call from PickupItem when a clue is collected).</summary>
    public bool AddClue(string id) // Adds a collected clue ID and reports whether it was new.
    { // Starts the AddClue method body.
        if (string.IsNullOrWhiteSpace(id)) // Checks whether the provided clue ID is empty.
            return false; // Rejects empty clue IDs.

        bool wasNew = collected.Add(id); // Adds the clue ID and records whether it was not already collected.
        RefreshUI(); // Updates the grid to reveal newly collected clues.
        if (wasNew) // Checks whether this clue was newly collected.
            ClueUI.Instance?.IncrementUnread(); // Increments unread clue count when the clue UI exists.

        return wasNew; // Reports whether the clue was newly added.
    } // Ends the AddClue method body.

    /// <summary>Return whether a clue has been collected.</summary>
    public bool HasClue(string id) => collected.Contains(id); // Returns true when this clue ID is already collected.

    public bool TryShowClueImageById(string id) // Attempts to open a clue detail image directly by ID.
    { // Starts the TryShowClueImageById method body.
        if (string.IsNullOrWhiteSpace(id)) // Checks whether the requested clue ID is empty.
            return false; // Fails because there is no valid clue ID to show.

        foreach (var def in allClues) // Loops through configured clue definitions.
        { // Starts the clue lookup loop body.
            if (def == null || string.IsNullOrWhiteSpace(def.id)) // Skips invalid clue definitions.
                continue; // Moves to the next clue definition.

            if (!string.Equals(def.id, id, StringComparison.OrdinalIgnoreCase)) // Checks whether this definition matches the requested clue ID.
                continue; // Moves to the next clue definition when IDs do not match.

            if (def.fullImage == null) // Checks whether the matched clue has a detail image.
            { // Starts the missing-direct-image block.
                Debug.LogWarning($"[ClueSystem] Clue '{id}' does not have a fullImage assigned."); // Logs the missing detail image.
                return false; // Fails because there is no image to show.
            } // Ends the missing-direct-image block.

            if (ClueUI.Instance == null) // Checks whether the clue detail UI exists.
            { // Starts the missing-clue-ui block.
                Debug.LogWarning("[ClueSystem] ClueUI not present in scene."); // Logs that the detail UI is unavailable.
                return false; // Fails because no UI can display the image.
            } // Ends the missing-clue-ui block.

            ClueUI.Instance.ShowImage(def.fullImage); // Opens the matched clue detail image.
            return true; // Reports that the clue image was shown.
        } // Ends the clue lookup loop body.

        return false; // Reports that no matching clue ID was found.
    } // Ends the TryShowClueImageById method body.

    /// <summary>Clear all collected clues (e.g., on level reset).</summary>
    public void ClearAllClues() // Clears collected clue state and refreshes the grid.
    { // Starts the ClearAllClues method body.
        collected.Clear(); // Removes every collected clue ID.
        RefreshUI(); // Returns the clue grid to locked visual state.
    } // Ends the ClearAllClues method body.

    /// <summary>Update icons based on collected set.</summary>
    public void RefreshUI() // Updates every clue slot to match collected state.
    { // Starts the RefreshUI method body.
        foreach (var kvp in slots) // Loops through each generated clue slot.
        { // Starts the slot refresh loop body.
            string id = kvp.Key; // Reads the clue ID for this slot.
            var s = kvp.Value; // Reads the slot UI references for this clue.
            bool isCollected = collected.Contains(id); // Checks whether this clue has been collected.

            if (s.icon) // Checks whether the slot has an icon image reference.
                s.icon.sprite = isCollected ? s.revealedSprite : s.lockedSprite; // Shows revealed or locked art based on collection state.

            if (s.button) // Checks whether the slot has a button reference.
                s.button.interactable = true; // keep clickable; we handle locked message in OnSlotClicked // Keeps locked clues clickable so they can show the locked message.
        } // Ends the slot refresh loop body.
    } // Ends the RefreshUI method body.

#if UNITY_EDITOR // Compiles editor-only validation code inside the Unity Editor.
    private void OnValidate() // Runs in the editor when serialized values change.
    { // Starts the OnValidate method body.
        if (allClues == null) return; // Leaves when the clue list is not initialized.
        for (int i = 0; i < allClues.Count; i++) // Loops through each configured clue definition by index.
        { // Starts the editor validation loop body.
            var def = allClues[i]; // Reads the clue definition at this index.
            if (def == null) continue; // Skips null clue definitions.

            if (string.IsNullOrWhiteSpace(def.id)) // Checks whether this clue definition has an empty ID.
                Debug.LogWarning($"[ClueSystem] Clue at index {i} has empty id."); // Warns about an empty clue ID.

            if (def.lockedSprite == null) // Checks whether this clue is missing required locked art.
                Debug.LogWarning($"[ClueSystem] Clue '{def?.id}' has no lockedSprite assigned (required)."); // Warns about missing locked art.

            if (def.revealedSprite == null) // Checks whether this clue is missing required revealed art.
                Debug.LogWarning($"[ClueSystem] Clue '{def?.id}' has no revealedSprite assigned (required)."); // Warns about missing revealed art.

            // fullImage is optional, but recommended
        } // Ends the editor validation loop body.
    } // Ends the OnValidate method body.
#endif // Ends the editor-only validation section.
} // Ends the ClueSystem class body.
