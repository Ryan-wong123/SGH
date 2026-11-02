using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.

public class InsulinPuzzleManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [SerializeField] private int required; // Exposes this private field in the Unity Inspector.

    [SerializeField] GameObject panel1; // Exposes this private field in the Unity Inspector.
    [SerializeField] GameObject panel2; // Exposes this private field in the Unity Inspector.

    public TMP_Dropdown syringeDropdown; // Declares a field, property, or member used by this script.
    public int correctSyringeIndex = 1;       // The correct option index in dropdown
    public int CurrentPart => panel2 != null && panel2.activeSelf ? 2 : 1; // Identifies the active Puzzle 10 section for analytics.
    public GameObject ActivePanel => CurrentPart == 2 ? panel2 : panel1; // Provides the active Puzzle 10 panel for answer snapshots.

    public Color correctColor = Color.green; // Declares a field, property, or member used by this script.
    public Color wrongColor = Color.red; // Declares a field, property, or member used by this script.
    public Color defaultColor = Color.white; // Declares a field, property, or member used by this script.
    private Coroutine part1AdvanceRoutine;
    private int localResetRevision;

    public void SubmitSyringeAnswer()
    {
        if (CurrentPart != 1 || syringeDropdown == null || part1AdvanceRoutine != null)
            return;

        // In a room only the master schedules the shared deadline. Never fall
        // back to a client-local delay if the flow manager is unavailable.
        if (PhotonNetwork.InRoom)
        {
            PuzzleStateManager.Instance?.RequestInsulinPart2Transition(syringeDropdown.value);
            return;
        }
        part1AdvanceRoutine = StartCoroutine(CheckCorrectSyringe());
    }

    public IEnumerator CheckCorrectSyringe()
    {
        if (PhotonNetwork.InRoom)
        {
            PuzzleStateManager.Instance?.RequestInsulinPart2Transition(syringeDropdown.value);
            yield break;
        }
        int revision = localResetRevision;
        bool correct = syringeDropdown.value == correctSyringeIndex;
        PuzzleStateManager.Instance?.RecordPuzzle10PartAnswer(correct);
        syringeDropdown.SetSyncedFeedbackColor(correct ? correctColor : wrongColor);
        if (correct)
        {
            yield return new WaitForSecondsRealtime(2f);
            if (revision == localResetRevision && !PhotonNetwork.InRoom && CurrentPart == 1)
                GoToPart2();
        }
        else
        {
            // Let StartCoroutine return its handle before clearing it, so an
            // immediately incorrect answer cannot leave a completed handle latched.
            yield return null;
        }
        part1AdvanceRoutine = null;
    }

    public void CancelLocalAdvance()
    {
        localResetRevision++;
        if (part1AdvanceRoutine != null)
            StopCoroutine(part1AdvanceRoutine);
        part1AdvanceRoutine = null;
    }

    private void OnDisable()
    {
        CancelLocalAdvance();
    }

    private void OnEnable()
    {
        PuzzleStateManager.Instance?.RefreshInsulinRoomState();
    }

    public void ApplyRoomPart(int part, bool reset)
    {
        CancelLocalAdvance();
        if (reset && syringeDropdown != null)
        {
            syringeDropdown.SetValueWithoutNotify(0);
            if (syringeDropdown.targetGraphic != null)
                syringeDropdown.targetGraphic.color = defaultColor;
        }
        if (panel1 != null) panel1.SetActive(part == 1);
        if (panel2 != null) panel2.SetActive(part == 2);
    }

    public void AddResetProperties(Hashtable properties)
    {
        if (syringeDropdown == null) return;
        PhotonSyncedTMPDropdown synced = syringeDropdown.GetComponent<PhotonSyncedTMPDropdown>();
        if (synced != null) synced.AddResetProperties(properties);
    }

    public void GoToPart2() // Declares a method used by this script.
    { // Starts this code block.
        if (panel1 != null) panel1.SetActive(false); // Checks this condition before continuing.
        if (panel2 != null) panel2.SetActive(true); // Checks this condition before continuing.
    } // Ends this code block.


    public bool AreAllPuzzle8CardsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        

        int correct = 0; // Creates a local value used by the following logic.

        // Reset all card visuals
        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        // Evaluate placed cards only
        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                bool isCorrect = slot.IsCardCorrect(card); // Creates a local value used by the following logic.

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red); // Runs this line of puzzle logic.

                if (isCorrect) correct++; // Checks this condition before continuing.
            } // Ends this code block.
        } // Ends this code block.

        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f)); // Starts a Unity coroutine.

        Debug.Log($"[Puzzle 2] Correct: {correct} / Required: {required}"); // Writes a diagnostic message to the Unity console.

        return correct >= required; // Returns this result to the caller.
    } // Ends this code block.
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return AreAllPuzzle8CardsCorrect(); // Returns this result to the caller.
    } // Ends this code block.
    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds) // Declares a method used by this script.
    { // Starts this code block.
        yield return new WaitForSeconds(delaySeconds); // Pauses this coroutine until the yielded condition finishes.

        foreach (DropSlot slot in DropSlot.allDropSlots) // The registry also contains slots belonging to other puzzles.
        { // Starts this code block.
            if (!(slot is Activity6DropSlot)) continue;
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                Image img = card.GetComponent<Image>(); // Runs this line of puzzle logic.
                if (img != null && img.color == Color.red) // Checks this condition before continuing.
                { // Starts this code block.
                    card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    public void ResetPuzzle()
    {
        CancelLocalAdvance();
        if (PhotonNetwork.InRoom)
        {
            // The stage reset runs on every client; only its master publishes
            // the new revision and clears the shared dropdown snapshot.
            PuzzleStateManager.Instance?.ResetInsulinRoomState(this);
            ApplyRoomPart(1, true);
            return;
        }
        if (syringeDropdown != null)
        {
            syringeDropdown.value = 0;
            syringeDropdown.RefreshShownValue();
            syringeDropdown.SetSyncedFeedbackColor(defaultColor);
        }
        ApplyRoomPart(1, false);
    }

} // Ends this code block.
