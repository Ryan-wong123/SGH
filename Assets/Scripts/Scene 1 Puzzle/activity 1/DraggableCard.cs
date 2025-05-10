// DraggableCard.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using static PuzzleStateManager;

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

        // Check if dragged out of a slot
        if (lastSlotOrParent != null && lastSlotOrParent.GetComponent<DropSlot>() != null)
        {
            DropSlot previousSlot = lastSlotOrParent.GetComponent<DropSlot>();

            bool wasCorrect = previousSlot.correctSymptoms.Contains(symptomName);
            bool droppedOutsideSlot = transform.parent == canvas.transform || transform.parent.GetComponent<DropSlot>() == null;

            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStage.Puzzle1 && wasCorrect && droppedOutsideSlot)
            {
                DropSlot.puzzle1CorrectCount = Mathf.Max(0, DropSlot.puzzle1CorrectCount - 1);
                Debug.Log($"Correct card removed. New count: {DropSlot.puzzle1CorrectCount}");

                previousSlot.RemoveCard(this); // Optional: clean tracking
            }

            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStage.Puzzle2 && droppedOutsideSlot)
            {
                string bearColor = backSprite.name.ToLower();
                if (previousSlot.acceptableBearColors.Contains(bearColor))
                {
                    DropSlot.puzzle2CorrectCount = Mathf.Max(0, DropSlot.puzzle2CorrectCount - 1);
                    Debug.Log($"Correct Puzzle 2 card removed. New count: {DropSlot.puzzle2CorrectCount}");

                    previousSlot.RemoveCard(this);
                }
            }

        }

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;
            bool fromPanel3 = lastSlotOrParent != null && lastSlotOrParent.parent == panel3GridLayout;

            // 1. From panel2, dropped on panel1(not a slot) → snap to panel1
            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                SnapTo(panel1GridLayout);
            }
            // 2. From panel2, dropped on panel2(not a slot) and panel1 is disabled → snap to panel2
            else if (IsPointerOverPanel("Panel2") && fromPanel2 && !panel1GridLayout.gameObject.activeSelf)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    SnapTo(emptySlot);
                    rectTransform.localScale = Vector2.one;
                }
                else
                {
                    SnapTo(panel2GridLayout);
                    rectTransform.localScale = Vector2.one;
                }

            }
            // 2. From panel2, dropped on panel2 (not a slot) → snap to panel1 if panel1 is active
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                SnapTo(panel1GridLayout);
            }
            // 3. From panel2, dropped on panel3 (not slot) → snap to empty child in panel2
            else if (IsPointerOverPanel("Panel3") && fromPanel2)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    SnapTo(emptySlot);
                    rectTransform.localScale = Vector2.one;
                }
                else
                {
                    SnapTo(panel2GridLayout);
                    rectTransform.localScale = Vector2.one;
                }    
            }
            // 4. From panel3, dropped on panel3 (not slot) → snap to panel3
            else if ((IsPointerOverPanel("Panel3") && fromPanel3) || (IsPointerOverPanel("Panel2") && fromPanel3))
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    SnapTo(emptySlot);
                    rectTransform.localScale = Vector2.one;
                }
                else
                {
                    SnapTo(panel2GridLayout);
                    rectTransform.localScale = Vector2.one;
                }
            }
            else
            {
                SnapTo(trueHome);
            }
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }
    public void SnapBack()
    {
        if (!panel1GridLayout.gameObject.activeSelf)
        {
            Transform slot = FindFirstEmptyChild(trueHome);
            if (slot != null)
            {
                SnapTo(slot);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(trueHome);
        }
    }
    private void SnapTo(Transform parent)
    {
        transform.SetParent(parent);
        rectTransform.anchoredPosition = new Vector2(100, -110);
        rectTransform.localScale = new Vector2(0.6409343f, 0.66764f);
    }

    private Transform FindFirstEmptyChild(Transform gridParent)
    {
        foreach (Transform child in gridParent)
        {
            if (child.childCount == 0)
                return child;
        }
        return null;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right &&
            PuzzleStateManager.Instance.CurrentStage == PuzzleStage.Puzzle2 &&
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
