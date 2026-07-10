using Photon.Pun; // Imports Photon networking types such as PhotonView, PhotonNetwork, and PunRPC.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Transform, Quaternion, and Debug.

public class DoorInteractable : MonoBehaviour, IInteractable, IHighlightable // Defines a door component that can be interacted with and highlighted.
{ // Opens the DoorInteractable class body.
    [Header("Door Settings")] // Groups the door lock fields in the Inspector.
    [SerializeField] private bool isLocked = false; // Stores whether the door is currently locked.
    [SerializeField] private bool requiresKey = false; // Stores whether the player must have a named inventory item to use the door.
    [SerializeField] private string keyName = "Key"; // Stores the inventory item name required when key access is enabled.

    [Header("Keypad Unlock Behaviour")] // Groups the keypad-related unlock fields in the Inspector.
    [SerializeField] private bool openOnUnlock = true; // Stores whether the door should open automatically after keypad unlock.
    [SerializeField] private bool permanentlyDisableKeyRequirementOnUnlock = true; // Stores whether keypad unlock should remove the key requirement.

    [Header("Leaf Settings")] // Groups the door leaf and hinge fields in the Inspector.
    [SerializeField] private bool isLeftDoor = false; // Stores whether the hinge should be treated as the left edge of the door.
    [SerializeField] private float openAngle = 90f; // Stores how many degrees the door rotates when opened.
    [Tooltip("Optional. If not set, a hinge is auto-created at the door edge.")] // Shows Inspector help text for the hinge assignment.
    [SerializeField] private Transform hinge; // Stores the transform used as the door's rotation pivot.

    [Header("Animation")] // Groups the door animation fields in the Inspector.
    [SerializeField] private bool smooth = false; // Stores whether opening and closing should animate smoothly.
    [SerializeField] private float smoothSpeed = 360f; // Stores the smooth rotation speed in degrees per second.

    private HighlightEffect highlightEffect; // Caches the highlight effect used to visually mark the door.
    private Transform highlightRoot; // Optionally stores the root transform to search for a highlight effect.

    private bool isOpen = false; // Tracks whether the door is currently open.
    private Quaternion closedRotLocal; // Stores the hinge's local rotation when the door is closed.
    private Quaternion openRotLocal; // Stores the hinge's local rotation when the door is open.

    private PhotonView pv; // Caches the PhotonView used to synchronize door state over the network.

    private void Awake() // Runs Unity initialization before the first frame.
    { // Opens the Awake method body.
        gameObject.layer = LayerMask.NameToLayer("Interactable"); // Places this door object on the Interactable layer.
        SetLayerRecursive(transform, gameObject.layer); // Applies the same Interactable layer to this object and all children.

        SetupPhotonView(); // Ensures the door has the Photon components needed for networking.
        EnsureHingeExistsAndIsParented(); // Finds or creates a hinge and parents the door under it.

        if (hinge == null) // Checks whether hinge setup failed.
        { // Opens the missing-hinge error block.
            Debug.LogError($"{name}: Hinge could not be found/created. Assign one manually."); // Logs an error that tells designers to assign a hinge manually.
            return; // Stops initialization because rotations cannot be calculated without a hinge.
        } // Closes the missing-hinge error block.

        closedRotLocal = hinge.localRotation; // Records the current hinge rotation as the closed door rotation.

        float dir = isLeftDoor ? -1f : 1f; // Chooses the opening direction based on whether this is a left door.
        if (IsMirrored(hinge)) dir *= -1f; // Flips the opening direction when the hinge hierarchy is mirrored.

        openRotLocal = closedRotLocal * Quaternion.Euler(0f, dir * openAngle, 0f); // Calculates the local rotation the hinge should use when open.

        if (highlightEffect == null) // Checks whether the highlight effect has not already been assigned.
        { // Opens the highlight lookup block.
            Transform searchRoot = highlightRoot != null ? highlightRoot : transform; // Chooses the configured highlight root or this door as the search root.
            highlightEffect = searchRoot.GetComponent<HighlightEffect>(); // Looks for a highlight effect directly on the search root.
            if (highlightEffect == null) // Checks whether the direct lookup failed.
                highlightEffect = searchRoot.GetComponentInChildren<HighlightEffect>(true); // Searches children, including inactive children, for a highlight effect.
        } // Closes the highlight lookup block.
    } // Closes the Awake method body.

