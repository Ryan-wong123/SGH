using System.Collections.Generic; // Imports a namespace used by this script.
using System; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

[RequireComponent(typeof(PhotonView))] // Adds an attribute that changes how Unity or C# treats the next declaration.
[RequireComponent(typeof(Button))] // Adds an attribute that changes how Unity or C# treats the next declaration.
[RequireComponent(typeof(Image))] // Adds an attribute that changes how Unity or C# treats the next declaration.
public class PhotonSyncedUIButtonColor : MonoBehaviourPun // Declares this type.
{ // Starts this code block.
    private const string SelectedPropertyPrefix = "UIButtonSelected_"; // Declares a field, property, or member used by this script.
    private const string ResultStatePropertyPrefix = "UIButtonResultState_"; // Declares a field, property, or member used by this script.

    public event Action<PhotonSyncedUIButtonColor> StateChanged; // Declares a field, property, or member used by this script.

    public enum VisualState // Declares this type.
    { // Starts this code block.
        Normal = 0, // Updates this value.
        Selected = 1, // Updates this value.
        Correct = 2, // Updates this value.
        Wrong = 3 // Updates this value.
    } // Ends this code block.

    [Header("Group (kept for compatibility, NOT used in multi-select)")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool autoGroupByParent = true; // Exposes this private field in the Unity Inspector.
    [SerializeField] private string groupId = ""; // Exposes this private field in the Unity Inspector.

    [Header("Colors")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Color normalColor = Color.white; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color selectedColor = Color.green; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color correctColor = new Color(0.2f, 0.9f, 0.2f, 1f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color wrongColor = new Color(0.95f, 0.2f, 0.2f, 1f); // Exposes this private field in the Unity Inspector.

    [Header("Behavior")] // Adds a labeled section in the Unity Inspector.
    [Tooltip("Clicking selected button again will deselect (multi-select toggle)")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private bool allowToggleOff = true; // Exposes this private field in the Unity Inspector.

    private Button button; // Declares a field, property, or member used by this script.
    private Image image; // Declares a field, property, or member used by this script.

    // RESULT state lives per-button (synced)
    [SerializeField] private VisualState resultState = VisualState.Normal; // Exposes this private field in the Unity Inspector.

    // MULTI-SELECT state lives per-button (synced)
    [SerializeField] private bool isSelected = false; // Exposes this private field in the Unity Inspector.

    public bool IsSelected => isSelected; // Declares a field, property, or member used by this script.
    public VisualState CurrentResultState => resultState; // Declares a field, property, or member used by this script.

    // GROUP selection state (kept for compatibility, not used for multi-select)
    private static readonly Dictionary<string, List<PhotonSyncedUIButtonColor>> Groups = new(); // Declares a field, property, or member used by this script.
    private static readonly Dictionary<string, int> SelectedViewId = new(); // -1 = none

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        button = GetComponent<Button>(); // Updates this value.
        image = GetComponent<Image>(); // Updates this value.

        // kept (doesn't harm) – but groupId won't control selection anymore
        if (autoGroupByParent && string.IsNullOrEmpty(groupId)) // Checks this condition before continuing.
            groupId = BuildStableGroupId(); // Updates this value.

        button.onClick.AddListener(OnLocalClick); // Runs this line of puzzle logic.

        // kept to not break any existing expectations, but NOT used for selection enforcement
        Register(); // Runs this line of puzzle logic.

        ApplyVisual(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        ApplyPersistedRoomState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDestroy() // Declares a method used by this script.
    { // Starts this code block.
        button.onClick.RemoveListener(OnLocalClick); // Runs this line of puzzle logic.
        Unregister(); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ─────────────────────────────
    // CLICK (MULTI-SELECT TOGGLE)
    // ─────────────────────────────
    private void OnLocalClick() // Declares a method used by this script.
    { // Starts this code block.
        // Multi-select behavior: toggle THIS button only.
        // Correct/Wrong overrides visuals but selection can still be toggled underneath (kept).
        bool nextSelected = isSelected; // Creates a local value used by the following logic.

        if (allowToggleOff && isSelected) // Checks this condition before continuing.
            nextSelected = false; // Updates this value.
        else // Runs when the previous condition was false.
            nextSelected = true; // Updates this value.

        GameSessionCsvLogger.Instance?.RecordLocalPlayerInput(gameObject.name, nextSelected ? "selected" : "deselected");

        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
        { // Starts this code block.
            isSelected = nextSelected; // Updates this value.
            PersistState(); // Runs this line of puzzle logic.
            NotifyStateChanged(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Sync only THIS button selection state
        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, nextSelected); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetSelected(bool selected) // Declares a method used by this script.
    { // Starts this code block.
        isSelected = selected; // Updates this value.

        // If you are using resultState.Selected to represent selection,
        // keep it consistent (so older code that checks resultState still works).
        if (!isSelected && (resultState == VisualState.Selected || resultState == VisualState.Wrong)) // Checks this condition before continuing.
            resultState = VisualState.Normal; // Updates this value.

        PersistState(); // Runs this line of puzzle logic.
        NotifyStateChanged(); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ─────────────────────────────
    // RESULT STATE (Correct/Wrong/Normal)
    // ─────────────────────────────
    public void MarkCorrectAndSync() // Declares a method used by this script.
    { // Starts this code block.
        SetResultAndSync(VisualState.Correct); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void MarkWrongAndSync() // Declares a method used by this script.
    { // Starts this code block.
        SetResultAndSync(VisualState.Wrong); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ClearResultAndSync() // Declares a method used by this script.
    { // Starts this code block.
        SetResultAndSync(VisualState.Normal); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ResetStateAndSync() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
        { // Starts this code block.
            isSelected = false; // Updates this value.
            resultState = VisualState.Normal; // Updates this value.
            PersistState(); // Runs this line of puzzle logic.
            NotifyStateChanged(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        photonView.RPC(nameof(RPC_SetState), RpcTarget.AllViaServer, false, (int)VisualState.Normal); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void MarkSelectedAndSync() // Declares a method used by this script.
    { // Starts this code block.
        // In multi-select, "selected" is a separate flag.
        // Keep resultState as Normal (or Selected if you prefer), but selection visual is driven by isSelected.
        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
        { // Starts this code block.
            isSelected = true; // Updates this value.
            PersistState(); // Runs this line of puzzle logic.
            NotifyStateChanged(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, true); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ClearSelectedAndSync() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
        { // Starts this code block.
            isSelected = false; // Updates this value.
            PersistState(); // Runs this line of puzzle logic.
            NotifyStateChanged(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, false); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void SetResultAndSync(VisualState newState) // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
        { // Starts this code block.
            resultState = newState; // Updates this value.

            // if setting to Normal, keep selection highlight if selected
            PersistState(); // Runs this line of puzzle logic.
            NotifyStateChanged(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        photonView.RPC(nameof(RPC_SetResultState), RpcTarget.AllViaServer, (int)newState); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetResultState(int state) // Declares a method used by this script.
    { // Starts this code block.
        resultState = (VisualState)state; // Updates this value.

        // If external scripts used "Selected" resultState previously, map it to isSelected
        if (resultState == VisualState.Selected) // Checks this condition before continuing.
            isSelected = true; // Updates this value.

        PersistState(); // Runs this line of puzzle logic.
        NotifyStateChanged(); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetState(bool selected, int state) // Declares a method used by this script.
    { // Starts this code block.
        isSelected = selected; // Updates this value.
        resultState = (VisualState)state; // Updates this value.
        PersistState(); // Runs this line of puzzle logic.
        NotifyStateChanged(); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ─────────────────────────────
    // VISUAL RESOLUTION
    // Priority: Correct/Wrong overrides Selected/Normal
    // In multi-select: selection comes from `isSelected` (per-button)
    // ─────────────────────────────
    private void ApplyVisual() // Declares a method used by this script.
    { // Starts this code block.
        VisualState finalState; // Runs this line of puzzle logic.

        if (resultState == VisualState.Correct || resultState == VisualState.Wrong) // Checks this condition before continuing.
        { // Starts this code block.
            finalState = resultState; // Updates this value.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            finalState = isSelected ? VisualState.Selected : VisualState.Normal; // Updates this value.
        } // Ends this code block.

        SetVisual(finalState); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void SetVisual(VisualState state) // Declares a method used by this script.
    { // Starts this code block.
        switch (state) // Chooses behavior based on this value.
        { // Starts this code block.
            case VisualState.Selected: image.color = selectedColor; break; // Handles this switch case.
            case VisualState.Correct: image.color = correctColor; break; // Handles this switch case.
            case VisualState.Wrong: image.color = wrongColor; break; // Handles this switch case.
            default: image.color = normalColor; break; // Handles any unmatched switch case.
        } // Ends this code block.
    } // Ends this code block.

    private void NotifyStateChanged() // Declares a method used by this script.
    { // Starts this code block.
        ApplyVisual(); // Runs this line of puzzle logic.
        StateChanged?.Invoke(this); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ─────────────────────────────
    // GROUP REGISTRATION (kept; harmless)
    // ─────────────────────────────
    private void Register() // Declares a method used by this script.
    { // Starts this code block.
        if (!Groups.TryGetValue(groupId, out var list)) // Checks this condition before continuing.
        { // Starts this code block.
            list = new List<PhotonSyncedUIButtonColor>(); // Updates this value.
            Groups[groupId] = list; // Updates this value.
        } // Ends this code block.

        if (!list.Contains(this)) // Checks this condition before continuing.
            list.Add(this); // Runs this line of puzzle logic.

        if (!SelectedViewId.ContainsKey(groupId)) // Checks this condition before continuing.
            SelectedViewId[groupId] = -1; // Updates this value.
    } // Ends this code block.

    private void Unregister() // Declares a method used by this script.
    { // Starts this code block.
        if (Groups.TryGetValue(groupId, out var list)) // Checks this condition before continuing.
            list.Remove(this); // Runs this line of puzzle logic.
    } // Ends this code block.

    // Optional: kept for compatibility. In multi-select, this simply clears THIS button.
    public void ClearSelectionAndSync() // Declares a method used by this script.
    { // Starts this code block.
        ClearSelectedAndSync(); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ─────────────────────────────
    // STABLE AUTO GROUP ID (kept)
    // ─────────────────────────────
    private string BuildStableGroupId() // Declares a method used by this script.
    { // Starts this code block.
        var parts = new Stack<string>(); // Creates a local value used by the following logic.
        Transform t = transform.parent; // group by parent container
        while (t != null) // Repeats this block while the condition stays true.
        { // Starts this code block.
            parts.Push(t.name); // Runs this line of puzzle logic.
            t = t.parent; // Updates this value.
        } // Ends this code block.

        string path = string.Join("/", parts.ToArray()); // Creates a local value used by the following logic.
        return $"Scene:{gameObject.scene.name}|{path}"; // Returns this result to the caller.
    } // Ends this code block.

    private void PersistState() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildSelectedKey()] = isSelected, // Adds an attribute that changes how Unity or C# treats the next declaration.
            [BuildResultStateKey()] = (int)resultState // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyPersistedRoomState() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties; // Creates a local value used by the following logic.
        bool changed = false; // Creates a local value used by the following logic.

        string selectedKey = BuildSelectedKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(selectedKey) && properties[selectedKey] is bool selected) // Checks this condition before continuing.
        { // Starts this code block.
            isSelected = selected; // Updates this value.
            changed = true; // Updates this value.
        } // Ends this code block.

        string resultStateKey = BuildResultStateKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(resultStateKey) && properties[resultStateKey] is int state) // Checks this condition before continuing.
        { // Starts this code block.
            resultState = (VisualState)state; // Updates this value.
            changed = true; // Updates this value.
        } // Ends this code block.

        if (changed) // Checks this condition before continuing.
            NotifyStateChanged(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private string BuildSelectedKey() => $"{SelectedPropertyPrefix}{photonView.ViewID}"; // Declares a method used by this script.
    private string BuildResultStateKey() => $"{ResultStatePropertyPrefix}{photonView.ViewID}"; // Declares a method used by this script.
} // Ends this code block.
