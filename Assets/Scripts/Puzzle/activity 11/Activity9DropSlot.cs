using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PuzzleStateManager;

public class Activity9DropSlot : DropSlot
{
    [Header("Puzzle 11 answers")]
    [SerializeField] private int requiredCorrect = 5;
    [HideInInspector] public string stage1Answer = "";

    [Header("Optional Stage 2 linking")]
    public Activity9DropSlot linkedStage1Slot; // ✅ assign for Stage 2 slots

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity9DraggableCard a1Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStage.Puzzle11)
            {
                string stage2Answer = a1Card.lapsesAnswer.Trim().ToLower();

                string expectedAnswer = stage1Answer.Trim().ToLower();

                // ✅ For Stage 2, use linked Stage 1 slot
                if (linkedStage1Slot != null)
                    expectedAnswer = linkedStage1Slot.stage1Answer.Trim().ToLower();

                Debug.Log($"[IsCardCorrect] Stage 2 Card '{stage2Answer}' vs Expected '{expectedAnswer}'");

                return stage2Answer == expectedAnswer;
            }
        }
        return false;
    }
}
