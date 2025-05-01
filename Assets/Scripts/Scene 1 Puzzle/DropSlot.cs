// DropSlot.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Puzzle 1 Symptoms")]
    [SerializeField] private List<string> correctSymptoms = new List<string>();

    [Header("Puzzle 2 Colors")]
    [SerializeField] private List<string> acceptableBearColors = new List<string>();

    public static int puzzle1CorrectCount = 0;
    public static int puzzle2CorrectCount = 0;
    public static int requiredCorrect = 10;
    public static System.Action OnPuzzle1Complete;
    public static System.Action OnPuzzle2Complete;

    private bool hasPlacedCorrectPuzzle1 = false;
    private readonly Dictionary<GameObject, string> placedCardsPuzzle2 = new();

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

                if (!PuzzleStateManager.Instance.isPuzzle2Active)
                {
                    // Puzzle 1 Logic
                    if (!hasPlacedCorrectPuzzle1 && correctSymptoms.Contains(card.symptomName))
                    {
                        hasPlacedCorrectPuzzle1 = true;
                        puzzle1CorrectCount++;
                        PuzzleStateManager.Instance.MarkCorrect(card.symptomName);
                        Debug.Log("Correct Puzzle 1 symptom: " + card.symptomName);

                        if (puzzle1CorrectCount >= requiredCorrect)
                        {
                            OnPuzzle1Complete?.Invoke();
                        }
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

                    if (acceptableBearColors.Contains(bearColor) && !placedCardsPuzzle2.ContainsKey(droppedObj))
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
                }
            }
        }
    }
}
