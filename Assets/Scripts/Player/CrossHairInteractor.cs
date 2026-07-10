using UnityEngine; // Gives this script access to Unity engine types like Camera, Ray, Physics, Transform, and MonoBehaviour.
using TMPro; // Gives this script access to TextMeshProUGUI for showing interaction prompts.
using Photon.Pun; // Gives this script access to Photon ownership checks through MonoBehaviourPun and photonView.

public class CrosshairInteractor : MonoBehaviourPun // Defines a Photon-aware Unity component for interacting with objects through the crosshair.
{ // Starts the CrosshairInteractor class body.
    [Header("Raycast")] // Adds a "Raycast" section header in the Unity Inspector.
    [SerializeField] private Camera playerCamera; // Stores the camera used as the origin and direction for interaction raycasts.
    [SerializeField] private float interactDistance = 3f; // Stores the maximum distance the player can interact from.

    [Tooltip("Layers that contain interactable things (doors, keypad buttons, etc.).")] // Describes what the layer mask is for in the Unity Inspector.
    [SerializeField] private LayerMask interactableLayer = ~0; // Stores the layers to raycast against, defaulting to every layer.

    [Tooltip("If true, we try to click keypad buttons even if a door is slightly in front.")] // Explains the keypad priority option in the Unity Inspector.
    [SerializeField] private bool preferKeypadButtons = true; // Controls whether keypad buttons should be prioritized over other interactables.

    [Header("UI")] // Adds a "UI" section header in the Unity Inspector.
    [SerializeField] private GameObject interactionUIPanel; // Optionally stores the UI panel or background that contains the interaction prompt.
    private TextMeshProUGUI interactionLabel; // Stores the text component used to display the current interaction prompt.

    // State // Marks the fields that track the current interaction state.
    private IInteractable currentTarget; // Stores the interactable currently under the crosshair.
    private IHighlightable currentHighlight; // Stores the highlightable object currently being highlighted.

    private void Start() // Runs once when this component starts.
    { // Starts the Start method body.
        if (!photonView.IsMine) return; // Stops setup on remote players so only the local player controls this interactor.

        if (playerCamera == null) // Checks whether a camera was assigned in the Inspector.
        { // Starts the missing-camera fallback block.
            playerCamera = Camera.main; // Tries to use the scene's main camera as a fallback.
            if (playerCamera == null) // Checks whether no main camera could be found either.
                Debug.LogError($"{name}: playerCamera is not assigned and no MainCamera found."); // Logs an error so the missing camera is visible during testing.
        } // Ends the missing-camera fallback block.

        if (interactionUIPanel != null) // Checks whether a UI panel was assigned in the Inspector.
            interactionLabel = interactionUIPanel.GetComponentInChildren<TextMeshProUGUI>(true); // Finds the prompt label inside the assigned UI panel, including inactive children.

        if (interactionLabel == null) // Checks whether no prompt label has been found yet.
        { // Starts the fallback search for the prompt label.
            var labelObj = GameObject.Find("Cross Hair Text"); // Searches the scene for a GameObject named "Cross Hair Text".
            if (labelObj != null) // Checks whether the named prompt object exists.
                interactionLabel = labelObj.GetComponent<TextMeshProUGUI>(); // Gets the TextMeshProUGUI component from the found prompt object.
        } // Ends the fallback search for the prompt label.

        if (interactionLabel != null) // Checks whether a prompt label is available.
        { // Starts the prompt label setup block.
            if (interactionUIPanel == null && interactionLabel.transform.parent != null) // Checks whether the panel should be inferred from the label's parent.
                interactionUIPanel = interactionLabel.transform.parent.gameObject; // Uses the label's parent GameObject as the UI panel.

            SetInteractionUIVisible(false); // Hides the interaction UI at startup.
        } // Ends the prompt label setup block.
    } // Ends the Start method body.

