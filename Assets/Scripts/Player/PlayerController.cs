using UnityEngine; // Gives this script access to Unity engine types like Transform, Camera, Cursor, Input, and CharacterController.
using Photon.Pun; // Gives this script access to Photon networking types like MonoBehaviourPunCallbacks, photonView, and PhotonTransformView.
using System.Collections; // Gives this script access to coroutine-related collection interfaces if needed by this class.

[RequireComponent(typeof(CharacterController))] // Makes Unity require a CharacterController component on the same GameObject.
public class PlayerController : MonoBehaviourPunCallbacks // Defines a Photon-aware player controller that can receive Photon callback events.
{ // Starts the PlayerController class body.
    [SerializeField] private float moveSpeed = 5f; // Stores the player's movement speed and exposes it in the Unity Inspector.
    [SerializeField] private float mouseSensitivity = 2f; // Stores how strongly mouse movement rotates the camera and exposes it in the Unity Inspector.
    [SerializeField] private Transform cameraRoot; // Stores the transform that pitches the player camera up and down.
    [SerializeField] private Camera playerCamera; // Stores the local player's camera reference.

    private CharacterController controller; // Stores the CharacterController used to move the player.
    private Animator animator; // Stores the child Animator used to update walking animation state.

    [SerializeField] private float verticalRotation = 0f; // Stores the current up/down camera rotation and exposes it for debugging in the Inspector.
    private bool cursorUnlocked = false; // Tracks whether the cursor is currently free instead of locked for camera control.
    public static PlayerController Local { get; private set; } // Stores the local player's PlayerController so other scripts can access it safely.
    private void Start() // Runs once when this player object starts.
    { // Starts the Start method body.
        controller = GetComponent<CharacterController>(); // Gets the CharacterController attached to this player object.
        animator = GetComponentInChildren<Animator>(); // Gets the Animator from this object or one of its children.

        if (GetComponent<PlayerTagDisplay>() == null) // Checks whether this player object is missing a PlayerTagDisplay.
        { // Starts the missing-player-tag block.
            gameObject.AddComponent<PlayerTagDisplay>(); // Adds a PlayerTagDisplay so the player can show its name or tag.
        } // Ends the missing-player-tag block.

        if (photonView.IsMine) // Checks whether this player object belongs to the local client.
        { // Starts the local-player setup block.
            Local = this; // Stores this controller as the globally accessible local player controller.
            //fix camera jitter on local player // Notes why PhotonTransformView is disabled for the local player.
            PhotonTransformView photonTransform = GetComponent<PhotonTransformView>(); // Gets the PhotonTransformView used for network transform syncing.
            if (photonTransform != null) // Checks whether a PhotonTransformView exists on this player.
            { // Starts the PhotonTransformView disable block.
                photonTransform.enabled = false; // Disables network smoothing/sync on the local player to prevent local camera jitter.
            } // Ends the PhotonTransformView disable block.

            playerCamera.gameObject.SetActive(true); // Enables the local player's camera.
            Cursor.lockState = CursorLockMode.Locked; // Locks the cursor so mouse movement controls the camera.

            // Find the ObjectivePointer in the scene (on your Canvas/UI Manager) // Explains where the objective pointer is expected to live.
            if (ObjectivePointer.Instance != null) // Checks whether an ObjectivePointer singleton exists in the scene.
            { // Starts the objective-pointer setup block.
                ObjectivePointer.Instance.SetPlayer(transform); // Assigns this local player's transform as the objective pointer's tracked player.
                // Assign camera (get the child camera) // Explains how the camera is assigned to the objective pointer.
                ObjectivePointer.Instance.SetCamera(GetComponentInChildren<Camera>() ?? throw new System.Exception("No camera found on local player!")); // Assigns the child camera or throws an error if none exists.
                PuzzleStateManager.Instance?.SyncObjectivePointerToCurrentStage(); // Syncs the objective pointer to the current puzzle stage if the puzzle manager exists.
            } // Ends the objective-pointer setup block.

        } // Ends the local-player setup block.
        else // Runs for player objects owned by other clients.
        { // Starts the remote-player setup block.
            cameraRoot.gameObject.SetActive(false); // Disables other players' cameras so only the local camera renders.
        } // Ends the remote-player setup block.
    } // Ends the Start method body.