    static void SetLayerRecursive(Transform t, int layer) // Assigns a layer to a transform and all of its descendants.
    { // Opens the recursive layer helper body.
        t.gameObject.layer = layer; // Sets the current transform's GameObject layer.
        for (int i = 0; i < t.childCount; i++) // Loops through every child transform.
            SetLayerRecursive(t.GetChild(i), layer); // Applies the same layer to the current child and its descendants.
    } // Closes the recursive layer helper body.

    void SetupPhotonView() // Prepares Photon networking components for the door.
    { // Opens the Photon setup method body.
        pv = GetComponent<PhotonView>(); // Tries to find an existing PhotonView on this GameObject.
        if (pv == null) pv = gameObject.AddComponent<PhotonView>(); // Adds a PhotonView when one is missing.

        pv.OwnershipTransfer = OwnershipOption.Fixed; // Keeps ownership fixed so the scene object is not transferred between players.

        if (GetComponent<PhotonTransformView>() == null) // Checks whether transform synchronization is missing.
        { // Opens the transform-view setup block.
            var transformView = gameObject.AddComponent<PhotonTransformView>(); // Adds a Photon transform synchronization component.
            pv.ObservedComponents.Add(transformView); // Registers the transform view with the PhotonView's observed components.
        } // Closes the transform-view setup block.

        pv.Synchronization = ViewSynchronization.UnreliableOnChange; // Synchronizes changes only when values change, using unreliable delivery.

        if (PhotonNetwork.IsConnectedAndReady && pv.ViewID == 0) // Checks whether Photon is ready and the view has not been registered.
        { // Opens the runtime Photon registration block.
            PhotonNetwork.RegisterPhotonView(pv); // Registers this scene-created PhotonView with Photon.
            Debug.Log($"Registered scene object with ViewID: {pv.ViewID}"); // Logs the ViewID assigned by Photon.
        } // Closes the runtime Photon registration block.
    } // Closes the Photon setup method body.

    private void OnDisable() // Runs when Unity disables this component or GameObject.
    { // Opens the OnDisable method body.
        highlightEffect?.Unhighlight(); // Clears the highlight safely if a highlight effect exists.
    } // Closes the OnDisable method body.

    // ---------------- IInteractable ---------------- // Marks the start of the IInteractable implementation.

    public void Interact() // Handles the player interaction request.
    { // Opens the Interact method body.
        if (isLocked) // Checks whether the door is locked.
        { // Opens the locked-door block.
            Debug.Log("Door is locked."); // Logs feedback that the door cannot be opened.
            return; // Stops interaction because locked doors cannot toggle.
        } // Closes the locked-door block.

        if (requiresKey && (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(keyName))) // Checks whether the required key is missing.
        { // Opens the missing-key block.
            Debug.Log("Use keypad to unlock door"); // Logs feedback that keypad unlock is needed.
            return; // Stops interaction because the key requirement is not satisfied.
        } // Closes the missing-key block.

        bool targetState = !isOpen; // Chooses the opposite of the current open state.

        if (pv != null && PhotonNetwork.IsConnected) // Checks whether the door should synchronize through Photon.
        { // Opens the networked interaction block.
            // Scene object ownership fixed; allow Master to toggle, or allow anyone by directly RPCing AllBuffered // Explains why the RPC is sent directly to all buffered clients.
            pv.RPC(nameof(RPC_SetOpen), RpcTarget.AllBuffered, targetState); // Sends the new open state to all current and future clients.
        } // Closes the networked interaction block.
        else // Handles local play or missing Photon setup.
        { // Opens the local interaction block.
            SetOpenLocal(targetState); // Applies the new open state locally.
        } // Closes the local interaction block.
    } // Closes the Interact method body.