    private void Update() // Runs once per frame.
    { // Starts the Update method body.
        if (!photonView.IsMine || playerCamera == null) return; // Stops processing for remote players or when no camera is available.

        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Creates a ray from the camera position straight forward through the crosshair.

        // Grab all hits so we can prioritize KeypadButton first // Explains why RaycastAll is used instead of a single raycast.
        var hits = Physics.RaycastAll(ray, interactDistance, interactableLayer, QueryTriggerInteraction.Collide); // Finds every collider hit by the interaction ray within range and layer settings.
        if (hits.Length == 0) // Checks whether the ray hit nothing interactable.
        { // Starts the no-hit block.
            ClearInteraction(); // Clears UI and highlighting because there is no current target.
            return; // Stops this frame's interaction processing.
        } // Ends the no-hit block.

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // Sorts hits from nearest to farthest.

        // Resolve the best target from the hits // Introduces the target selection step.
        var (button, interactable, highlightable, hitTransform) = ResolveBestTarget(hits, preferKeypadButtons); // Picks the best keypad button, interactable, highlightable, and hit transform from the raycast hits.

        if (interactable != null || button != null) // Checks whether the ray found something the player can interact with or click.
        { // Starts the valid-target block.
            // Update UI + highlight // Introduces the UI and highlight refresh step.
            UpdateInteractionUIAndHighlight(interactable, highlightable); // Updates the prompt text and highlighted object for the current target.

            // Click (LMB) -> KeypadButton first, else Interact() // Explains the left-click priority for keypad buttons.
            if (Input.GetMouseButtonDown(0) && button != null) // Checks whether the player left-clicked while aiming at a keypad button.
            { // Starts the keypad click block.
                button.PressButton(); // Presses the keypad button.
                return; // Stops here so the same click does not also trigger other interaction logic.
            } // Ends the keypad click block.

            // Press E -> Interact() on doors/other interactables // Explains the keyboard interaction path.
            if (Input.GetKeyDown(KeyCode.E) && interactable != null) // Checks whether the player pressed E while aiming at an interactable.
            { // Starts the interact key block.
                interactable.Interact(); // Runs the interactable object's interaction behavior.
            } // Ends the interact key block.
        } // Ends the valid-target block.
        else // Runs when raycast hits existed but none resolved to a usable target.
        { // Starts the invalid-target block.
            ClearInteraction(); // Clears UI and highlighting because no usable target is selected.
        } // Ends the invalid-target block.
    } // Ends the Update method body.

    // ---------- Helpers ---------- // Marks the start of helper methods.

