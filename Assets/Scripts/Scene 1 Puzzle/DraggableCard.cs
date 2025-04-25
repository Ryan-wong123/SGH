using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Canvas canvas;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [HideInInspector] public Transform trueHome;     // initial draggable panel
    private Transform lastSlotOrParent;

    public string symptomName;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Store the true original panel once
        if (trueHome == null)
            trueHome = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        lastSlotOrParent = transform.parent;
        transform.SetParent(canvas.transform); // move to top
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If dropped nowhere (not a slot), return to true home
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(trueHome);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
