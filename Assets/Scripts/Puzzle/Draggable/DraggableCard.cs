// DraggableCard.cs (NETWORKED - shared ownership via RequestOwnership)

using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

public class DraggableCard : MonoBehaviour, // Declares this type.
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, // Runs this line of puzzle logic.
    IPunObservable, IPunOwnershipCallbacks // Runs this line of puzzle logic.
{ // Starts this code block.
    private const string PlayerTagPropertyKey = "PlayerTag"; // Declares a field, property, or member used by this script.
    private const string CardParentPathPropertyPrefix = "CardParentPath_"; // Declares a field, property, or member used by this script.
    private const string CardAnchoredPosPropertyPrefix = "CardAnchoredPos_"; // Declares a field, property, or member used by this script.
    private const string CardScalePropertyPrefix = "CardScale_"; // Declares a field, property, or member used by this script.
    private const string CardColorPropertyPrefix = "CardColor_"; // Declares a field, property, or member used by this script.

    public enum DraggableCardBehavior // Declares this type.
    { // Starts this code block.
        Auto, // Runs this line of puzzle logic.
        Default, // Runs this line of puzzle logic.
        Activity1, // Runs this line of puzzle logic.
        Activity1_2, // Runs this line of puzzle logic.
        Activity2, // Runs this line of puzzle logic.
        Activity6, // Runs this line of puzzle logic.
        Activity9 // Runs this line of puzzle logic.
    } // Ends this code block.

    [Header("UI References")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] protected Canvas canvas; // Exposes this private field in the Unity Inspector.

    [SerializeField] protected Transform panel1GridLayout;  // Right Slot All Symptoms
    [SerializeField] protected Transform panel2GridLayout;  // Left Slot Correct Symptom

    [Header("Networking Path Root (IMPORTANT)")] // Adds a labeled section in the Unity Inspector.
    [Tooltip("Root used to compute hierarchy paths for parenting sync. Usually your Canvas transform.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private Transform pathRoot; // Exposes this private field in the Unity Inspector.

    [Header("Puzzle Behavior")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private DraggableCardBehavior cardBehavior = DraggableCardBehavior.Auto; // Exposes this private field in the Unity Inspector.

    [Header("Puzzle Metadata")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] public string symptomName; // Exposes this private field in the Unity Inspector.
    [SerializeField] public Sprite backSprite; // Exposes this private field in the Unity Inspector.
    [SerializeField] public string stepName; // Exposes this private field in the Unity Inspector.
    [SerializeField] public string lapsesAnswer; // Exposes this private field in the Unity Inspector.
    [SerializeField] public bool isLocked; // Exposes this private field in the Unity Inspector.

    [Header("Drag Player Tag")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Vector2 dragTagSize = new Vector2(220f, 34f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Vector2 dragTagOffset = new Vector2(0f, -8f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color dragTagBackgroundColor = new Color(0f, 0f, 0f, 0.8f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color dragTagTextColor = Color.white; // Exposes this private field in the Unity Inspector.
    [SerializeField] private float dragTagFontSize = 22f; // Exposes this private field in the Unity Inspector.

    protected Image cardImage; // Declares a field, property, or member used by this script.
    [SerializeField] protected Sprite frontSprite; // Exposes this private field in the Unity Inspector.
    public string FrontImageName => frontSprite != null ? frontSprite.name : string.Empty; // Provides the source image name for analytics.

    protected RectTransform rectTransform; // Declares a field, property, or member used by this script.
    protected CanvasGroup canvasGroup; // Declares a field, property, or member used by this script.

    [HideInInspector] public Transform trueHome; // last successful grid parent
    public Transform lastSlotOrParent; // Declares a field, property, or member used by this script.

    protected Vector3 originalPosition; // Declares a field, property, or member used by this script.
    protected Transform originalParent; // Declares a field, property, or member used by this script.

    // Photon
    private PhotonView pv; // Declares a field, property, or member used by this script.

    // Local drag state
    private bool isDraggingLocal; // Declares a field, property, or member used by this script.
    private bool waitingForOwnership; // Declares a field, property, or member used by this script.

    // Remote state
    private Vector2 netAnchoredPos; // Declares a field, property, or member used by this script.
    private Vector2 netScale; // Declares a field, property, or member used by this script.
    private string netParentPath; // Declares a field, property, or member used by this script.
    private bool netDragging; // "someone is dragging" (owner sends)
    private string netDraggingPlayerName; // Declares a field, property, or member used by this script.

    private bool isFlipped; // Declares a field, property, or member used by this script.
    private bool acceptedThisDrag; // Declares a field, property, or member used by this script.
    private Transform dragStartParent; // Declares a field, property, or member used by this script.
    private Vector2 dragStartAnchoredPos; // Declares a field, property, or member used by this script.
    private Vector2 dragStartScale; // Declares a field, property, or member used by this script.

    private RectTransform dragTagRect; // Declares a field, property, or member used by this script.
    private Image dragTagBackground; // Declares a field, property, or member used by this script.
    private TextMeshProUGUI dragTagText; // Declares a field, property, or member used by this script.

    public DraggableCardBehavior Behavior => ResolveBehavior(); // Declares a field, property, or member used by this script.

    protected virtual void Awake() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        cardImage = GetComponent<Image>(); // Updates this value.
        rectTransform = GetComponent<RectTransform>(); // Updates this value.
        canvasGroup = GetComponent<CanvasGroup>(); // Updates this value.
        pv = GetComponent<PhotonView>(); // Updates this value.

        EnsureDragPlayerTag(); // Runs this line of puzzle logic.
        SetDragTagVisible(false, string.Empty); // Runs this line of puzzle logic.
    } // Ends this code block.

    protected virtual void OnEnable() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (pv != null) PhotonNetwork.AddCallbackTarget(this); // Checks this condition before continuing.
        ApplyPersistedRoomState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    protected virtual void OnDisable() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (pv != null) PhotonNetwork.RemoveCallbackTarget(this); // Checks this condition before continuing.
    } // Ends this code block.

    protected virtual void Start() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        EnsurePathRoot(); // Runs this line of puzzle logic.

        if (trueHome == null) // Checks this condition before continuing.
            trueHome = transform.parent; // Updates this value.

        originalParent = transform.parent; // Updates this value.
        originalPosition = transform.position; // Updates this value.

        // IMPORTANT:
        // Do NOT disable raycasts/interactable for non-owners here.
        // Otherwise other players can never click to request ownership.
        SetInteractableState(); // Runs this line of puzzle logic.

        if (Behavior == DraggableCardBehavior.Activity1) // Checks this condition before continuing.
            ShowFront(); // Runs this line of puzzle logic.

        ApplyPersistedRoomState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private bool IsNetworked => pv != null; // Declares a field, property, or member used by this script.

    private bool SomeoneElseDragging() // Declares a method used by this script.
    { // Starts this code block.
        // If remote says it's being dragged and we are not the owner, treat as locked.
        return IsNetworked && netDragging && !pv.IsMine; // Returns this result to the caller.
    } // Ends this code block.

    private void SetInteractableState() // Declares a method used by this script.
    { // Starts this code block.
        if (canvasGroup == null) return; // Checks this condition before continuing.

        // If someone else is dragging it, lock it locally.
        bool canInteract = !SomeoneElseDragging() && !waitingForOwnership; // Creates a local value used by the following logic.

        // Owner can always interact.
        if (!IsNetworked || pv.IsMine) canInteract = true; // Checks this condition before continuing.

        canvasGroup.interactable = canInteract; // Updates this value.
        canvasGroup.blocksRaycasts = canInteract; // Updates this value.
    } // Ends this code block.

    public virtual void OnBeginDrag(PointerEventData eventData) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (isLocked) return; // Checks this condition before continuing.

        RecordDragOrigin(); // Runs this line of puzzle logic.
        acceptedThisDrag = false; // Updates this value.

        if (SomeoneElseDragging()) return; // Checks this condition before continuing.

        if (IsNetworked && !pv.IsMine) // Checks this condition before continuing.
        { // Starts this code block.
            // Request ownership so this player becomes the sync authority
            waitingForOwnership = true; // Updates this value.
            SetInteractableState(); // Runs this line of puzzle logic.
            pv.RequestOwnership(); // Runs this line of puzzle logic.

            // Don’t start moving yet; wait until ownership transfers.
            return; // Returns this result to the caller.
        } // Ends this code block.

        BeginDragInternal(eventData); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void BeginDragInternal(PointerEventData eventData = null) // Declares a method used by this script.
    { // Starts this code block.
        waitingForOwnership = false; // Updates this value.
        isDraggingLocal = true; // Updates this value.

        canvasGroup.blocksRaycasts = false; // Updates this value.
        lastSlotOrParent = transform.parent; // Updates this value.

        // Move to top drag layer (canvas)
        SetParentLocal(canvas.transform, true); // Runs this line of puzzle logic.
        MoveToPointer(eventData); // Runs this line of puzzle logic.
        SetDragTagVisible(true, GetLocalPlayerDisplayName()); // Runs this line of puzzle logic.
    } // Ends this code block.

    public virtual void OnDrag(PointerEventData eventData) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        // Only the owner should move the card (so it stays authoritative)
        if (IsNetworked && !pv.IsMine) return; // Checks this condition before continuing.
        if (waitingForOwnership) return; // Checks this condition before continuing.
        if (SomeoneElseDragging()) return; // Checks this condition before continuing.

        MoveToPointer(eventData); // Runs this line of puzzle logic.
    } // Ends this code block.

    public virtual void OnEndDrag(PointerEventData eventData) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (!TryCompleteDrag()) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (transform.parent != canvas.transform && transform.parent != null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        switch (Behavior) // Chooses behavior based on this value.
        { // Starts this code block.
            case DraggableCardBehavior.Activity1: // Handles this switch case.
                HandleActivity1EndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            case DraggableCardBehavior.Activity1_2: // Handles this switch case.
                HandleActivity1_2EndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            case DraggableCardBehavior.Activity2: // Handles this switch case.
                HandleActivity2EndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            case DraggableCardBehavior.Activity6: // Handles this switch case.
                HandleActivity6EndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            case DraggableCardBehavior.Activity9: // Handles this switch case.
                HandleActivity9EndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            default: // Handles any unmatched switch case.
                HandleDefaultEndDrag(); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
        } // Ends this code block.
    } // Ends this code block.

    protected bool TryCompleteDrag() // Declares a method used by this script.
    { // Starts this code block.
        if (IsNetworked && !pv.IsMine) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        if (waitingForOwnership) // Checks this condition before continuing.
        { // Starts this code block.
            waitingForOwnership = false; // Updates this value.
            SetInteractableState(); // Runs this line of puzzle logic.
            return false; // Returns this result to the caller.
        } // Ends this code block.

        isDraggingLocal = false; // Updates this value.
        SetDragTagVisible(false, string.Empty); // Runs this line of puzzle logic.

        if (canvasGroup != null) // Checks this condition before continuing.
            canvasGroup.blocksRaycasts = true; // Updates this value.

        SetInteractableState(); // Runs this line of puzzle logic.
        return true; // Returns this result to the caller.
    } // Ends this code block.

    public void SnapBack() // Declares a method used by this script.
    { // Starts this code block.
        if (IsNetworked && !pv.IsMine) return; // Checks this condition before continuing.

        if (!panel1GridLayout.gameObject.activeSelf) // Checks this condition before continuing.
        { // Starts this code block.
            Transform slot = FindFirstEmptyChild(trueHome); // Creates a local value used by the following logic.

            if (slot != null) // Checks this condition before continuing.
            { // Starts this code block.
                SnapTo(slot, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    protected virtual void SnapTo(Transform parent, float xPos, float yPos, float xScale, float yScale) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        ApplyLayoutState(parent, new Vector2(xPos, yPos), new Vector2(xScale, yScale)); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ApplyLayoutState(Transform parent, Vector2 anchoredPosition, Vector2 scale) // Declares a method used by this script.
    { // Starts this code block.
        if (parent == null) return; // Checks this condition before continuing.

        SetParentLocal(parent, false); // Runs this line of puzzle logic.
        rectTransform.anchoredPosition = anchoredPosition; // Updates this value.
        rectTransform.localScale = scale; // Updates this value.

        if (parent is RectTransform parentRect) // Checks this condition before continuing.
        { // Starts this code block.
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect); // Runs this line of puzzle logic.
        } // Ends this code block.

        PersistLayoutStateIfAuthoritative(); // Runs this line of puzzle logic.
    } // Ends this code block.

    protected void SetParentLocal(Transform newParent, bool worldPositionStays = true) // Declares a method used by this script.
    { // Starts this code block.
        transform.SetParent(newParent, worldPositionStays); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void MoveToPointer(PointerEventData eventData) // Declares a method used by this script.
    { // Starts this code block.
        Vector2 pointerPosition = eventData != null ? eventData.position : (Vector2)Input.mousePosition; // Creates a local value used by the following logic.
        Camera eventCamera = eventData != null ? eventData.pressEventCamera : null; // Creates a local value used by the following logic.
        if (eventCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) // Checks this condition before continuing.
            eventCamera = canvas.worldCamera; // Updates this value.

        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null; // Runs this line of puzzle logic.
        if (canvasRect == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle( // Checks this condition before continuing.
                canvasRect, // Runs this line of puzzle logic.
                pointerPosition, // Runs this line of puzzle logic.
                eventCamera, // Runs this line of puzzle logic.
                out Vector3 worldPoint)) // Runs this line of puzzle logic.
        { // Starts this code block.
            rectTransform.position = worldPoint; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    protected Transform FindFirstEmptyChild(Transform gridParent) // Declares a method used by this script.
    { // Starts this code block.
        foreach (Transform child in gridParent) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (child.childCount == 0) // Checks this condition before continuing.
                return child; // Returns this result to the caller.
        } // Ends this code block.
        return null; // Returns this result to the caller.
    } // Ends this code block.

    protected bool IsPointerOverPanel(string panelTag) // Declares a method used by this script.
    { // Starts this code block.
        var results = new List<RaycastResult>(); // Creates a local value used by the following logic.
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition }; // Creates a local value used by the following logic.
        EventSystem.current.RaycastAll(eventData, results); // Runs this line of puzzle logic.

        foreach (var result in results) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (result.gameObject.CompareTag(panelTag)) // Checks this condition before continuing.
                return true; // Returns this result to the caller.
        } // Ends this code block.
        return false; // Returns this result to the caller.
    } // Ends this code block.

    public virtual void OnPointerClick(PointerEventData eventData) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (SomeoneElseDragging()) return; // Checks this condition before continuing.
        if (IsNetworked && !pv.IsMine && waitingForOwnership) return; // Checks this condition before continuing.

        if (eventData.button == PointerEventData.InputButton.Right) // Checks this condition before continuing.
        { // Starts this code block.
            Sprite spriteToShow = (Behavior == DraggableCardBehavior.Activity1 && isFlipped && backSprite != null) // Runs this line of puzzle logic.
                ? backSprite // Runs this line of puzzle logic.
                : frontSprite; // Runs this line of puzzle logic.
            CardPopupManager.Instance.ShowCard(spriteToShow); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public virtual void ResetCard() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (IsNetworked && !pv.IsMine) return; // Checks this condition before continuing.

        SetParentLocal(originalParent, true); // Runs this line of puzzle logic.
        transform.position = originalPosition; // Updates this value.

        ApplyCardColor(Color.white); // Runs this line of puzzle logic.

        acceptedThisDrag = false; // Updates this value.

        if (Behavior == DraggableCardBehavior.Activity1) // Checks this condition before continuing.
        { // Starts this code block.
            isFlipped = false; // Updates this value.
            ShowFront(); // Runs this line of puzzle logic.
        } // Ends this code block.

        if (Behavior == DraggableCardBehavior.Activity9) // Checks this condition before continuing.
        { // Starts this code block.
            isLocked = false; // Updates this value.
        } // Ends this code block.

        SetDragTagVisible(false, string.Empty); // Runs this line of puzzle logic.
        PersistLayoutStateIfAuthoritative(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void SetCardColorAndSync(Color color) // Declares a method used by this script.
    { // Starts this code block.
        ApplyCardColor(color); // Runs this line of puzzle logic.

        if (IsNetworked && PhotonNetwork.InRoom) // Checks this condition before continuing.
            pv.RPC(nameof(RPC_SetCardColor), RpcTarget.Others, color.r, color.g, color.b, color.a); // Runs this line of puzzle logic.

        PersistColorState(color); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ResetCardColorAndSync() // Declares a method used by this script.
    { // Starts this code block.
        SetCardColorAndSync(Color.white); // Runs this line of puzzle logic.
    } // Ends this code block.

    // ---------------------------
    // Photon Sync
    // ---------------------------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // Declares a method used by this script.
    { // Starts this code block.
        if (pv == null) return; // Checks this condition before continuing.

        if (stream.IsWriting) // Checks this condition before continuing.
        { // Starts this code block.
            string parentPath = GetPathFromRoot(transform.parent); // Creates a local value used by the following logic.
            stream.SendNext(isDraggingLocal); // Runs this line of puzzle logic.
            stream.SendNext(isDraggingLocal ? GetLocalPlayerDisplayName() : string.Empty); // Runs this line of puzzle logic.
            stream.SendNext(parentPath); // Runs this line of puzzle logic.
            stream.SendNext(rectTransform.anchoredPosition); // Runs this line of puzzle logic.
            stream.SendNext((Vector2)rectTransform.localScale); // Runs this line of puzzle logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            netDragging = (bool)stream.ReceiveNext(); // Updates this value.
            netDraggingPlayerName = (string)stream.ReceiveNext(); // Updates this value.
            netParentPath = (string)stream.ReceiveNext(); // Updates this value.
            netAnchoredPos = (Vector2)stream.ReceiveNext(); // Updates this value.
            netScale = (Vector2)stream.ReceiveNext(); // Updates this value.

            if (!pv.IsMine) // Checks this condition before continuing.
            { // Starts this code block.
                ApplyRemoteState(); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void ApplyRemoteState() // Declares a method used by this script.
    { // Starts this code block.
        Transform parent = FindByPath(netParentPath); // Creates a local value used by the following logic.
        if (parent != null && transform.parent != parent) // Checks this condition before continuing.
        { // Starts this code block.
            SetParentLocal(parent, false); // Runs this line of puzzle logic.
        } // Ends this code block.

        rectTransform.anchoredPosition = netAnchoredPos; // Updates this value.
        rectTransform.localScale = netScale; // Updates this value.

        if (parent is RectTransform parentRect) // Checks this condition before continuing.
        { // Starts this code block.
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect); // Runs this line of puzzle logic.
        } // Ends this code block.

        // Lock/unlock interaction based on whether someone else is dragging
        SetInteractableState(); // Runs this line of puzzle logic.
        SetDragTagVisible(netDragging, netDraggingPlayerName); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void PersistLayoutStateIfAuthoritative() // Declares a method used by this script.
    { // Starts this code block.
        if (!IsNetworked || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || !pv.IsMine) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        EnsurePathRoot(); // Runs this line of puzzle logic.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildCardParentPathKey()] = GetPathFromRoot(transform.parent), // Adds an attribute that changes how Unity or C# treats the next declaration.
            [BuildCardAnchoredPosKey()] = EncodeVector2(rectTransform.anchoredPosition), // Adds an attribute that changes how Unity or C# treats the next declaration.
            [BuildCardScaleKey()] = EncodeVector2(rectTransform.localScale) // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void PersistColorState(Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (!IsNetworked || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildCardColorKey()] = EncodeColor(color) // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyPersistedRoomState() // Declares a method used by this script.
    { // Starts this code block.
        if (!IsNetworked || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        EnsurePathRoot(); // Runs this line of puzzle logic.

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties; // Creates a local value used by the following logic.

        string parentPathKey = BuildCardParentPathKey(); // Creates a local value used by the following logic.
        string anchoredPosKey = BuildCardAnchoredPosKey(); // Creates a local value used by the following logic.
        string scaleKey = BuildCardScaleKey(); // Creates a local value used by the following logic.

        if (properties.ContainsKey(parentPathKey) && // Checks this condition before continuing.
            properties.ContainsKey(anchoredPosKey) && // Runs this line of puzzle logic.
            properties.ContainsKey(scaleKey) && // Runs this line of puzzle logic.
            properties[parentPathKey] is string parentPath && // Runs this line of puzzle logic.
            TryDecodeVector2(properties[anchoredPosKey], out Vector2 anchoredPosition) && // Runs this line of puzzle logic.
            TryDecodeVector2(properties[scaleKey], out Vector2 scale)) // Runs this line of puzzle logic.
        { // Starts this code block.
            Transform parent = FindByPath(parentPath); // Creates a local value used by the following logic.
            if (parent != null) // Checks this condition before continuing.
            { // Starts this code block.
                SetParentLocal(parent, false); // Runs this line of puzzle logic.
                rectTransform.anchoredPosition = anchoredPosition; // Updates this value.
                rectTransform.localScale = scale; // Updates this value.

                if (parent is RectTransform parentRect) // Checks this condition before continuing.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        string colorKey = BuildCardColorKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(colorKey) && TryDecodeColor(properties[colorKey], out Color color)) // Checks this condition before continuing.
            ApplyCardColor(color); // Runs this line of puzzle logic.

        SetInteractableState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private string BuildCardParentPathKey() => $"{CardParentPathPropertyPrefix}{pv.ViewID}"; // Declares a method used by this script.
    private string BuildCardAnchoredPosKey() => $"{CardAnchoredPosPropertyPrefix}{pv.ViewID}"; // Declares a method used by this script.
    private string BuildCardScaleKey() => $"{CardScalePropertyPrefix}{pv.ViewID}"; // Declares a method used by this script.
    private string BuildCardColorKey() => $"{CardColorPropertyPrefix}{pv.ViewID}"; // Declares a method used by this script.

    private static float[] EncodeVector2(Vector2 value) => new[] { value.x, value.y }; // Declares a method used by this script.

    private static bool TryDecodeVector2(object value, out Vector2 vector) // Declares a method used by this script.
    { // Starts this code block.
        if (value is float[] xy && xy.Length >= 2) // Checks this condition before continuing.
        { // Starts this code block.
            vector = new Vector2(xy[0], xy[1]); // Updates this value.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        if (value is Vector2 storedVector) // Checks this condition before continuing.
        { // Starts this code block.
            vector = storedVector; // Updates this value.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        vector = Vector2.zero; // Updates this value.
        return false; // Returns this result to the caller.
    } // Ends this code block.

    private static float[] EncodeColor(Color color) => new[] { color.r, color.g, color.b, color.a }; // Declares a method used by this script.

    private static bool TryDecodeColor(object value, out Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (value is float[] rgba && rgba.Length >= 4) // Checks this condition before continuing.
        { // Starts this code block.
            color = new Color(rgba[0], rgba[1], rgba[2], rgba[3]); // Creates a local value used by the following logic.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        if (value is Color storedColor) // Checks this condition before continuing.
        { // Starts this code block.
            color = storedColor; // Creates a local value used by the following logic.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        color = Color.white; // Creates a local value used by the following logic.
        return false; // Returns this result to the caller.
    } // Ends this code block.

    private void EnsurePathRoot() // Declares a method used by this script.
    { // Starts this code block.
        if (pathRoot == null && canvas != null) // Checks this condition before continuing.
            pathRoot = canvas.transform; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetCardColor(float r, float g, float b, float a) // Declares a method used by this script.
    { // Starts this code block.
        ApplyCardColor(new Color(r, g, b, a)); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyCardColor(Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (cardImage != null) // Checks this condition before continuing.
            cardImage.color = color; // Updates this value.
    } // Ends this code block.

    private string GetPathFromRoot(Transform target) // Declares a method used by this script.
    { // Starts this code block.
        if (pathRoot == null || target == null) return ""; // Checks this condition before continuing.
        if (target == pathRoot) return ""; // Checks this condition before continuing.

        var stack = new Stack<string>(); // Creates a local value used by the following logic.
        Transform cur = target; // Creates a local value used by the following logic.

        while (cur != null && cur != pathRoot) // Repeats this block while the condition stays true.
        { // Starts this code block.
            stack.Push(BuildPathSegment(cur)); // Runs this line of puzzle logic.
            cur = cur.parent; // Updates this value.
        } // Ends this code block.

        if (cur != pathRoot) return ""; // Checks this condition before continuing.
        return string.Join("/", stack.ToArray()); // Returns this result to the caller.
    } // Ends this code block.

    private Transform FindByPath(string path) // Declares a method used by this script.
    { // Starts this code block.
        if (pathRoot == null) return null; // Checks this condition before continuing.
        if (string.IsNullOrEmpty(path)) return pathRoot; // Checks this condition before continuing.

        Transform current = pathRoot; // Creates a local value used by the following logic.
        string[] segments = path.Split('/'); // Runs this line of puzzle logic.

        foreach (string segment in segments) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (!TryParsePathSegment(segment, out string childName, out int sameNameIndex)) // Checks this condition before continuing.
                return null; // Returns this result to the caller.

            current = FindChildByNameIndex(current, childName, sameNameIndex); // Updates this value.
            if (current == null) // Checks this condition before continuing.
                return null; // Returns this result to the caller.
        } // Ends this code block.

        return current; // Returns this result to the caller.
    } // Ends this code block.

    private string BuildPathSegment(Transform target) // Declares a method used by this script.
    { // Starts this code block.
        if (target == null) // Checks this condition before continuing.
            return string.Empty; // Returns this result to the caller.

        int sameNameIndex = 0; // Creates a local value used by the following logic.
        Transform parent = target.parent; // Creates a local value used by the following logic.

        if (parent != null) // Checks this condition before continuing.
        { // Starts this code block.
            for (int i = 0; i < parent.childCount; i++) // Repeats this block for a counted loop.
            { // Starts this code block.
                Transform sibling = parent.GetChild(i); // Creates a local value used by the following logic.
                if (sibling.name != target.name) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                if (sibling == target) // Checks this condition before continuing.
                    break; // Exits the current loop or switch.

                sameNameIndex++; // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        return $"{target.name}|{sameNameIndex}"; // Returns this result to the caller.
    } // Ends this code block.

    private bool TryParsePathSegment(string segment, out string childName, out int sameNameIndex) // Declares a method used by this script.
    { // Starts this code block.
        childName = segment; // Updates this value.
        sameNameIndex = 0; // Updates this value.

        if (string.IsNullOrEmpty(segment)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        int separatorIndex = segment.LastIndexOf('|'); // Creates a local value used by the following logic.
        if (separatorIndex < 0) // Checks this condition before continuing.
            return true; // Returns this result to the caller.

        childName = segment.Substring(0, separatorIndex); // Updates this value.
        string indexText = segment.Substring(separatorIndex + 1); // Creates a local value used by the following logic.

        if (string.IsNullOrEmpty(childName)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        return int.TryParse(indexText, out sameNameIndex); // Returns this result to the caller.
    } // Ends this code block.

    private Transform FindChildByNameIndex(Transform parent, string childName, int sameNameIndex) // Declares a method used by this script.
    { // Starts this code block.
        if (parent == null) // Checks this condition before continuing.
            return null; // Returns this result to the caller.

        int currentIndex = 0; // Creates a local value used by the following logic.

        for (int i = 0; i < parent.childCount; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            Transform child = parent.GetChild(i); // Creates a local value used by the following logic.
            if (child.name != childName) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            if (currentIndex == sameNameIndex) // Checks this condition before continuing.
                return child; // Returns this result to the caller.

            currentIndex++; // Runs this line of puzzle logic.
        } // Ends this code block.

        return null; // Returns this result to the caller.
    } // Ends this code block.

    // ---------------------------
    // Ownership callbacks
    // ---------------------------
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer) // Declares a method used by this script.
    { // Starts this code block.
        if (pv == null || targetView != pv || requestingPlayer == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (pv.Owner == requestingPlayer) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        // Always allow the next player to take control of draggable puzzle cards.
        if (pv.IsMine || PhotonNetwork.IsMasterClient) // Checks this condition before continuing.
        { // Starts this code block.
            pv.TransferOwnership(requestingPlayer); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner) // Declares a method used by this script.
    { // Starts this code block.
        if (pv == null || targetView != pv) return; // Checks this condition before continuing.

        // If we requested it, start dragging now
        if (pv.IsMine && waitingForOwnership) // Checks this condition before continuing.
        { // Starts this code block.
            BeginDragInternal(); // Runs this line of puzzle logic.
        } // Ends this code block.

        SetInteractableState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest) // Declares a method used by this script.
    { // Starts this code block.
        if (pv == null || targetView != pv) return; // Checks this condition before continuing.

        waitingForOwnership = false; // Updates this value.
        SetInteractableState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void MarkAcceptedThisDrag() // Declares a method used by this script.
    { // Starts this code block.
        acceptedThisDrag = true; // Updates this value.
    } // Ends this code block.

    public void RevertToDragOrigin() // Declares a method used by this script.
    { // Starts this code block.
        if (dragStartParent != null) // Checks this condition before continuing.
        { // Starts this code block.
            ApplyLayoutState(dragStartParent, dragStartAnchoredPos, dragStartScale); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        switch (Behavior) // Chooses behavior based on this value.
        { // Starts this code block.
            case DraggableCardBehavior.Activity2: // Handles this switch case.
            case DraggableCardBehavior.Activity9: // Handles this switch case.
                SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            case DraggableCardBehavior.Activity6: // Handles this switch case.
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.6409343f); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
            default: // Handles any unmatched switch case.
                SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
                break; // Exits the current loop or switch.
        } // Ends this code block.
    } // Ends this code block.

    public void ShowFront() // Declares a method used by this script.
    { // Starts this code block.
        if (cardImage != null && frontSprite != null) // Checks this condition before continuing.
        { // Starts this code block.
            isFlipped = false; // Updates this value.
            cardImage.sprite = frontSprite; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    public void ShowBack() // Declares a method used by this script.
    { // Starts this code block.
        if (cardImage != null && backSprite != null) // Checks this condition before continuing.
        { // Starts this code block.
            isFlipped = true; // Updates this value.
            cardImage.sprite = backSprite; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    private DraggableCardBehavior ResolveBehavior() // Declares a method used by this script.
    { // Starts this code block.
        if (cardBehavior != DraggableCardBehavior.Auto) // Checks this condition before continuing.
            return cardBehavior; // Returns this result to the caller.

        return DraggableCardBehavior.Default; // Returns this result to the caller.
    } // Ends this code block.

    private void RecordDragOrigin() // Declares a method used by this script.
    { // Starts this code block.
        dragStartParent = transform.parent; // Updates this value.
        dragStartAnchoredPos = rectTransform.anchoredPosition; // Updates this value.
        dragStartScale = rectTransform.localScale; // Updates this value.
    } // Ends this code block.

    private void HandleDefaultEndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout; // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            if (emptySlot != null) // Checks this condition before continuing.
            { // Starts this code block.
                SnapTo(emptySlot, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void HandleActivity1EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool droppedOnPanel1 = IsPointerOverPanel("Panel1"); // Creates a local value used by the following logic.

        if (!acceptedThisDrag && !droppedOnPanel1) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.Log("[DraggableCard] Activity1 dropped outside a slot and Panel1 -> revert to drag origin"); // Writes a diagnostic message to the Unity console.
            RevertToDragOrigin(); // Runs this line of puzzle logic.
            ResetCardColor(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.IsChildOf(panel2GridLayout); // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1); // Runs this line of puzzle logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.
    private void HandleActivity1_2EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout; // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1); // Runs this line of puzzle logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.
    private void HandleActivity2EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout; // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            if (emptySlot != null) // Checks this condition before continuing.
            { // Starts this code block.
                SnapTo(emptySlot, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SnapTo(panel2GridLayout, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void HandleActivity6EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout; // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 150, -150, 0.6409343f, 0.6409343f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            if (emptySlot != null) // Checks this condition before continuing.
            { // Starts this code block.
                SnapTo(emptySlot, 225, -230, 1.55f, 1.55f); // Runs this line of puzzle logic.
                rectTransform.localScale = new Vector2(1.55f, 1.55f); // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SnapTo(panel2GridLayout, 150, -150, 1.55f, 1.55f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.6409343f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void HandleActivity9EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout; // Creates a local value used by the following logic.

        if (IsPointerOverPanel("Panel1") && fromPanel2) // Checks this condition before continuing.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (IsPointerOverPanel("Panel2") && fromPanel2) // Runs when the previous condition was false.
        { // Starts this code block.
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout); // Creates a local value used by the following logic.
            if (emptySlot != null) // Checks this condition before continuing.
            { // Starts this code block.
                SnapTo(emptySlot, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SnapTo(panel2GridLayout, 150, -150, 0.66f, 0.66f); // Runs this line of puzzle logic.
                rectTransform.localScale = Vector2.one; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f); // Runs this line of puzzle logic.
        } // Ends this code block.

        ResetCardColor(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ResetCardColor() // Declares a method used by this script.
    { // Starts this code block.
        if (cardImage != null) // Checks this condition before continuing.
            cardImage.color = Color.white; // Updates this value.
    } // Ends this code block.

    private void EnsureDragPlayerTag() // Declares a method used by this script.
    { // Starts this code block.
        if (dragTagRect != null) // Checks this condition before continuing.
        { // Starts this code block.
            return; // Returns this result to the caller.
        } // Ends this code block.

        GameObject tagObject = new GameObject("DragPlayerTag", typeof(RectTransform), typeof(Image)); // Creates a local value used by the following logic.
        tagObject.transform.SetParent(transform, false); // Runs this line of puzzle logic.

        dragTagRect = tagObject.GetComponent<RectTransform>(); // Updates this value.
        dragTagRect.anchorMin = new Vector2(0.5f, 0f); // Updates this value.
        dragTagRect.anchorMax = new Vector2(0.5f, 0f); // Updates this value.
        dragTagRect.pivot = new Vector2(0.5f, 1f); // Updates this value.
        dragTagRect.anchoredPosition = dragTagOffset; // Updates this value.
        dragTagRect.sizeDelta = dragTagSize; // Updates this value.

        dragTagBackground = tagObject.GetComponent<Image>(); // Updates this value.
        dragTagBackground.color = dragTagBackgroundColor; // Updates this value.
        dragTagBackground.raycastTarget = false; // Updates this value.

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); // Creates a local value used by the following logic.
        textObject.transform.SetParent(tagObject.transform, false); // Runs this line of puzzle logic.

        RectTransform textRect = textObject.GetComponent<RectTransform>(); // Runs this line of puzzle logic.
        textRect.anchorMin = Vector2.zero; // Updates this value.
        textRect.anchorMax = Vector2.one; // Updates this value.
        textRect.offsetMin = new Vector2(10f, 4f); // Updates this value.
        textRect.offsetMax = new Vector2(-10f, -4f); // Updates this value.

        dragTagText = textObject.GetComponent<TextMeshProUGUI>(); // Updates this value.
        dragTagText.alignment = TextAlignmentOptions.Center; // Updates this value.
        dragTagText.fontSize = dragTagFontSize; // Updates this value.
        dragTagText.color = dragTagTextColor; // Updates this value.
        dragTagText.enableWordWrapping = false; // Updates this value.
        dragTagText.overflowMode = TextOverflowModes.Ellipsis; // Updates this value.
        dragTagText.raycastTarget = false; // Updates this value.
    } // Ends this code block.

    private void SetDragTagVisible(bool visible, string playerName) // Declares a method used by this script.
    { // Starts this code block.
        if (dragTagRect == null) // Checks this condition before continuing.
        { // Starts this code block.
            return; // Returns this result to the caller.
        } // Ends this code block.

        bool shouldShow = visible && !string.IsNullOrWhiteSpace(playerName); // Creates a local value used by the following logic.

        if (dragTagText != null) // Checks this condition before continuing.
        { // Starts this code block.
            dragTagText.text = shouldShow ? playerName.Trim() : string.Empty; // Updates this value.
        } // Ends this code block.

        if (dragTagRect.gameObject.activeSelf != shouldShow) // Checks this condition before continuing.
        { // Starts this code block.
            dragTagRect.gameObject.SetActive(shouldShow); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    private string GetLocalPlayerDisplayName() // Declares a method used by this script.
    { // Starts this code block.
        return GetPlayerDisplayName(PhotonNetwork.LocalPlayer); // Returns this result to the caller.
    } // Ends this code block.

    private string GetPlayerDisplayName(Player player) // Declares a method used by this script.
    { // Starts this code block.
        if (player == null) // Checks this condition before continuing.
        { // Starts this code block.
            return "Player"; // Returns this result to the caller.
        } // Ends this code block.

        if (player.CustomProperties != null && // Checks this condition before continuing.
            player.CustomProperties.TryGetValue(PlayerTagPropertyKey, out object tagValue) && // Runs this line of puzzle logic.
            tagValue is string tag && // Runs this line of puzzle logic.
            !string.IsNullOrWhiteSpace(tag)) // Runs this line of puzzle logic.
        { // Starts this code block.
            return tag.Trim(); // Returns this result to the caller.
        } // Ends this code block.

        if (!string.IsNullOrWhiteSpace(player.NickName)) // Checks this condition before continuing.
        { // Starts this code block.
            return player.NickName.Trim(); // Returns this result to the caller.
        } // Ends this code block.

        return $"Player {player.ActorNumber}"; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