    private (NavKeypad.KeypadButton button, IInteractable interactable, IHighlightable highlightable, Transform hitT) // Declares a helper that returns the selected button, interactable, highlightable, and hit transform.
        ResolveBestTarget(RaycastHit[] hits, bool preferButtons) // Names the helper and receives the sorted raycast hits plus the keypad priority setting.
    { // Starts the ResolveBestTarget method body.
        NavKeypad.KeypadButton foundButton = null; // Stores the keypad button selected from the hits, if any.
        IInteractable foundInteractable = null; // Stores the interactable selected from the hits, if any.
        IHighlightable foundHighlightable = null; // Stores the highlight target selected from the hits, if any.
        Transform hitT = null; // Stores the transform of the collider that produced the chosen hit.

        // 1) If we prefer buttons, search for the nearest KeypadButton in parent chain first // Explains the first target resolution pass.
        if (preferButtons) // Checks whether keypad buttons should be prioritized.
        { // Starts the keypad-priority block.
            foreach (var h in hits) // Loops through raycast hits from nearest to farthest.
            { // Starts the per-hit keypad search block.
                var b = h.collider.GetComponent<NavKeypad.KeypadButton>() ?? // Tries to find a keypad button directly on the hit collider.
                        h.collider.GetComponentInParent<NavKeypad.KeypadButton>(); // Falls back to finding a keypad button on a parent object.
                if (b != null) // Checks whether this hit belongs to a keypad button.
                { // Starts the found-button block.
                    foundButton = b; // Stores the selected keypad button.
                    // Also find highlight on the same object chain for feedback // Explains why highlight lookup happens with the button.
                    foundHighlightable = h.collider.GetComponent<IHighlightable>() ?? // Tries to find a highlightable component directly on the hit collider.
                                         h.collider.GetComponentInParent<IHighlightable>(); // Falls back to finding a highlightable component on a parent object.
                    hitT = h.collider.transform; // Stores the transform of the collider hit for this selection.
                    break; // Stops searching because the nearest suitable keypad button was found.
                } // Ends the found-button block.
            } // Ends the per-hit keypad search block.
        } // Ends the keypad-priority block.

        // 2) If no button (or not preferring), resolve to nearest IInteractable in parent chain // Explains the fallback target resolution pass.
        if (foundButton == null) // Checks whether no keypad button has been selected.
        { // Starts the interactable fallback block.
            foreach (var h in hits) // Loops through raycast hits from nearest to farthest.
            { // Starts the per-hit interactable search block.
                var inter = h.collider.GetComponentInParent<IInteractable>(); // Finds the nearest interactable component in the hit collider's parent chain.
                if (inter != null) // Checks whether this hit belongs to an interactable object.
                { // Starts the found-interactable block.
                    foundInteractable = inter; // Stores the selected interactable.

                    // Prefer highlight on the exact collider hit; fall back to parent // Explains the highlight lookup order for interactables.
                    foundHighlightable = h.collider.GetComponent<IHighlightable>() ?? // Tries to find a highlightable component directly on the hit collider.
                                         h.collider.GetComponentInParent<IHighlightable>(); // Falls back to finding a highlightable component on a parent object.
                    hitT = h.collider.transform; // Stores the transform of the collider hit for this selection.
                    break; // Stops searching because the nearest suitable interactable was found.
                } // Ends the found-interactable block.
            } // Ends the per-hit interactable search block.
        } // Ends the interactable fallback block.

        return (foundButton, foundInteractable, foundHighlightable, hitT); // Returns the best resolved target information.
    } // Ends the ResolveBestTarget method body.

    private void UpdateInteractionUIAndHighlight(IInteractable interactable, IHighlightable highlightable) // Updates the prompt UI and highlight state for the current target.
    { // Starts the UpdateInteractionUIAndHighlight method body.
        // ----- PROMPT ----- // Marks the start of prompt UI handling.
        if (interactable != null) // Checks whether there is an interactable that can provide prompt text.
        { // Starts the interactable prompt block.
            if (interactionLabel != null) // Checks whether a prompt label exists.
            { // Starts the prompt label update block.
                string prompt = interactable.GetInteractionPrompt(); // Gets the prompt text from the current interactable.
                bool show = !string.IsNullOrEmpty(prompt); // Decides whether the UI should be visible based on whether prompt text exists.
                if (show) interactionLabel.text = prompt; // Updates the label text only when there is prompt text to show.
                SetInteractionUIVisible(show); // Shows or hides the prompt UI based on the prompt text.
            } // Ends the prompt label update block.

            // Track current target (for potential prompt refresh later) // Explains why the current target reference is stored.
            if (!ReferenceEquals(currentTarget, interactable)) // Checks whether the selected interactable changed.
                currentTarget = interactable; // Stores the new current interactable target.
        } // Ends the interactable prompt block.
        else // Runs when there is no interactable prompt source.
        { // Starts the no-interactable prompt block.
            SetInteractionUIVisible(false); // Hides the interaction prompt UI.
            currentTarget = null; // Clears the current interactable target reference.
        } // Ends the no-interactable prompt block.

        // ----- HIGHLIGHT ----- // Marks the start of highlight handling.
        if (!ReferenceEquals(currentHighlight, highlightable)) // Checks whether the highlighted object changed.
        { // Starts the changed-highlight block.
            SafeUnhighlight(currentHighlight); // Removes highlighting from the previous object if it is still valid.
            currentHighlight = highlightable; // Stores the new highlight target.
            SafeHighlight(currentHighlight); // Applies highlighting to the new object if it is valid.
        } // Ends the changed-highlight block.
        else // Runs when the highlighted object reference has not changed.
        { // Starts the unchanged-highlight block.
            // If the cached highlight got destroyed meanwhile, clear it // Explains why the cached highlight is validated.
            if (!IsUnityAlive(currentHighlight)) // Checks whether the cached highlight target was destroyed.
            { // Starts the destroyed-highlight block.
                currentHighlight = null; // Clears the stale highlight reference.
            } // Ends the destroyed-highlight block.
        } // Ends the unchanged-highlight block.
    } // Ends the UpdateInteractionUIAndHighlight method body.