    public string GetInteractionPrompt() // Returns the prompt text shown to the player.
    { // Opens the prompt method body.
        if (isLocked) return "This door is locked."; // Tells the player the door is locked.
        if (requiresKey && (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(keyName))) return "Use keypad to unlock door"; // Tells the player to use the keypad when the key requirement is unmet.
        return isOpen ? "Press E to close door" : "Press E to open door"; // Shows the correct open or close interaction prompt.
    } // Closes the prompt method body.

    // ---------------- IHighlightable ---------------- // Marks the start of the IHighlightable implementation.
    public void Highlight() => highlightEffect?.Highlight(); // Turns on the highlight if the highlight effect exists.
    public void Unhighlight() => highlightEffect?.Unhighlight(); // Turns off the highlight if the highlight effect exists.

    // ---------------- Keypad API ---------------- // Marks the start of the keypad-facing API.

    // Called by Keypad when correct PIN entered // Explains when the keypad calls this method.
    public void UnlockFromKeypad() // Unlocks the door after a successful keypad entry.
    { // Opens the keypad unlock method body.
        if (pv != null && PhotonNetwork.IsConnected) // Checks whether keypad unlock should be synchronized through Photon.
        { // Opens the networked keypad unlock block.
            pv.RPC(nameof(RPC_KeypadUnlock), RpcTarget.AllBuffered, true); // Sends the unlocked state to all current and future clients.
        } // Closes the networked keypad unlock block.
        else // Handles local play or missing Photon setup.
        { // Opens the local keypad unlock block.
            ApplyKeypadUnlockLocal(true); // Applies the unlocked state locally.
        } // Closes the local keypad unlock block.
    } // Closes the keypad unlock method body.

    [PunRPC] // Allows Photon to call this method through RPC.
    private void RPC_KeypadUnlock(bool unlocked) // Receives the keypad unlock state from the network.
    { // Opens the keypad RPC method body.
        ApplyKeypadUnlockLocal(unlocked); // Applies the received unlock state locally.
    } // Closes the keypad RPC method body.

    private void ApplyKeypadUnlockLocal(bool unlocked) // Applies keypad unlock effects on this client.
    { // Opens the local keypad unlock application body.
        isLocked = !unlocked; // Converts the unlocked flag into the inverse locked state.
        if (permanentlyDisableKeyRequirementOnUnlock) // Checks whether unlocking should remove future key checks.
            requiresKey = false; // Disables the key requirement after keypad unlock.

        if (unlocked && openOnUnlock) // Checks whether the unlocked door should open immediately.
            SetOpenLocal(true); // Opens the door locally.
    } // Closes the local keypad unlock application body.

    // ---------------- Network sync (open/close) ---------------- // Marks the start of open and close synchronization.

    [PunRPC] // Allows Photon to call this method through RPC.
    void RPC_SetOpen(bool open) // Receives the target open state from the network.
    { // Opens the open-state RPC method body.
        SetOpenLocal(open); // Applies the received open state locally.
    } // Closes the open-state RPC method body.

    void SetOpenLocal(bool open) // Applies an open or closed state on this client.
    { // Opens the local open-state method body.
        if (hinge == null) return; // Stops if there is no hinge to rotate.

        isOpen = open; // Stores the current open state.
        Quaternion target = isOpen ? openRotLocal : closedRotLocal; // Chooses the target hinge rotation for the desired state.

        if (!smooth) // Checks whether the door should snap instantly.
        { // Opens the instant-rotation block.
            hinge.localRotation = target; // Sets the hinge directly to the target rotation.
            return; // Stops because no smooth animation is needed.
        } // Closes the instant-rotation block.

        StopAllCoroutines(); // Cancels any existing door rotation animation.
        StartCoroutine(RotateTo(hinge, target, smoothSpeed)); // Starts a smooth rotation toward the target rotation.
    } // Closes the local open-state method body.

