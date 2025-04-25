using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Acceptable Correct Symptoms")]
    [SerializeField] private List<string> correctSymptoms = new List<string>();

    [Header("Global Configuration")]
    public static int correctCount = 0;
    public static int requiredCorrect = 10;
    public static System.Action OnAllCorrectPlaced;

    private bool hasPlacedCorrect = false;

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount > 0)
        {
            Debug.Log("Slot already occupied");
            return;
        }

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj != null)
        {
            DraggableCard card = droppedObj.GetComponent<DraggableCard>();
            if (card != null)
            {
                card.transform.SetParent(transform);
                card.transform.localPosition = Vector3.zero;

                if (!hasPlacedCorrect && correctSymptoms.Contains(card.symptomName))
                {
                    hasPlacedCorrect = true;
                    correctCount++;
                    Debug.Log("Correct symptom placed: " + card.symptomName);

                    if (correctCount >= requiredCorrect)
                    {
                        OnAllCorrectPlaced?.Invoke();
                    }
                }
                else if (!correctSymptoms.Contains(card.symptomName))
                {
                    Debug.Log("Incorrect symptom dropped: " + card.symptomName);
                }
            }
        }
    }
}
