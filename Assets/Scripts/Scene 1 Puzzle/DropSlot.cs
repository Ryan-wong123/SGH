// DropSlot.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Puzzle 1 Symptoms")]
    [SerializeField] public List<string> correctSymptoms = new List<string>();

    [Header("Puzzle 2 Colors")]
    [SerializeField] private List<string> acceptableBearColors = new List<string>();

    [Header("Tint Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    public static int puzzle1CorrectCount = 0;
    public static int puzzle2CorrectCount = 0;
    public static int requiredCorrect = 10;
    public static System.Action OnPuzzle1Complete;
    public static System.Action OnPuzzle2Complete;

    private readonly Dictionary<GameObject, string> placedCardsPuzzle2 = new();

    private Image cardImage;
    private bool isCorrect;
    private List<DraggableCard> cardsInSlot = new();
    public static List<DropSlot> allDropSlots = new();
    public List<DraggableCard> GetCardsInSlot() => cardsInSlot;

    private void Start()
    {
        allDropSlots.Add(this);
    }
    private void OnDestroy()
    {
        allDropSlots.Remove(this);
    }
    public void RemoveCard(DraggableCard card)
    {
        cardsInSlot.Remove(card);
    }


    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;

        if (transform.childCount > 0)
        {
            Debug.Log("Slot already occupied");

            if (droppedObj != null)
            {
                DraggableCard card = droppedObj.GetComponent<DraggableCard>();
                card?.SnapBack(); // Return card if slot is full
            }

            return;
        }

        if (droppedObj != null)
        {
            DraggableCard card = droppedObj.GetComponent<DraggableCard>();
            if (card != null)
            {
                card.transform.SetParent(transform);

                var rect = card.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(100, -110);
                rect.localScale = Vector2.one;

                // Mark the parent grid layout as the new true home
                card.trueHome = transform.parent;

                cardImage = card.GetComponent<Image>();

                if (!PuzzleStateManager.Instance.isPuzzle2Active)
                {
                    isCorrect = correctSymptoms.Contains(card.symptomName);

                    cardsInSlot.Add(card);

                    // Puzzle 1 Logic
                    if (correctSymptoms.Contains(card.symptomName))
                    {
                        
                        puzzle1CorrectCount++;
                        PuzzleStateManager.Instance.MarkCorrect(card.symptomName);
                        Debug.Log("Correct Puzzle 1 symptom: " + card.symptomName);
                    }
                    else
                    {
                        Debug.Log("Incorrect Puzzle 1 symptom: " + card.symptomName);
                    }
                  
                }
                else
                {
                    // Puzzle 2 Logic
                    string bearColor = card.backSprite.name.ToLower();
                    isCorrect = acceptableBearColors.Contains(bearColor);

                    if (acceptableBearColors.Contains(bearColor) && !placedCardsPuzzle2.ContainsKey(droppedObj) && isCorrect)
                    {
                        placedCardsPuzzle2[droppedObj] = bearColor;
                        puzzle2CorrectCount++;
                        Debug.Log("Correct Puzzle 2 bear color: " + bearColor);

                        if (puzzle2CorrectCount >= requiredCorrect)
                        {
                            OnPuzzle2Complete?.Invoke();
                        }
                    }
                    else
                    {
                        Debug.Log("Incorrect Puzzle 2 bear color: " + bearColor);
                    }
                    if (cardImage != null)
                        cardImage.color = isCorrect ? correctColor : wrongColor;
                }
            }
        }
    }


}