    private void Update() // Runs once per frame.
    { // Starts the Update method body.
     
        ToggleCursor(); // Checks whether the cursor lock state should be toggled.
        
        if (!photonView.IsMine || cursorUnlocked) return; // Stops movement and camera look for remote players or while the cursor is unlocked.

        HandleMovement(); // Reads movement input and moves the local player.
        HandleCameraLook(); // Reads mouse input and rotates the camera/player.
    } // Ends the Update method body.
    void ToggleCursor() // Toggles between locked gameplay cursor mode and unlocked cursor mode.
    { // Starts the ToggleCursor method body.
        if (Input.GetKeyDown(KeyCode.Q)) // Checks whether the player pressed Q this frame.
        { // Starts the Q-key toggle block.
            cursorUnlocked = !cursorUnlocked; // Flips the cursor unlocked state.
        } // Ends the Q-key toggle block.

        if (cursorUnlocked) // Checks whether the cursor should currently be free.
        { // Starts the cursor-unlocked block.
            Cursor.lockState = CursorLockMode.None; // Unlocks the cursor from the game window.
            Cursor.visible = true; // Shows the cursor.
        } // Ends the cursor-unlocked block.
        else // Runs when the cursor should be locked for gameplay.
        { // Starts the cursor-locked block.
            Cursor.lockState = CursorLockMode.Locked; // Locks the cursor to the game window for camera control.
            Cursor.visible = false; // Hides the cursor during gameplay.
        } // Ends the cursor-locked block.
    } // Ends the ToggleCursor method body.


    void HandleMovement() // Handles keyboard movement input and walking animation.
    { // Starts the HandleMovement method body.
        float h = Input.GetAxis("Horizontal"); // Reads horizontal movement input from A/D, left/right, or mapped controls.
        float v = Input.GetAxis("Vertical"); // Reads vertical movement input from W/S, up/down, or mapped controls.

        Vector3 forward = cameraRoot.forward; // Gets the camera root's forward direction.
        Vector3 right = cameraRoot.right; // Gets the camera root's right direction.
        forward.y = 0f; // Removes vertical tilt from the forward movement direction.
        right.y = 0f; // Removes vertical tilt from the sideways movement direction.
        forward.Normalize(); // Converts the forward direction to a length of one.
        right.Normalize(); // Converts the right direction to a length of one.

        Vector3 moveDir = (forward * v + right * h) * moveSpeed; // Builds the final movement direction from input and speed.
        controller.SimpleMove(moveDir); // Moves the CharacterController using Unity's simple gravity-aware movement.

        bool isMoving = moveDir.magnitude > 0.1f; // Checks whether the movement direction is strong enough to count as walking.
        animator.SetBool("isWalking", isMoving); // Updates the Animator's walking parameter.
    } // Ends the HandleMovement method body.

    void HandleCameraLook() // Handles mouse look for camera pitch and player yaw.
    { // Starts the HandleCameraLook method body.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity; // Reads horizontal mouse movement and applies sensitivity.
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity; // Reads vertical mouse movement and applies sensitivity.

        verticalRotation -= mouseY; // Adjusts camera pitch opposite the mouse Y direction.
        verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f); // Limits camera pitch so the player cannot look too far up or down.

        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // Applies the vertical camera rotation to the camera root.
        transform.Rotate(Vector3.up * mouseX); // Rotates the whole player left or right around the Y axis.
    } // Ends the HandleCameraLook method body.

    public void ResetCameraView() // Resets the local player's camera pitch back to a neutral view.
    { // Starts the ResetCameraView method body.
        if (!photonView.IsMine) return; // Stops remote player objects from resetting their cameras through this local call.

        verticalRotation = 0f; // Clears the stored vertical camera rotation.
        cameraRoot.localRotation = Quaternion.identity; // Resets the camera root rotation to no local rotation.
    } // Ends the ResetCameraView method body.
} // Ends the PlayerController class body.
