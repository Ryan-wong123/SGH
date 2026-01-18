// DraggableCard.cs (NETWORKED)
// Drop-in replacement for your existing DraggableCard.cs

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPunObservable
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

    // Local drag state (owned client)
    private bool isDragging;

    // Remote smoothing
    private Vector2 netAnchoredPos;
    private Vector2 netScale;
    private string netParentPath;
    private bool netDragging;

    protected virtual void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        pv = GetComponent<PhotonView>();
    }

    protected virtual void Start()
    {
        if (pathRoot == null && canvas != null) pathRoot = canvas.transform;

        if (trueHome == null)
            trueHome = transform.parent;

        originalParent = transform.parent;
        originalPosition = transform.position;

        // Only owner should be interactable (prevents remote click/drag stealing raycasts)
        if (pv != null && !pv.IsMine)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private bool CanControl()
    {
        // If not networked, allow. If networked, only owner can control.
        return pv == null || pv.IsMine;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanControl()) return;

        // Take ownership so only 1 player drives the sync while dragging
        if (pv != null && !pv.IsMine)
        {
            pv.RequestOwnership();
        }

        isDragging = true;

        canvasGroup.blocksRaycasts = false;
        lastSlotOrParent = transform.parent;

        // Move to top drag layer (canvas)
        SetParentLocal(canvas.transform);
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!CanControl()) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!CanControl()) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

            // 1. From panel2, dropped on panel1(not a slot) → snap to panel1
            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            // 2. From panel2, dropped on panel2(not a slot)
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

    public void SnapBack()
    {
        if (!CanControl()) return;

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
        SetParentLocal(parent);
        rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        rectTransform.localScale = new Vector2(xScale, yScale);
    }

    private void SetParentLocal(Transform newParent)
    {
        transform.SetParent(newParent);
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
        if (!CanControl()) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Sprite spriteToShow = frontSprite;
            CardPopupManager.Instance.ShowCard(spriteToShow);
        }
    }

    public virtual void ResetCard()
    {
        if (!CanControl()) return;

        SetParentLocal(originalParent);
        transform.position = originalPosition;

        if (cardImage != null)
            cardImage.color = Color.white;
    }

    // ---------------------------
    // Photon Sync
    // ---------------------------
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // If not networked, do nothing
        if (pv == null) return;

        if (stream.IsWriting)
        {
            // Send parent path + rect state
            string parentPath = GetPathFromRoot(transform.parent);
            stream.SendNext(isDragging);
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
        // Reparent by path
        Transform parent = FindByPath(netParentPath);
        if (parent != null && transform.parent != parent)
        {
            transform.SetParent(parent);
        }

        // Apply rect state
        rectTransform.anchoredPosition = netAnchoredPos;
        rectTransform.localScale = netScale;

        // Remote should never block raycasts (prevents them from interfering with local drag)
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private string GetPathFromRoot(Transform target)
    {
        if (pathRoot == null || target == null) return "";

        // If target is the root itself
        if (target == pathRoot) return "";

        // Build "A/B/C" path relative to root
        var stack = new Stack<string>();
        Transform cur = target;

        while (cur != null && cur != pathRoot)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }

        // If we never reached root, hierarchy mismatch
        if (cur != pathRoot) return "";

        return string.Join("/", stack.ToArray());
    }

    private Transform FindByPath(string path)
    {
        if (pathRoot == null) return null;
        if (string.IsNullOrEmpty(path)) return pathRoot;

        return pathRoot.Find(path);
    }
}
