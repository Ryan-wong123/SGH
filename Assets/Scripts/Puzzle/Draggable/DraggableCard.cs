// DraggableCard.cs (NETWORKED - shared ownership via RequestOwnership)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class DraggableCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
    IPunObservable, IPunOwnershipCallbacks
{
    public enum DraggableCardBehavior
    {
        Auto,
        Default,
        Activity1,
        Activity2,
        Activity6,
        Activity9
    }

    [Header("UI References")]
    [SerializeField] protected Canvas canvas;

    [SerializeField] protected Transform panel1GridLayout;  // Right Slot All Symptoms
    [SerializeField] protected Transform panel2GridLayout;  // Left Slot Correct Symptom
    [SerializeField] protected Transform panel3GridLayout;

    [Header("Networking Path Root (IMPORTANT)")]
    [Tooltip("Root used to compute hierarchy paths for parenting sync. Usually your Canvas transform.")]
    [SerializeField] private Transform pathRoot;

    [Header("Puzzle Behavior")]
    [SerializeField] private DraggableCardBehavior cardBehavior = DraggableCardBehavior.Auto;

    [Header("Puzzle Metadata")]
    [SerializeField] public string symptomName;
    [SerializeField] public Sprite backSprite;
    [SerializeField] public string stepName;
    [SerializeField] public string lapsesAnswer;
    [SerializeField] public bool isLocked;

    protected Image cardImage;
    [SerializeField] protected Sprite frontSprite;

    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;

    [HideInInspector] public Transform trueHome; // last successful grid parent
    public Transform lastSlotOrParent;

    protected Vector3 originalPosition;
    protected Transform originalParent;

    // Photon
    private PhotonView pv;

    // Local drag state
    private bool isDraggingLocal;
    private bool waitingForOwnership;

    // Remote state
    private Vector2 netAnchoredPos;
    private Vector2 netScale;
    private string netParentPath;
    private bool netDragging; // "someone is dragging" (owner sends)

    private bool isFlipped;
    private bool acceptedThisDrag;
    private Transform dragStartParent;
    private Vector2 dragStartAnchoredPos;
    private Vector2 dragStartScale;

    public DraggableCardBehavior Behavior => ResolveBehavior();

    protected virtual void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        pv = GetComponent<PhotonView>();
    }

    protected virtual void OnEnable()
    {
        if (pv != null) PhotonNetwork.AddCallbackTarget(this);
    }

    protected virtual void OnDisable()
    {
        if (pv != null) PhotonNetwork.RemoveCallbackTarget(this);
    }

    protected virtual void Start()
    {
        if (pathRoot == null && canvas != null) pathRoot = canvas.transform;

        if (trueHome == null)
            trueHome = transform.parent;

        originalParent = transform.parent;
        originalPosition = transform.position;

        // IMPORTANT:
        // Do NOT disable raycasts/interactable for non-owners here.
        // Otherwise other players can never click to request ownership.
        SetInteractableState();

        if (Behavior == DraggableCardBehavior.Activity1)
            ShowFront();
    }

    private bool IsNetworked => pv != null;

    private bool SomeoneElseDragging()
    {
        // If remote says it's being dragged and we are not the owner, treat as locked.
        return IsNetworked && netDragging && !pv.IsMine;
    }

    private void SetInteractableState()
    {
        if (canvasGroup == null) return;

        // If someone else is dragging it, lock it locally.
        bool canInteract = !SomeoneElseDragging() && !waitingForOwnership;

        // Owner can always interact.
        if (!IsNetworked || pv.IsMine) canInteract = true;

        canvasGroup.interactable = canInteract;
        canvasGroup.blocksRaycasts = canInteract;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        RecordDragOrigin();
        acceptedThisDrag = false;

        if (SomeoneElseDragging()) return;

        if (IsNetworked && !pv.IsMine)
        {
            // Request ownership so this player becomes the sync authority
            waitingForOwnership = true;
            SetInteractableState();
            pv.RequestOwnership();

            // Don’t start moving yet; wait until ownership transfers.
            return;
        }

        BeginDragInternal();
    }

    private void BeginDragInternal()
    {
        waitingForOwnership = false;
        isDraggingLocal = true;

        canvasGroup.blocksRaycasts = false;
        lastSlotOrParent = transform.parent;

        // Move to top drag layer (canvas)
        SetParentLocal(canvas.transform, true);
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        // Only the owner should move the card (so it stays authoritative)
        if (IsNetworked && !pv.IsMine) return;
        if (waitingForOwnership) return;
        if (SomeoneElseDragging()) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!TryCompleteDrag())
            return;

        if (transform.parent != canvas.transform && transform.parent != null)
            return;

        switch (Behavior)
        {
            case DraggableCardBehavior.Activity1:
                HandleActivity1EndDrag();
                break;
            case DraggableCardBehavior.Activity2:
                HandleActivity2EndDrag();
                break;
            case DraggableCardBehavior.Activity6:
                HandleActivity6EndDrag();
                break;
            case DraggableCardBehavior.Activity9:
                HandleActivity9EndDrag();
                break;
            default:
                HandleDefaultEndDrag();
                break;
        }
    }

    protected bool TryCompleteDrag()
    {
        if (IsNetworked && !pv.IsMine)
            return false;

        if (waitingForOwnership)
        {
            waitingForOwnership = false;
            SetInteractableState();
            return false;
        }

        isDraggingLocal = false;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        SetInteractableState();
        return true;
    }

    public void SnapBack()
    {
        if (IsNetworked && !pv.IsMine) return;

        if (!panel1GridLayout.gameObject.activeSelf)
        {
            Transform slot = FindFirstEmptyChild(trueHome);

            if (slot != null)
            {
                SnapTo(slot, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
        }
    }

    protected virtual void SnapTo(Transform parent, float xPos, float yPos, float xScale, float yScale)
    {
        ApplyLayoutState(parent, new Vector2(xPos, yPos), new Vector2(xScale, yScale));
    }

    public void ApplyLayoutState(Transform parent, Vector2 anchoredPosition, Vector2 scale)
    {
        if (parent == null) return;

        SetParentLocal(parent, false);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = scale;

        if (parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    protected void SetParentLocal(Transform newParent, bool worldPositionStays = true)
    {
        transform.SetParent(newParent, worldPositionStays);
    }

    protected Transform FindFirstEmptyChild(Transform gridParent)
    {
        foreach (Transform child in gridParent)
        {
            if (child.childCount == 0)
                return child;
        }
        return null;
    }

    protected bool IsPointerOverPanel(string panelTag)
    {
        var results = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.CompareTag(panelTag))
                return true;
        }
        return false;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (SomeoneElseDragging()) return;
        if (IsNetworked && !pv.IsMine && waitingForOwnership) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Sprite spriteToShow = (Behavior == DraggableCardBehavior.Activity1 && isFlipped && backSprite != null)
                ? backSprite
                : frontSprite;
            CardPopupManager.Instance.ShowCard(spriteToShow);
        }
    }

    public virtual void ResetCard()
    {
        if (IsNetworked && !pv.IsMine) return;

        SetParentLocal(originalParent, true);
        transform.position = originalPosition;

        if (cardImage != null)
            cardImage.color = Color.white;

        acceptedThisDrag = false;

        if (Behavior == DraggableCardBehavior.Activity1)
        {
            isFlipped = false;
            ShowFront();
        }

        if (Behavior == DraggableCardBehavior.Activity9)
        {
            isLocked = false;
        }
    }

    // ---------------------------
    // Photon Sync
    // ---------------------------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (pv == null) return;

        if (stream.IsWriting)
        {
            string parentPath = GetPathFromRoot(transform.parent);
            stream.SendNext(isDraggingLocal);
            stream.SendNext(parentPath);
            stream.SendNext(rectTransform.anchoredPosition);
            stream.SendNext((Vector2)rectTransform.localScale);
        }
        else
        {
            netDragging = (bool)stream.ReceiveNext();
            netParentPath = (string)stream.ReceiveNext();
            netAnchoredPos = (Vector2)stream.ReceiveNext();
            netScale = (Vector2)stream.ReceiveNext();

            if (!pv.IsMine)
            {
                ApplyRemoteState();
            }
        }
    }

    private void ApplyRemoteState()
    {
        Transform parent = FindByPath(netParentPath);
        if (parent != null && transform.parent != parent)
        {
            SetParentLocal(parent, false);
        }

        rectTransform.anchoredPosition = netAnchoredPos;
        rectTransform.localScale = netScale;

        if (parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        // Lock/unlock interaction based on whether someone else is dragging
        SetInteractableState();
    }

    private string GetPathFromRoot(Transform target)
    {
        if (pathRoot == null || target == null) return "";
        if (target == pathRoot) return "";

        var stack = new Stack<string>();
        Transform cur = target;

        while (cur != null && cur != pathRoot)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }

        if (cur != pathRoot) return "";
        return string.Join("/", stack.ToArray());
    }

    private Transform FindByPath(string path)
    {
        if (pathRoot == null) return null;
        if (string.IsNullOrEmpty(path)) return pathRoot;
        return pathRoot.Find(path);
    }

    // ---------------------------
    // Ownership callbacks
    // ---------------------------
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (pv == null || targetView != pv || requestingPlayer == null)
            return;

        if (pv.Owner == requestingPlayer)
            return;

        // Always allow the next player to take control of draggable puzzle cards.
        if (pv.IsMine || PhotonNetwork.IsMasterClient)
        {
            pv.TransferOwnership(requestingPlayer);
        }
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (pv == null || targetView != pv) return;

        // If we requested it, start dragging now
        if (pv.IsMine && waitingForOwnership)
        {
            BeginDragInternal();
        }

        SetInteractableState();
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        if (pv == null || targetView != pv) return;

        waitingForOwnership = false;
        SetInteractableState();
    }

    public void MarkAcceptedThisDrag()
    {
        acceptedThisDrag = true;
    }

    public void RevertToDragOrigin()
    {
        if (dragStartParent != null)
        {
            ApplyLayoutState(dragStartParent, dragStartAnchoredPos, dragStartScale);
            return;
        }

        switch (Behavior)
        {
            case DraggableCardBehavior.Activity2:
            case DraggableCardBehavior.Activity9:
                SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
                break;
            case DraggableCardBehavior.Activity6:
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.6409343f);
                break;
            default:
                SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
                break;
        }
    }

    public void ShowFront()
    {
        if (cardImage != null && frontSprite != null)
        {
            isFlipped = false;
            cardImage.sprite = frontSprite;
        }
    }

    public void ShowBack()
    {
        if (cardImage != null && backSprite != null)
        {
            isFlipped = true;
            cardImage.sprite = backSprite;
        }
    }

    private DraggableCardBehavior ResolveBehavior()
    {
        if (cardBehavior != DraggableCardBehavior.Auto)
            return cardBehavior;

        return DraggableCardBehavior.Default;
    }

    private void RecordDragOrigin()
    {
        dragStartParent = transform.parent;
        dragStartAnchoredPos = rectTransform.anchoredPosition;
        dragStartScale = rectTransform.localScale;
    }

    private void HandleDefaultEndDrag()
    {
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            if (emptySlot != null)
            {
                SnapTo(emptySlot, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
        }

        ResetCardColor();
    }

    private void HandleActivity1EndDrag()
    {
        bool droppedOnPanel1 = IsPointerOverPanel("Panel1");

        if (!acceptedThisDrag && !droppedOnPanel1)
        {
            Debug.Log("[DraggableCard] Activity1 dropped outside a slot and Panel1 -> revert to drag origin");
            RevertToDragOrigin();
            ResetCardColor();
            return;
        }

        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.IsChildOf(panel2GridLayout);
        bool fromPanel3 = lastSlotOrParent != null && lastSlotOrParent.IsChildOf(panel3GridLayout);

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2 && !panel1GridLayout.gameObject.activeSelf)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
        }
        else if (IsPointerOverPanel("Panel3") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
        }
        else if ((IsPointerOverPanel("Panel3") && fromPanel3) || (IsPointerOverPanel("Panel2") && fromPanel3))
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
        }
        else
        {
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
        }

        ResetCardColor();
    }

    private void HandleActivity2EndDrag()
    {
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 150, -150, 0.66f, 0.66f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            if (emptySlot != null)
            {
                SnapTo(emptySlot, 150, -150, 0.66f, 0.66f);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout, 150, -150, 0.66f, 0.66f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
        }

        ResetCardColor();
    }

    private void HandleActivity6EndDrag()
    {
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 150, -150, 0.6409343f, 0.6409343f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            if (emptySlot != null)
            {
                SnapTo(emptySlot, 225, -230, 1.55f, 1.55f);
                rectTransform.localScale = new Vector2(1.55f, 1.55f);
            }
            else
            {
                SnapTo(panel2GridLayout, 150, -150, 1.55f, 1.55f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.6409343f);
        }

        ResetCardColor();
    }

    private void HandleActivity9EndDrag()
    {
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 150, -150, 0.66f, 0.66f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            if (emptySlot != null)
            {
                SnapTo(emptySlot, 150, -150, 0.66f, 0.66f);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout, 150, -150, 0.66f, 0.66f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
        }

        ResetCardColor();
    }

    private void ResetCardColor()
    {
        if (cardImage != null)
            cardImage.color = Color.white;
    }
}
