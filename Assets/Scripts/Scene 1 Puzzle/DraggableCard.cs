using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [HideInInspector] public Transform trueHome;
    private Transform lastSlotOrParent;

    public string symptomName;

    private Image cardImage;               // The Image component to display the sprite
    public Sprite frontSprite;           // Symptom image
    public Sprite backSprite;            // Bear image

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

        if (transform.parent == canvas.transform)
        {
            transform.SetParent(trueHome);
            rectTransform.anchoredPosition = Vector2.zero;
        }
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