    System.Collections.IEnumerator RotateTo(Transform t, Quaternion target, float degPerSec) // Rotates a transform gradually toward a target rotation.
    { // Opens the rotation coroutine body.
        while (Quaternion.Angle(t.localRotation, target) > 0.1f) // Continues until the transform is nearly at the target rotation.
        { // Opens the rotation loop body.
            float step = degPerSec * Time.deltaTime; // Calculates the maximum rotation amount for this frame.
            t.localRotation = Quaternion.RotateTowards(t.localRotation, target, step); // Moves the transform rotation toward the target rotation.
            yield return null; // Waits until the next frame before continuing the rotation.
        } // Closes the rotation loop body.
        t.localRotation = target; // Snaps exactly to the target rotation at the end.
    } // Closes the rotation coroutine body.

    // ---------------- Hinge helpers ---------------- // Marks the start of hinge setup helper methods.

    void EnsureHingeExistsAndIsParented() // Ensures the door has a hinge parent to rotate around.
    { // Opens the hinge setup method body.
        if (hinge != null) // Checks whether a hinge was assigned in the Inspector.
        { // Opens the assigned-hinge block.
            if (transform.parent != hinge) // Checks whether the door is not already parented under the hinge.
                ReparentWithoutMoving(transform, hinge); // Parents the door under the hinge without changing world position or rotation.
            return; // Stops because the assigned hinge is ready.
        } // Closes the assigned-hinge block.

        Transform found = null; // Declares a variable for a hinge found in the hierarchy.
        if (transform.parent != null) found = transform.parent.Find("Hinge"); // Looks for a sibling hinge under the current parent.
        if (found == null) found = transform.Find("Hinge"); // Looks for a child hinge if no sibling hinge was found.

        if (found != null) // Checks whether a hinge was found in the hierarchy.
        { // Opens the found-hinge block.
            hinge = found; // Stores the found hinge transform.
            if (transform.parent != hinge) // Checks whether the door is not already parented under the found hinge.
                ReparentWithoutMoving(transform, hinge); // Parents the door under the found hinge without changing world position or rotation.
            return; // Stops because the found hinge is ready.
        } // Closes the found-hinge block.

        var rend = GetComponentInChildren<Renderer>(); // Looks for a renderer so the door bounds can be used to place a hinge.
        if (rend == null) // Checks whether bounds cannot be inferred.
        { // Opens the missing-renderer block.
            Debug.LogWarning($"{name}: No Renderer found to infer bounds; cannot auto-create hinge."); // Logs a warning that auto hinge creation cannot continue.
            return; // Stops hinge creation because there are no renderer bounds.
        } // Closes the missing-renderer block.

        Bounds b = rend.bounds; // Stores the renderer's world-space bounds.
        Vector3 edge = b.center; // Starts the hinge position at the center of the bounds.
        edge.x = isLeftDoor ? b.min.x : b.max.x; // Moves the hinge position to the left or right edge of the bounds.

        var go = new GameObject("Hinge"); // Creates a new GameObject to use as the hinge.
        hinge = go.transform; // Stores the new GameObject's transform as the hinge.
        hinge.position = edge; // Places the hinge at the calculated door edge.
        hinge.rotation = transform.rotation; // Matches the hinge rotation to the door's current rotation.
        hinge.localScale = Vector3.one; // Gives the hinge a neutral local scale.

        hinge.SetParent(transform.parent, worldPositionStays: true); // Parents the hinge beside the door while preserving its world transform.
        ReparentWithoutMoving(transform, hinge); // Parents the door under the hinge without changing world position or rotation.
    } // Closes the hinge setup method body.

    static void ReparentWithoutMoving(Transform child, Transform newParent) // Reparents a transform while preserving its world pose.
    { // Opens the reparent helper body.
        Vector3 wp = child.position; // Saves the child's current world position.
        Quaternion wr = child.rotation; // Saves the child's current world rotation.
        child.SetParent(newParent, worldPositionStays: false); // Assigns the new parent without asking Unity to preserve the world transform.
        child.position = wp; // Restores the saved world position.
        child.rotation = wr; // Restores the saved world rotation.
    } // Closes the reparent helper body.

    static bool IsMirrored(Transform t) => t.localToWorldMatrix.determinant < 0f; // Returns true when the transform hierarchy has a mirrored scale.
} // Closes the DoorInteractable class body.
