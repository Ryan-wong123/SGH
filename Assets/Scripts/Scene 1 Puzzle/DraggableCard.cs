// DraggableCard.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Canvas canvas;

    [Header("Panel References")]
    public Transform panel1GridLayout;  // Right Slot All Symptoms
    public Transform panel2GridLayout;  // Left Slot Correct Symptom
    public Transform panel3GridLayout;  // Right Colored Slot Bear

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [HideInInspector] public Transform trueHome; // last successful grid parent
    public Transform lastSlotOrParent;

    public string symptomName;

    private Image cardImage;
    public Sprite frontSprite;
    public Sprite backSprite;
    private bool isFlipped = false;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (trueHome == null)
            trueHome = transform.parent;
        ShowFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        lastSlotOrParent = transform.parent;
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            // Special case 1: dragged out of Panel2 slot and dropped back on Panel2
            if (IsPointerOverPanel("Panel2") && lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout)
            {
                // Came from panel2, dropped outside its slot — go back to panel1
                SnapTo(panel1GridLayout);
            }
            // Special case 2: dragged out of Panel2 slot and dropped onto Panel1
            else if (IsPointerOverPanel("Panel1") && lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout)
            {
                SnapTo(panel1GridLayout);
            }
            else if (IsPointerOverPanel("Panel2") && lastSlotOrParent != null && lastSlotOrParent.parent == panel3GridLayout)
            {
                SnapTo(panel2GridLayout);
            }
            else if (IsPointerOverPanel("Panel3") && lastSlotOrParent != null && lastSlotOrParent.parent == panel3GridLayout)
            {
                SnapTo(panel2GridLayout);
            }
            else
            {
                // Default: snap back to last known valid panel
                SnapTo(trueHome);
            }
        }

    }
    private bool IsPointerOverPanel(string panelTag)
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
    private void SnapTo(Transform parent)
    {
        transform.SetParent(parent);
        rectTransform.anchoredPosition = new Vector2(100, -110);
        rectTransform.localScale = new Vector2(0.6409343f, 0.66764f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right &&
            PuzzleStateManager.Instance.isPuzzle2Active &&
            PuzzleStateManager.Instance.IsSymptomCorrect(symptomName))
        {
            isFlipped = !isFlipped;
            if (isFlipped) ShowBack();
            else ShowFront();
        }
    }

    private void ShowFront()
    {
        if (cardImage != null && frontSprite != null)
            cardImage.sprite = frontSprite;
    }

    private void ShowBack()
    {
        if (cardImage != null && backSprite != null)
            cardImage.sprite = backSprite;
    }
}
