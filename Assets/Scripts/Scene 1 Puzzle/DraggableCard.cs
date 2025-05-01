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

    [HideInInspector] public Transform trueHome;
    private Transform lastSlotOrParent;

    public string symptomName;

    private Image cardImage; // The Image component to display the sprite
    public Sprite frontSprite; // Symptom image
    public Sprite backSprite; // Bear image
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
        trueHome = lastSlotOrParent; // Set trueHome to current slot
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If dropped outside of a valid DropSlot
        if (transform.parent == canvas.transform || transform.parent == null)
        {
            if (IsPointerOverPanel("Panel1"))
                SnapTo(panel1GridLayout);
            else if (IsPointerOverPanel("Panel2"))
                SnapTo(panel2GridLayout);
            else if (IsPointerOverPanel("Panel3"))
                SnapTo(panel3GridLayout);
            else
                SnapTo(trueHome);
        }
    }

    private void SnapTo(Transform parent)
    {
        transform.SetParent(parent);
        rectTransform.anchoredPosition = new Vector2(100, -110);
        rectTransform.localScale = new Vector2(0.6409343f, 0.66764f);
    }

    private bool IsPointerOverPanel(string panelTag)
    {
        var results = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            Debug.Log(result.gameObject.tag);

            if (result.gameObject.CompareTag(panelTag))
                return true;
        }
        return false;
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