    private void ClearInteraction() // Clears the current interaction target, prompt UI, and highlight.
    { // Starts the ClearInteraction method body.
        currentTarget = null; // Clears the current interactable target reference.

        SetInteractionUIVisible(false); // Hides the interaction prompt UI.

        if (currentHighlight != null) // Checks whether something is currently highlighted.
        { // Starts the clear-highlight block.
            SafeUnhighlight(currentHighlight); // Removes highlighting from the current highlight target if it is valid.
            currentHighlight = null; // Clears the current highlight reference.
        } // Ends the clear-highlight block.
    } // Ends the ClearInteraction method body.

    // ---------- Safe helpers for IHighlightable ---------- // Marks the start of helper methods that guard highlight calls.

    private void SetInteractionUIVisible(bool visible) // Shows or hides the interaction UI.
    { // Starts the SetInteractionUIVisible method body.
        if (interactionUIPanel != null) // Checks whether a full UI panel is available.
        { // Starts the panel visibility block.
            interactionUIPanel.SetActive(visible); // Sets the panel active state to show or hide the prompt UI.
            return; // Stops here because the panel handled the UI visibility.
        } // Ends the panel visibility block.

        if (interactionLabel != null) // Checks whether only the text label is available.
            interactionLabel.gameObject.SetActive(visible); // Sets the label GameObject active state to show or hide the prompt text.
    } // Ends the SetInteractionUIVisible method body.

    // Unity "alive" check for interface-backed components // Explains why this helper exists.
    private static bool IsUnityAlive(IHighlightable h) // Checks whether an interface-backed highlight target still points to a live Unity component.
    { // Starts the IsUnityAlive method body.
        if (h == null) return false; // Returns false when there is no highlight target.
        // h is an interface; need to cast to Component to get Unity's custom null semantics // Explains the interface-to-Component cast.
        var comp = h as Component; // Casts the interface reference to a Unity Component.
        return comp != null; // Returns false if the Unity component has been destroyed.
    } // Ends the IsUnityAlive method body.

    private static void SafeHighlight(IHighlightable h) // Safely applies highlight behavior to a target.
    { // Starts the SafeHighlight method body.
        if (!IsUnityAlive(h)) return; // Stops if the target is missing or destroyed.
        try { h.Highlight(); } catch { /* swallow to keep loop healthy */ } // Tries to highlight and ignores errors so the interaction loop keeps running.
    } // Ends the SafeHighlight method body.

    private static void SafeUnhighlight(IHighlightable h) // Safely removes highlight behavior from a target.
    { // Starts the SafeUnhighlight method body.
        if (!IsUnityAlive(h)) return; // Stops if the target is missing or destroyed.
        try { h.Unhighlight(); } catch { /* swallow to keep loop healthy */ } // Tries to unhighlight and ignores errors so the interaction loop keeps running.
    } // Ends the SafeUnhighlight method body.
} // Ends the CrosshairInteractor class body.
