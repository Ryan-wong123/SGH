using ExitGames.Client.Photon; // Gives this script access to Photon Hashtable values for custom player properties.
using Photon.Pun; // Gives this script access to Photon PUN types like MonoBehaviourPunCallbacks and photonView.
using Photon.Realtime; // Gives this script access to Photon Realtime Player data.
using TMPro; // Gives this script access to TextMeshPro for 3D world-space text.
using UnityEngine; // Gives this script access to Unity engine types like GameObject, Transform, Camera, Bounds, Color, and Vector3.

[RequireComponent(typeof(PhotonView))] // Makes Unity require a PhotonView component on the same GameObject.
public class PlayerTagDisplay : MonoBehaviourPunCallbacks // Defines a Photon-aware component that displays a floating name/tag above a player.
{ // Starts the PlayerTagDisplay class body.
    private const string PlayerTagPropertyKey = "PlayerTag"; // Stores the custom Photon property key used for player tags.

    [Header("Tag Visuals")] // Adds a "Tag Visuals" section header in the Unity Inspector.
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f); // Stores the extra world-space offset applied to the tag position.
    [SerializeField] private int fontSize = 4; // Stores the TextMeshPro font size for the tag.
    [SerializeField] private Color textColor = Color.white; // Stores the text color for the tag.
    [SerializeField] private bool hideForLocalOwner = true; // Controls whether the local player should hide their own tag.

    private Camera targetCamera; // Stores the camera that the tag should face.
    private TextMeshPro textMesh; // Stores the generated TextMeshPro component that displays the tag.
    private Transform tagTransform; // Stores the generated tag object's transform.
    private Vector3 resolvedOffset; // Stores the final calculated offset used to position the tag above the player.

    private void Awake() // Runs when this component is initialized.
    { // Starts the Awake method body.
        CreateTagObject(); // Creates the floating TextMeshPro tag object if it does not already exist.
        resolvedOffset = CalculateOffsetFromRenderers(); // Calculates how high above the player the tag should appear.
        UpdateTagText(); // Sets the tag text from Photon player data.
        ApplyLocalVisibility(); // Applies the setting that hides the local owner's own tag if enabled.
    } // Ends the Awake method body.

    private void LateUpdate() // Runs after regular Update calls each frame.
    { // Starts the LateUpdate method body.
        if (tagTransform == null) // Checks whether the tag object is missing.
        { // Starts the missing-tag guard block.
            return; // Stops because there is no tag transform to update.
        } // Ends the missing-tag guard block.

        tagTransform.position = transform.position + resolvedOffset; // Moves the tag to the player's position plus the calculated offset.

        Camera cam = GetTargetCamera(); // Finds or reuses the camera that the tag should face.
        if (cam == null) // Checks whether no usable camera is available.
        { // Starts the missing-camera guard block.
            return; // Stops because the tag cannot face a camera that does not exist.
        } // Ends the missing-camera guard block.

        Vector3 direction = tagTransform.position - cam.transform.position; // Calculates the direction from the camera to the tag.
        if (direction.sqrMagnitude > 0.001f) // Checks whether the direction is long enough to create a reliable rotation.
        { // Starts the face-camera block.
            tagTransform.rotation = Quaternion.LookRotation(direction.normalized); // Rotates the tag so it faces toward the camera.
        } // Ends the face-camera block.
    } // Ends the LateUpdate method body.

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) // Runs when Photon reports changed custom properties for a player.
    { // Starts the OnPlayerPropertiesUpdate callback body.
        if (photonView.Owner == null || targetPlayer != photonView.Owner || changedProps == null) // Checks that the update belongs to this tag's owner and includes property data.
        { // Starts the irrelevant-property-update guard block.
            return; // Stops because this property update should not affect this tag.
        } // Ends the irrelevant-property-update guard block.

        if (changedProps.ContainsKey(PlayerTagPropertyKey)) // Checks whether the player's tag property changed.
        { // Starts the tag-property-changed block.
            UpdateTagText(); // Refreshes the displayed tag text.
        } // Ends the tag-property-changed block.
    } // Ends the OnPlayerPropertiesUpdate callback body.

    private void CreateTagObject() // Creates the generated world-space text object used as the player tag.
    { // Starts the CreateTagObject method body.
        if (tagTransform != null) // Checks whether the tag object has already been created.
        { // Starts the already-created guard block.
            return; // Stops so duplicate tag objects are not created.
        } // Ends the already-created guard block.

        GameObject tagObject = new GameObject("PlayerTag"); // Creates a new GameObject to hold the floating tag text.
        tagObject.transform.SetParent(null, false); // Keeps the tag object unparented so it can be positioned directly in world space.

        textMesh = tagObject.AddComponent<TextMeshPro>(); // Adds a TextMeshPro component to render the tag text.
        textMesh.alignment = TextAlignmentOptions.Center; // Centers the tag text horizontally.
        textMesh.fontSize = fontSize; // Applies the configured font size to the tag text.
        textMesh.color = textColor; // Applies the configured text color to the tag text.
        textMesh.enableWordWrapping = false; // Prevents the player tag from wrapping onto multiple lines.
        textMesh.outlineWidth = 0.2f; // Adds an outline to make the tag easier to read.
        textMesh.outlineColor = Color.black; // Sets the outline color to black for contrast.
        textMesh.text = string.Empty; // Starts the tag with no visible text until player data is applied.

        tagTransform = tagObject.transform; // Stores the generated tag object's transform.
        tagTransform.localScale = Vector3.one * 0.2f; // Scales the world-space text down to a suitable size.
    } // Ends the CreateTagObject method body.

    private Vector3 CalculateOffsetFromRenderers() // Calculates a vertical tag offset based on this player's visible renderers.
    { // Starts the CalculateOffsetFromRenderers method body.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(); // Gets all Renderer components under the player object.
        if (renderers.Length == 0) // Checks whether the player has no renderers to measure.
        { // Starts the no-renderers fallback block.
            return new Vector3(worldOffset.x, 2.25f + worldOffset.y, worldOffset.z); // Returns a default height plus the configured offset.
        } // Ends the no-renderers fallback block.

        Bounds combinedBounds = renderers[0].bounds; // Starts the combined bounds using the first renderer's world bounds.
        for (int i = 1; i < renderers.Length; i++) // Loops through the remaining renderers.
        { // Starts the renderer-bounds loop body.
            combinedBounds.Encapsulate(renderers[i].bounds); // Expands the combined bounds to include this renderer.
        } // Ends the renderer-bounds loop body.

        float height = Mathf.Max(1.5f, combinedBounds.max.y - transform.position.y); // Calculates at least 1.5 units of height above the player origin.
        return new Vector3(worldOffset.x, height + worldOffset.y, worldOffset.z); // Returns the final offset using measured height plus configured offset.
    } // Ends the CalculateOffsetFromRenderers method body.

    private void UpdateTagText() // Refreshes the TextMeshPro text shown above the player.
    { // Starts the UpdateTagText method body.
        if (textMesh == null) // Checks whether the text component is missing.
        { // Starts the missing-text guard block.
            return; // Stops because there is no text component to update.
        } // Ends the missing-text guard block.

        textMesh.text = GetPlayerTag(); // Sets the displayed tag to the best available player tag string.
    } // Ends the UpdateTagText method body.

    private string GetPlayerTag() // Gets the display name/tag for this player.
    { // Starts the GetPlayerTag method body.
        Player owner = photonView.Owner; // Gets the Photon player that owns this object.
        if (owner == null) // Checks whether this object has no Photon owner.
        { // Starts the missing-owner fallback block.
            return "Player"; // Returns a generic fallback tag.
        } // Ends the missing-owner fallback block.

        if (owner.CustomProperties != null && // Checks that the owner has a custom properties table.
            owner.CustomProperties.TryGetValue(PlayerTagPropertyKey, out object tagValue) && // Tries to read the configured player tag property.
            tagValue is string tag && // Checks that the stored tag value is a string.
            !string.IsNullOrWhiteSpace(tag)) // Checks that the tag contains visible text.
        { // Starts the custom-tag block.
            return tag.Trim(); // Returns the custom tag without leading or trailing whitespace.
        } // Ends the custom-tag block.

        if (!string.IsNullOrWhiteSpace(owner.NickName)) // Checks whether the Photon owner has a usable nickname.
        { // Starts the nickname fallback block.
            return owner.NickName.Trim(); // Returns the nickname without leading or trailing whitespace.
        } // Ends the nickname fallback block.

        return $"Player {owner.ActorNumber}"; // Returns a final fallback based on the owner's Photon actor number.
    } // Ends the GetPlayerTag method body.

    private void ApplyLocalVisibility() // Applies whether the local player should see their own tag.
    { // Starts the ApplyLocalVisibility method body.
        if (textMesh == null) // Checks whether the text component is missing.
        { // Starts the missing-text guard block.
            return; // Stops because there is no text component to show or hide.
        } // Ends the missing-text guard block.

        textMesh.enabled = !(hideForLocalOwner && photonView.IsMine); // Hides the tag when this is the local player's object and local hiding is enabled.
    } // Ends the ApplyLocalVisibility method body.

    private Camera GetTargetCamera() // Finds the best active camera for the tag to face.
    { // Starts the GetTargetCamera method body.
        if (targetCamera != null && targetCamera.isActiveAndEnabled) // Checks whether the cached camera is still usable.
        { // Starts the cached-camera block.
            return targetCamera; // Returns the cached active camera.
        } // Ends the cached-camera block.

        targetCamera = Camera.main; // Tries to use Unity's main camera.
        if (targetCamera != null && targetCamera.isActiveAndEnabled) // Checks whether the main camera exists and is active.
        { // Starts the main-camera block.
            return targetCamera; // Returns the active main camera.
        } // Ends the main-camera block.

        Camera[] cameras = FindObjectsOfType<Camera>(); // Finds all cameras in the scene.
        for (int i = 0; i < cameras.Length; i++) // Loops through every camera found in the scene.
        { // Starts the camera-search loop body.
            if (cameras[i].isActiveAndEnabled) // Checks whether this camera is active and enabled.
            { // Starts the active-camera-found block.
                targetCamera = cameras[i]; // Caches the active camera.
                return targetCamera; // Returns the active camera.
            } // Ends the active-camera-found block.
        } // Ends the camera-search loop body.

        return null; // Returns null when no active camera could be found.
    } // Ends the GetTargetCamera method body.

    private void OnDestroy() // Runs when this component or its GameObject is being destroyed.
    { // Starts the OnDestroy method body.
        if (tagTransform != null) // Checks whether the generated tag object still exists.
        { // Starts the destroy-tag block.
            Destroy(tagTransform.gameObject); // Destroys the generated tag object to avoid leaving orphaned text in the scene.
        } // Ends the destroy-tag block.
    } // Ends the OnDestroy method body.
} // Ends the PlayerTagDisplay class body.
