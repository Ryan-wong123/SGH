// DraggableCard.cs (NETWORKED - shared ownership via RequestOwnership)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class DraggableCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
    IPunObservable, IPunOwnershipCallbacks
{
    private const string PlayerTagPropertyKey = "PlayerTag";

    public enum DraggableCardBehavior
    {
        Auto,
        Default,
        Activity1,
        Activity1_2,
        Activity2,
        Activity6,
        Activity9
    }

    [Header("UI References")]
    [SerializeField] protected Canvas canvas;

    [SerializeField] protected Transform panel1GridLayout;  // Right Slot All Symptoms
    [SerializeField] protected Transform panel2GridLayout;  // Left Slot Correct Symptom

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

    [Header("Drag Player Tag")]
    [SerializeField] private Vector2 dragTagSize = new Vector2(220f, 34f);
    [SerializeField] private Vector2 dragTagOffset = new Vector2(0f, -8f);
    [SerializeField] private Color dragTagBackgroundColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color dragTagTextColor = Color.white;
    [SerializeField] private float dragTagFontSize = 22f;

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
    private string netDraggingPlayerName;

    private bool isFlipped;
    private bool acceptedThisDrag;
    private Transform dragStartParent;
    private Vector2 dragStartAnchoredPos;
    private Vector2 dragStartScale;

    private RectTransform dragTagRect;
    private Image dragTagBackground;
    private TextMeshProUGUI dragTagText;

    public DraggableCardBehavior Behavior => ResolveBehavior();

    protected virtual void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        pv = GetComponent<PhotonView>();

        EnsureDragPlayerTag();
        SetDragTagVisible(false, string.Empty);
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
        SetDragTagVisible(true, GetLocalPlayerDisplayName());
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
            case DraggableCardBehavior.Activity1_2:
                HandleActivity1_2EndDrag();
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
        SetDragTagVisible(false, string.Empty);

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

        SetDragTagVisible(false, string.Empty);
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
            stream.SendNext(isDraggingLocal ? GetLocalPlayerDisplayName() : string.Empty);
            stream.SendNext(parentPath);
            stream.SendNext(rectTransform.anchoredPosition);
            stream.SendNext((Vector2)rectTransform.localScale);
        }
        else
        {
            netDragging = (bool)stream.ReceiveNext();
            netDraggingPlayerName = (string)stream.ReceiveNext();
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
        SetDragTagVisible(netDragging, netDraggingPlayerName);
    }

    private string GetPathFromRoot(Transform target)
    {
        if (pathRoot == null || target == null) return "";
        if (target == pathRoot) return "";

        var stack = new Stack<string>();
        Transform cur = target;

        while (cur != null && cur != pathRoot)
        {
            stack.Push(BuildPathSegment(cur));
            cur = cur.parent;
        }

        if (cur != pathRoot) return "";
        return string.Join("/", stack.ToArray());
    }

    private Transform FindByPath(string path)
    {
        if (pathRoot == null) return null;
        if (string.IsNullOrEmpty(path)) return pathRoot;

        Transform current = pathRoot;
        string[] segments = path.Split('/');

        foreach (string segment in segments)
        {
            if (!TryParsePathSegment(segment, out string childName, out int sameNameIndex))
                return null;

            current = FindChildByNameIndex(current, childName, sameNameIndex);
            if (current == null)
                return null;
        }

        return current;
    }

    private string BuildPathSegment(Transform target)
    {
        if (target == null)
            return string.Empty;

        int sameNameIndex = 0;
        Transform parent = target.parent;

        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling.name != target.name)
                    continue;

                if (sibling == target)
                    break;

                sameNameIndex++;
            }
        }

        return $"{target.name}|{sameNameIndex}";
    }

    private bool TryParsePathSegment(string segment, out string childName, out int sameNameIndex)
    {
        childName = segment;
        sameNameIndex = 0;

        if (string.IsNullOrEmpty(segment))
            return false;

        int separatorIndex = segment.LastIndexOf('|');
        if (separatorIndex < 0)
            return true;

        childName = segment.Substring(0, separatorIndex);
        string indexText = segment.Substring(separatorIndex + 1);

        if (string.IsNullOrEmpty(childName))
            return false;

        return int.TryParse(indexText, out sameNameIndex);
    }

    private Transform FindChildByNameIndex(Transform parent, string childName, int sameNameIndex)
    {
        if (parent == null)
            return null;

        int currentIndex = 0;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name != childName)
                continue;

            if (currentIndex == sameNameIndex)
                return child;

            currentIndex++;
        }

        return null;
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

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
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
    private void HandleActivity1_2EndDrag()
    {
        bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

        if (IsPointerOverPanel("Panel1") && fromPanel2)
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
        }
        else if (IsPointerOverPanel("Panel2") && fromPanel2)
        {
            Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
            SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
        }
        else
        {
            SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
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

    private void EnsureDragPlayerTag()
    {
        if (dragTagRect != null)
        {
            return;
        }

        GameObject tagObject = new GameObject("DragPlayerTag", typeof(RectTransform), typeof(Image));
        tagObject.transform.SetParent(transform, false);

        dragTagRect = tagObject.GetComponent<RectTransform>();
        dragTagRect.anchorMin = new Vector2(0.5f, 0f);
        dragTagRect.anchorMax = new Vector2(0.5f, 0f);
        dragTagRect.pivot = new Vector2(0.5f, 1f);
        dragTagRect.anchoredPosition = dragTagOffset;
        dragTagRect.sizeDelta = dragTagSize;

        dragTagBackground = tagObject.GetComponent<Image>();
        dragTagBackground.color = dragTagBackgroundColor;
        dragTagBackground.raycastTarget = false;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(tagObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        dragTagText = textObject.GetComponent<TextMeshProUGUI>();
        dragTagText.alignment = TextAlignmentOptions.Center;
        dragTagText.fontSize = dragTagFontSize;
        dragTagText.color = dragTagTextColor;
        dragTagText.enableWordWrapping = false;
        dragTagText.overflowMode = TextOverflowModes.Ellipsis;
        dragTagText.raycastTarget = false;
    }

    private void SetDragTagVisible(bool visible, string playerName)
    {
        if (dragTagRect == null)
        {
            return;
        }

        bool shouldShow = visible && !string.IsNullOrWhiteSpace(playerName);

        if (dragTagText != null)
        {
            dragTagText.text = shouldShow ? playerName.Trim() : string.Empty;
        }

        if (dragTagRect.gameObject.activeSelf != shouldShow)
        {
            dragTagRect.gameObject.SetActive(shouldShow);
        }
    }

    private string GetLocalPlayerDisplayName()
    {
        return GetPlayerDisplayName(PhotonNetwork.LocalPlayer);
    }

    private string GetPlayerDisplayName(Player player)
    {
        if (player == null)
        {
            return "Player";
        }

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(PlayerTagPropertyKey, out object tagValue) &&
            tagValue is string tag &&
            !string.IsNullOrWhiteSpace(tag))
        {
            return tag.Trim();
        }

        if (!string.IsNullOrWhiteSpace(player.NickName))
        {
            return player.NickName.Trim();
        }

        return $"Player {player.ActorNumber}";
    }
}
