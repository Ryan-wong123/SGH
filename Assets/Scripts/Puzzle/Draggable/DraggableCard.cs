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
    [Header("UI References")]
    [SerializeField] protected Canvas canvas;

    [SerializeField] protected Transform panel1GridLayout;  // Right Slot All Symptoms
    [SerializeField] protected Transform panel2GridLayout;  // Left Slot Correct Symptom

    [Header("Networking Path Root (IMPORTANT)")]
    [Tooltip("Root used to compute hierarchy paths for parenting sync. Usually your Canvas transform.")]
    [SerializeField] private Transform pathRoot;

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

        if (transform.parent == canvas.transform || transform.parent == null)
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

            var img = GetComponent<Image>();
            if (img != null) img.color = Color.white;
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
            Sprite spriteToShow = frontSprite;
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
}
