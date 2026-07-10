using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Vector3, Mathf, and Transform.
using Photon.Pun; // Imports Photon networking types such as MonoBehaviourPun, PhotonStream, and PunRPC.

[RequireComponent(typeof(PhotonView))] // Ensures this GameObject has a PhotonView component.
public class MovementTargetForward : MonoBehaviourPun, IPunObservable, IHighlightable // Defines a networked object that moves forward once and can be highlighted.
{ // Opens the MovementTargetForward class body.
    [Header("Movement Settings")] // Groups movement fields in the Inspector.
    [SerializeField] private float moveDistance = 3f;   // Stores how far to move forward in local forward direction.
    [SerializeField] private float moveSpeed = 2f;      // Stores the movement speed in units per second.

    // Runtime/networked state // Marks fields that track movement state during play and synchronization.
    private bool isMoving = false; // Tracks whether the object is currently moving.
    private bool hasReachedTarget = false; // Tracks whether the one-shot movement has already completed.
    private Vector3 moveFrom; // Stores the world position where the current move starts.
    private Vector3 moveTo; // Stores the world position where the current move ends.
    private float moveDuration; // Stores how long the movement should take in seconds.
    private double moveStartTime; // Stores the Photon network timestamp when movement began.

    private HighlightEffect highlightEffect; // Caches the optional HighlightEffect used by IHighlightable methods.

    private void Start() // Runs Unity initialization before the first Update.
    { // Opens the Start method body.
        highlightEffect = GetComponent<HighlightEffect>(); // Finds the HighlightEffect on the same GameObject, if one exists.
    } // Closes the Start method body.

    // ---- IInteractable ---- // Marks the interaction method section.
    public void Interact() // Starts the one-shot forward movement when the object is interacted with.
    { // Opens the Interact method body.
        // Ignore if already moving or already completed the one-shot move // Explains the guard below.
        if (isMoving || hasReachedTarget) return; // Stops if the object is already moving or has finished its one allowed move.

        if (!photonView.AmOwner) photonView.RequestOwnership(); // Requests Photon ownership before starting the networked move.

        Vector3 currentPos = transform.position; // Stores the object's current world position.
        Vector3 targetPos = currentPos + transform.forward * moveDistance; // Calculates the target position forward from the current position.

        float duration = moveSpeed > 0f // Calculates movement duration only when speed is positive.
            ? Vector3.Distance(currentPos, targetPos) / moveSpeed // Uses distance divided by speed for linear travel time.
            : 0f; // Uses zero duration when movement speed is not positive.

        photonView.RPC(nameof(RPC_BeginMove), RpcTarget.All, currentPos, targetPos, Mathf.Max(0.0001f, duration), PhotonNetwork.Time); // Tells every client to begin moving with the same positions, duration, and start time.
    } // Closes the Interact method body.

    [PunRPC] // Allows Photon to call this method through an RPC.
    private void RPC_BeginMove(Vector3 from, Vector3 to, float duration, double startStamp) // Receives synchronized movement start data.
    { // Opens the movement RPC method body.
        if (hasReachedTarget) return; // Stops if this client already completed the one-shot movement.

        moveFrom = from; // Stores the synchronized start position.
        moveTo = to; // Stores the synchronized end position.
        moveDuration = duration; // Stores the synchronized movement duration.
        moveStartTime = startStamp; // Stores the synchronized Photon start timestamp.
        isMoving = true; // Marks this object as currently moving.
    } // Closes the movement RPC method body.

    private void Update() // Runs once per frame.
    { // Opens the Update method body.
        if (!isMoving) return; // Stops if there is no active movement to update.

        double now = PhotonNetwork.Time; // Reads the current Photon network time.
        float t = (float)((now - moveStartTime) / moveDuration); // Calculates normalized movement progress from network time.

        if (t < 1f) // Checks whether the movement is still in progress.
        { // Opens the in-progress movement block.
            // strictly linear motion // Notes that the interpolation is not eased.
            transform.position = Vector3.LerpUnclamped(moveFrom, moveTo, t); // Moves the object linearly between start and end positions.
        } // Closes the in-progress movement block.
        else // Handles movement completion.
        { // Opens the completed movement block.
            transform.position = moveTo; // Snaps exactly to the final target position.
            isMoving = false; // Marks movement as finished.
            hasReachedTarget = true; // Prevents further interactions after the one-shot move.
        } // Closes the completed movement block.
    } // Closes the Update method body.

    // ---- Minimal state sync for late joiners/snapshots ---- // Marks the Photon serialization section.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // Sends or receives minimal movement state over Photon.
    { // Opens the Photon serialization method body.
        if (stream.IsWriting) // Checks whether this client is writing state to the stream.
        { // Opens the state-writing block.
            stream.SendNext(isMoving); // Sends whether the object is currently moving.
            stream.SendNext(hasReachedTarget); // Sends whether the object has completed its one-shot move.
            stream.SendNext(transform.position); // Sends the object's current world position.
        } // Closes the state-writing block.
        else // Handles clients reading state from the stream.
        { // Opens the state-reading block.
            isMoving = (bool)stream.ReceiveNext(); // Receives and stores the moving flag.
            hasReachedTarget = (bool)stream.ReceiveNext(); // Receives and stores the completed flag.
            Vector3 netPos = (Vector3)stream.ReceiveNext(); // Receives the latest networked world position.

            if (!isMoving) transform.position = netPos; // Snaps to the network position when idle.
        } // Closes the state-reading block.
    } // Closes the Photon serialization method body.

    // ---- IHighlightable (using your HighlightEffect) ---- // Marks the highlight interface section.
    public void Highlight() => highlightEffect?.Highlight(); // Turns on the cached highlight effect if it exists.
    public void Unhighlight() => highlightEffect?.Unhighlight(); // Turns off the cached highlight effect if it exists.
} // Closes the MovementTargetForward class body.
