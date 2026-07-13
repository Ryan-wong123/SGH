using Photon.Pun; // Imports a namespace used by this puzzle state manager.
using Photon.Realtime; // Imports a namespace used by this puzzle state manager.
using ExitGames.Client.Photon;                      // keep Photon namespace // Imports a namespace used by this puzzle state manager.
using System; // Imports a namespace used by this puzzle state manager.
using System.Collections; // Imports a namespace used by this puzzle state manager.
using System.Collections.Generic; // Imports a namespace used by this puzzle state manager.
using System.Text; // Imports a namespace used by this puzzle state manager.
using TMPro; // Imports a namespace used by this puzzle state manager.
using Unity.VisualScripting; // Imports a namespace used by this puzzle state manager.
using UnityEngine; // Imports a namespace used by this puzzle state manager.
using UnityEngine.UI; // Imports a namespace used by this puzzle state manager.
using Hashtable = ExitGames.Client.Photon.Hashtable; // alias Photon Hashtable // Creates a shorter alias for Photon room/player property tables.

public class PuzzleStateManager : MonoBehaviourPunCallbacks // Declares the networked manager that tracks puzzle stage, locks, resets, and rewards.
{ // Starts this block.
    [Serializable] // Allows Unity to serialize this nested data type.
    private class PuzzleClueReward // Defines the clue reward data for a completed puzzle stage.
    { // Starts this block.
        public PuzzleStage stage; // Handles this part of the puzzle state flow.
        public string clueId; // Handles this part of the puzzle state flow.
        [Tooltip("Optional popup override. Leave empty to use 'Clue picked up: <clue name>'.")] // Shows guidance for this field in the Unity inspector.
        public string popupMessage; // Handles this part of the puzzle state flow.
    } // Ends this block.

    [Header("puzzle trigger object")] // Groups related fields in the Unity inspector.
    [SerializeField] private GameObject puzzle1Object; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject puzzle2Object; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject puzzle3Object; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject puzzle4Object; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject puzzle5Object; // Exposes this private field in the Unity inspector.

    [SerializeField] private float delayTransition; // Exposes this private field in the Unity inspector.

    public static PuzzleStateManager Instance; // Stores the shared PuzzleStateManager singleton instance.

    [Header("Puzzle State")] // Groups related fields in the Unity inspector.
    public PuzzleStage CurrentStage = PuzzleStage.Puzzle0; // Stores the puzzle stage currently active for the room.

    [Header("Puzzle 1 Correct Symptoms")] // Groups related fields in the Unity inspector.
    public HashSet<string> correctSymptomsFromPuzzle1 = new(); // Stores symptom IDs accepted from the first puzzle.

    [Header("puzzle 1 ")] // Groups related fields in the Unity inspector.
    [SerializeField] private GameObject Puzzle1Panel;  // Exposes this private field in the Unity inspector.
    [SerializeField] private SymptomsCardManager symptomsCardManager; // Exposes this private field in the Unity inspector.

    [Header("puzzle 2")] // Groups related fields in the Unity inspector.
    [SerializeField] private GameObject Puzzle2Panel; // Exposes this private field in the Unity inspector.
    [SerializeField] private BearCardManager bearCardManager; // Exposes this private field in the Unity inspector.

    [Header("puzzle 3")] // Groups related fields in the Unity inspector.
    [SerializeField] private GameObject Puzzle3Panel; // Exposes this private field in the Unity inspector.
    [SerializeField] private PictureGridManager pictureGridManager; // Exposes this private field in the Unity inspector.

    [Header("puzzle 4")] // Groups related fields in the Unity inspector.
    [SerializeField] private Activity2PuzzleManager activity2PuzzleManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle4Panel; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle4Panel1; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle4Panel2; // Exposes this private field in the Unity inspector.

    [Header("puzzle 5")] // Groups related fields in the Unity inspector.
    [SerializeField] private WordSearchManager wordSearchManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle5Panel; // Exposes this private field in the Unity inspector.

    [Header("puzzle 6")] // Groups related fields in the Unity inspector.
    [SerializeField] private DiaryPuzzleManager diaryPuzzleManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle6Panel; // Exposes this private field in the Unity inspector.

    [Header("puzzle 7")] // Groups related fields in the Unity inspector.
    [SerializeField] private RiskTable risktableManager; // Exposes this private field in the Unity inspector.
  
    [SerializeField] private GameObject Puzzle7Panel; // Exposes this private field in the Unity inspector.

    [Header("puzzle 8")] // Groups related fields in the Unity inspector.
    [SerializeField] private PrescriptionManager prescriptionManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle8Panel; // Exposes this private field in the Unity inspector.

    [Header("puzzle 9")] // Groups related fields in the Unity inspector.
    [SerializeField] private CrosswordManager crosswordManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle9Panel; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle9ImageHint; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle9AcrossHint; // Exposes this private field in the Unity inspector.

    [Header("puzzle 10")] // Groups related fields in the Unity inspector.
    [SerializeField] private InsulinPuzzleManager insulinPuzzleManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle10Panel; // Exposes this private field in the Unity inspector.

    [Header("puzzle 11")] // Groups related fields in the Unity inspector.
    [SerializeField] private DeathLapsesManager deathLapsesManager; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject Puzzle11Panel; // Exposes this private field in the Unity inspector.
    [SerializeField] private GameObject movementTargetPuzzle11; // Exposes this private field in the Unity inspector.

    [Header("Puzzle Clue Rewards")] // Groups related fields in the Unity inspector.
    [SerializeField] private List<PuzzleClueReward> puzzleClueRewards = new(); // Exposes this private field in the Unity inspector.

    private Dictionary<PuzzleStage, IPuzzle> puzzleHandlers; // Maps puzzle stages to their runtime puzzle handler components.



    // Tracks which panel belongs to which stage
    private readonly Dictionary<PuzzleStage, GameObject> stagePanels = new(); // Maps puzzle stages to their main UI panels.
    private GameObject currentPuzzlePanel; // Tracks the currently open puzzle panel.
    private int currentSubmitOwnerActorNumber = -1; // Tracks which Photon actor currently owns submit-button visibility.
    private bool localPlayerHasActivePuzzleLock; // Tracks whether this client owns the active puzzle lock.
    private int localActivePuzzleLockOwnerActorNumber = -1; // Stores the actor number that owns this client-side puzzle lock.
    private bool localPlayerHasPuzzleReentryAccess; // Tracks whether this client may re-enter a puzzle after closing it.
    private PuzzleStage localPuzzleReentryStage = PuzzleStage.Puzzle0; // Stores which stage this client can re-enter.
    private int localPuzzleReentryOwnerActorNumber = -1; // Stores the actor number tied to local puzzle re-entry access.

    // ===== Room / Player property keys =====
    private const string RP_PUZZLE_LOCKED = "PuzzleLocked";   // bool // Defines a Photon room-property key used for shared puzzle state.
    private const string RP_LOCK_OWNER = "LockOwner";       // int ActorNumber // Defines a Photon room-property key used for shared puzzle state.
    private const string RP_ROUND = "Round";           // int; increments to signal a reset // Defines a Photon room-property key used for shared puzzle state.
    private const string PP_PLAYED_ROUND = "PlayedRound";     // bool on each player // Defines a Photon player-property key used for per-player puzzle state.

    public enum PuzzleStage // Defines the ordered puzzle stages used by the game flow.
    { // Starts this block.
        Puzzle0 = 0, // Assigns a numeric value to this puzzle stage.
        Puzzle1 = 1, // Assigns a numeric value to this puzzle stage.
        Puzzle2 = 2, // Assigns a numeric value to this puzzle stage.
        Puzzle3 = 3, // Assigns a numeric value to this puzzle stage.
        Puzzle4 = 4, // Assigns a numeric value to this puzzle stage.
        Puzzle5 = 5, // Assigns a numeric value to this puzzle stage.
        Puzzle6 = 6, // Assigns a numeric value to this puzzle stage.
        Puzzle7 = 7, // Assigns a numeric value to this puzzle stage.
        Puzzle8 = 8, // Assigns a numeric value to this puzzle stage.
        Puzzle9 = 9, // Assigns a numeric value to this puzzle stage.
        Puzzle10 = 10, // Assigns a numeric value to this puzzle stage.
        Puzzle11 = 11 // Assigns a numeric value to this puzzle stage.
    } // Ends this block.

    private void Awake() // Initializes the singleton and persistent puzzle mappings.
    { // Starts this block.
        if (Instance == null) Instance = this; // Checks whether this puzzle-state condition requires action.
        else { Destroy(gameObject); return; } // Handles this part of the puzzle state flow.

        InitStagePanels(); // Initializes the stage panel lookup.
        DontDestroyOnLoad(gameObject); // optional if changing scenes // Keeps this manager alive across scene loads.
    } // Ends this block.

    private void Start() // Initializes puzzle handlers and room state after Unity startup.
    { // Starts this block.
        puzzleHandlers = new Dictionary<PuzzleStage, IPuzzle> // Creates the stage-to-handler lookup table.
        { // Starts this block.
            { PuzzleStage.Puzzle1, symptomsCardManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle2, bearCardManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle3, pictureGridManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle4, activity2PuzzleManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle5, wordSearchManager  }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle6, diaryPuzzleManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle7, risktableManager  }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle8, prescriptionManager  }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle9, crosswordManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle10, insulinPuzzleManager }, // Maps this stage to its puzzle handler.
            { PuzzleStage.Puzzle11, deathLapsesManager } // Maps this stage to its puzzle handler.
        }; // Ends this initializer block.




        EnsureRoomProps(); // initialize fair-turn props in room // Initializes shared Photon room puzzle properties.
        RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.
    } // Ends this block.

    private void InitStagePanels() // Builds the stage-to-panel lookup table.
    { // Starts this block.
        stagePanels.Clear(); // Clears this puzzle-state collection.
        stagePanels[PuzzleStage.Puzzle1] = Puzzle1Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle2] = Puzzle2Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle3] = Puzzle3Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle4] = Puzzle4Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle5] = Puzzle5Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle6] = Puzzle6Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle7] = Puzzle7Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle8] = Puzzle8Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle9] = Puzzle9Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle10] = Puzzle10Panel; // Registers a puzzle panel for this stage.
        stagePanels[PuzzleStage.Puzzle11] = Puzzle11Panel; // Registers a puzzle panel for this stage.
    } // Ends this block.



    // ===== Timer-driven reloads =====
    public void OnTimerExpired() // Reloads the current puzzle when the shared timer expires.
    { // Starts this block.
        Debug.Log("Timer expired � reloading current puzzle stage."); // Writes puzzle-state diagnostic information to the Unity console.
        GameSessionCsvLogger.Instance?.RecordPuzzleReset(CurrentStage); // Records puzzle-session information to the CSV logger when available.

        switch (CurrentStage) // Selects behavior based on the current puzzle stage or state.
        { // Starts this block.
            case PuzzleStage.Puzzle0: break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle1: ReloadPuzzle1(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle2: ReloadPuzzle2(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle3: ReloadPuzzle3(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle4: ReloadPuzzle4(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle5: ReloadPuzzle5(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle6: ReloadPuzzle6(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle7: ReloadPuzzle7(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle8: ReloadPuzzle8(); break;  // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle9: ReloadPuzzle9(); break;  // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle10: ReloadPuzzle10(); break;  // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle11: ReloadPuzzle11(); break;  // Handles this specific puzzle stage or switch value.
        } // Ends this block.
    } // Ends this block.

    public void ReloadPuzzle1() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 1 and 2"); // Writes puzzle-state diagnostic information to the Unity console.

        DropSlot.ResetAllSlots(); // Clears all drag-and-drop slot state for the puzzle reset.

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity1)) // Loops through each matching puzzle object.
        { // Starts this block.
            card.ResetSyncedFeedbackColor(); // Clears synced feedback color on this card.
            card.ResetCard(); // Returns this draggable card to its starting state.
            card.ShowFront(); // Shows the front side of this card.
        } // Ends this block.

        CurrentStage = PuzzleStage.Puzzle1; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle2() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 1 and 2"); // Writes puzzle-state diagnostic information to the Unity console.

        DropSlot.ResetAllSlots(); // Clears all drag-and-drop slot state for the puzzle reset.

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity1_2)) // Loops through each matching puzzle object.
        { // Starts this block.
            card.ResetSyncedFeedbackColor(); // Clears synced feedback color on this card.
            card.ResetCard(); // Returns this draggable card to its starting state.
            card.ShowFront(); // Shows the front side of this card.
        } // Ends this block.

        CurrentStage = PuzzleStage.Puzzle2; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.


    public void ReloadPuzzle3() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 3"); // Writes puzzle-state diagnostic information to the Unity console.
        pictureGridManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.
        CurrentStage = PuzzleStage.Puzzle3; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle4() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 4"); // Writes puzzle-state diagnostic information to the Unity console.
        DropSlot.ResetAllSlots(); // Clears all drag-and-drop slot state for the puzzle reset.
        PuzzleTileManager.Instance?.ResetPuzzle(); // Resets or updates the rotating puzzle tile manager when present.

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity2)) // Loops through each matching puzzle object.
        { // Starts this block.
            card.ResetSyncedFeedbackColor(); // Clears synced feedback color on this card.
            card.ResetCard(); // Returns this draggable card to its starting state.
        } // Ends this block.
        activity2PuzzleManager.ResetActivity2Part2(); // Updates or calls this puzzle-state object.
        Puzzle4Panel1?.SetActive(true); // Shows or hides this puzzle-related UI object.
        Puzzle4Panel2?.SetActive(false); // Shows or hides this puzzle-related UI object.
        CurrentStage = PuzzleStage.Puzzle4;  // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle5() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 5"); // Writes puzzle-state diagnostic information to the Unity console.
        wordSearchManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.
        CurrentStage = PuzzleStage.Puzzle5; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle6() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 6"); // Writes puzzle-state diagnostic information to the Unity console.
        diaryPuzzleManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.
        CurrentStage = PuzzleStage.Puzzle6; // fix // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle7() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 7"); // Writes puzzle-state diagnostic information to the Unity console.
        risktableManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.
        CurrentStage = PuzzleStage.Puzzle7; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle8() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 8"); // Writes puzzle-state diagnostic information to the Unity console.
        prescriptionManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.
        CurrentStage = PuzzleStage.Puzzle8; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.

    public void ReloadPuzzle9() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 9"); // Writes puzzle-state diagnostic information to the Unity console.
        crosswordManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.

        CurrentStage = PuzzleStage.Puzzle9; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.


    public void ReloadPuzzle10() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 10"); // Writes puzzle-state diagnostic information to the Unity console.
        DropSlot.ResetAllSlots(); // Clears all drag-and-drop slot state for the puzzle reset.
        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity6)) // Loops through each matching puzzle object.
        { // Starts this block.
            card.ResetSyncedFeedbackColor(); // Clears synced feedback color on this card.
            card.ResetCard(); // Returns this draggable card to its starting state.
        } // Ends this block.
        insulinPuzzleManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.

        CurrentStage = PuzzleStage.Puzzle10; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.


    public void ReloadPuzzle11() // Resets this puzzle stage after timeout or retry.
    { // Starts this block.
        Debug.Log("Reloading Puzzle 11"); // Writes puzzle-state diagnostic information to the Unity console.
        DropSlot.ResetAllSlots(); // Clears all drag-and-drop slot state for the puzzle reset.
        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity9)) // Loops through each matching puzzle object.
        { // Starts this block.
            card.ResetSyncedFeedbackColor(); // Clears synced feedback color on this card.
            card.ResetCard(); // Returns this draggable card to its starting state.
        } // Ends this block.
        deathLapsesManager?.ResetPuzzle(); // Resets that puzzle handler to its starting state.

        CurrentStage = PuzzleStage.Puzzle11; // Updates the current puzzle stage.

        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            FindObjectOfType<GameTimerManager>()?.StartTimer(); // Restarts the shared puzzle timer from the master client.
    } // Ends this block.


    // ===== Stage completion handlers =====
    public void MarkCorrect(string symptom) => correctSymptomsFromPuzzle1.Add(symptom); // Adds this value to the puzzle-state collection.
    public bool IsSymptomCorrect(string symptom) => correctSymptomsFromPuzzle1.Contains(symptom); // Handles this part of the puzzle state flow.

    private void HandlePuzzle1Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 1 complete! Now starting Puzzle 2..."); // Writes puzzle-state diagnostic information to the Unity console.

        Puzzle1Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.
        Puzzle2Panel?.SetActive(true); // Shows or hides this puzzle-related UI object.

        CurrentStage = PuzzleStage.Puzzle2; // Updates the current puzzle stage.
    } // Ends this block.

    private void HandlePuzzle2Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 2 complete! You win!"); // Writes puzzle-state diagnostic information to the Unity console.

        Puzzle2Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.

        GetKeyUponCompletion(puzzle1Object, "Time Crystal1"); // Handles this part of the puzzle state flow.

        CurrentStage = PuzzleStage.Puzzle3; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.

    } // Ends this block.

    public void HandlePuzzle3Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 3 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle3Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.
        Puzzle4Panel?.SetActive(true); // Shows or hides this puzzle-related UI object.
        CurrentStage = PuzzleStage.Puzzle4; // Updates the current puzzle stage.
    } // Ends this block.

    public void HandlePuzzle4Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 4 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle4Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.


        GetKeyUponCompletion(puzzle2Object, "Time Crystal2"); // Handles this part of the puzzle state flow.

        CurrentStage = PuzzleStage.Puzzle5; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.

    } // Ends this block.

    public void HandlePuzzle5Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 5 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle5Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.
        //Puzzle6Panel?.SetActive(true);
        CurrentStage = PuzzleStage.Puzzle6; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.
    } // Ends this block.
    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_DiaryGoToStage(int newStage) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        if (diaryPuzzleManager != null) // Checks whether this puzzle-state condition requires action.
            diaryPuzzleManager.GoToStage(newStage); // Updates or calls this puzzle-state object.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_DiaryAdvanceToNextStage() // Runs a synchronized Photon RPC action.
    { // Starts this block.
        diaryPuzzleManager?.GoToNextStage(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_Activity2GoToPart2() // Runs a synchronized Photon RPC action.
    { // Starts this block.
        activity2PuzzleManager?.GoToPart2(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_InsulinGoToPart2() // Runs a synchronized Photon RPC action.
    { // Starts this block.
        insulinPuzzleManager?.GoToPart2(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_DeathLapsesGoToStage2() // Runs a synchronized Photon RPC action.
    { // Starts this block.
        deathLapsesManager?.GoToStage2(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    public void BroadcastInsulinPart2Transition() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom) // Checks whether this puzzle-state condition requires action.
            photonView.RPC(nameof(RPC_InsulinGoToPart2), RpcTarget.All); // Sends a synchronized RPC to Photon clients.
        else // Runs when the previous condition is false.
            insulinPuzzleManager?.GoToPart2(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    public void HandlePuzzle6Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 6 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle6Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.

        GetKeyUponCompletion(puzzle3Object, "Time Crystal3"); // Handles this part of the puzzle state flow.

        CurrentStage = PuzzleStage.Puzzle7; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    public void HandlePuzzle7Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 7 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle7Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.
        Puzzle8Panel?.SetActive(true); // Shows or hides this puzzle-related UI object.

        CurrentStage = PuzzleStage.Puzzle8; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    public void HandlePuzzle8Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 8 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle8Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.

        CurrentStage = PuzzleStage.Puzzle9; // Updates the current puzzle stage.

        GetKeyUponCompletion(puzzle4Object, "Time Crystal4"); // Handles this part of the puzzle state flow.
        ResetTimer(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    public void HandlePuzzle9Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 9 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle9AcrossHint?.SetActive(false); // Shows or hides this puzzle-related UI object.
        Puzzle9ImageHint?.SetActive(true); // Shows or hides this puzzle-related UI object.

        CurrentStage = PuzzleStage.Puzzle10; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    public void HandlePuzzle10Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 10 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle10Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.

        CurrentStage = PuzzleStage.Puzzle11; // Updates the current puzzle stage.
        ResetTimer(); // Handles this part of the puzzle state flow.
       
    } // Ends this block.

    public void HandlePuzzle11Complete() // Handles this part of the puzzle state flow.
    { // Starts this block.
        Debug.Log("Puzzle 11 completed correctly!"); // Writes puzzle-state diagnostic information to the Unity console.
        Puzzle11Panel?.SetActive(false); // Shows or hides this puzzle-related UI object.

        GetKeyUponCompletion(puzzle5Object, "Time Crystal5"); // Handles this part of the puzzle state flow.
        ResetTimer(); // Handles this part of the puzzle state flow.
        movementTargetPuzzle11?.GetComponent<MovementTargetForward>().Interact(); // Calls this puzzle component only when it exists.
        GameSessionCsvLogger.Instance?.WriteCsvIfNeeded(); // Records puzzle-session information to the CSV logger when available.
      

        //if (PuzzleScoreTracker.Instance != null)
        //    PuzzleScoreTracker.Instance.ShowEndScreen();
    } // Ends this block.

    public void ResetTimer() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!PhotonNetwork.IsMasterClient) return; // Checks whether this puzzle-state condition requires action.

        var timerManager = FindObjectOfType<GameTimerManager>(); // Handles this part of the puzzle state flow.
        if (timerManager == null) return; // Checks whether this puzzle-state condition requires action.

        Debug.Log("Reset timer"); // Writes puzzle-state diagnostic information to the Unity console.
        timerManager.StartTimer(); // Updates or calls this puzzle-state object.

        PhotonView timerView = timerManager.GetComponent<PhotonView>(); // Handles this part of the puzzle state flow.
        if (timerView != null) timerView.RPC("HideTimeWarning", RpcTarget.All); // Checks whether this puzzle-state condition requires action.
        else Debug.LogError("GameTimerManager has no PhotonView attached!"); // Handles this part of the puzzle state flow.
    } // Ends this block.

    private void GetKeyUponCompletion(GameObject puzzleTriggerObject, string keyName) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (puzzleTriggerObject != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var trigger = puzzleTriggerObject.GetComponent<PuzzleTrigger>(); // Handles this part of the puzzle state flow.

            // ? Only destroy if trigger is NOT flagged as persistent
            if (trigger == null || !trigger.dontDestroyOnCompletion) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                PhotonView puzzlePV = puzzleTriggerObject.GetComponent<PhotonView>(); // Handles this part of the puzzle state flow.
                if (puzzlePV != null) // Checks whether this puzzle-state condition requires action.
                    photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID); // Sends a synchronized RPC to Photon clients.
                else // Runs when the previous condition is false.
                    Debug.LogError("Puzzle object does not have a PhotonView component!"); // Writes a puzzle-state error to the Unity console.
            } // Ends this block.
        } // Ends this block.

        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, keyName); // Sends a synchronized RPC to Photon clients.
    } // Ends this block.


    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_DestroyPuzzleObjectByID(int photonViewID) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        PhotonView targetView = PhotonView.Find(photonViewID); // Handles this part of the puzzle state flow.
        if (targetView != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            targetView.gameObject.SetActive(false); // Shows or hides this puzzle-related UI object.
            Debug.Log($"Destroyed object {targetView.gameObject.name} across all players."); // Writes puzzle-state diagnostic information to the Unity console.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            Debug.LogWarning($"No PhotonView found with ID {photonViewID}."); // Writes a puzzle-state warning to the Unity console.
        } // Ends this block.
    } // Ends this block.

    [PunRPC] private void RPC_AddSharedInventoryItem(string itemName) => StartCoroutine(DelayedAddItem(itemName)); // Marks the next method as callable through Photon RPC.
    [PunRPC] private void RPC_AddSharedClue(string clueId) => StartCoroutine(DelayedAddClue(clueId)); // Marks the next method as callable through Photon RPC.
    [PunRPC] private void RPC_ShowSharedCluePopup(string popupText) => CluePopupUI.Instance?.Show(popupText); // Marks the next method as callable through Photon RPC.

    private IEnumerator DelayedAddItem(string itemName) // Runs a delayed puzzle-state coroutine.
    { // Starts this block.
        while (InventorySystem.Instance == null || InventoryUIManager.Instance == null) yield return null; // Handles this part of the puzzle state flow.
        InventorySystem.Instance.AddItem(itemName); // Updates or calls this puzzle-state object.
        InventoryUIManager.Instance.UpdateInventoryUI(); // Updates or calls this puzzle-state object.
    } // Ends this block.

    private IEnumerator DelayedAddClue(string clueId) // Runs a delayed puzzle-state coroutine.
    { // Starts this block.
        while (ClueSystem.Instance == null) // Handles this part of the puzzle state flow.
            yield return null; // Waits before continuing this coroutine.

        ClueSystem.Instance.AddClue(clueId); // Updates or calls this puzzle-state object.
    } // Ends this block.

    public void DelayThen(Action callback, float delay = 10f) => StartCoroutine(DelayedCallback(callback, delay)); // Handles this part of the puzzle state flow.
    private IEnumerator DelayedCallback(Action callback, float delay) { yield return new WaitForSeconds(delay); callback?.Invoke(); } // Runs a delayed puzzle-state coroutine.

    private void HandlePuzzleCompletion(PuzzleStage stage) // Handles this part of the puzzle state flow.
    { // Starts this block.
        switch (stage) // Selects behavior based on the current puzzle stage or state.
        { // Starts this block.
            case PuzzleStage.Puzzle0: Debug.Log("Opening comic closed. Waiting for Puzzle 1."); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle1: HandlePuzzle1Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle2: HandlePuzzle2Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle3: HandlePuzzle3Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle4: HandlePuzzle4Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle5: HandlePuzzle5Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle6: HandlePuzzle6Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle7: HandlePuzzle7Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle8: HandlePuzzle8Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle9: HandlePuzzle9Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle10: HandlePuzzle10Complete(); break; // Handles this specific puzzle stage or switch value.
            case PuzzleStage.Puzzle11: HandlePuzzle11Complete(); break; // Handles this specific puzzle stage or switch value.
            default: Debug.LogWarning("Unhandled puzzle completion"); break; // Handles any switch value not matched above.
        } // Ends this block.

        AwardClueReward(stage); // Handles this part of the puzzle state flow.
        SyncObjectivePointerToCurrentStage(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    private void AwardClueReward(PuzzleStage stage) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!TryGetClueReward(stage, out var reward)) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        string clueId = reward.clueId?.Trim(); // Stores a puzzle-state text value.
        if (string.IsNullOrWhiteSpace(clueId)) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        string popupText = BuildCluePopupText(reward); // Stores a puzzle-state text value.

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            CluePopupUI.Instance?.Show(popupText); // Updates or calls this puzzle-state object.
            photonView.RPC(nameof(RPC_AddSharedClue), RpcTarget.AllBuffered, clueId); // Sends a synchronized RPC to Photon clients.
            photonView.RPC(nameof(RPC_ShowSharedCluePopup), RpcTarget.Others, popupText); // Sends a synchronized RPC to Photon clients.
            return; // Leaves the current method early.
        } // Ends this block.

        CluePopupUI.Instance?.Show(popupText); // Updates or calls this puzzle-state object.
        StartCoroutine(DelayedAddClue(clueId)); // Handles this part of the puzzle state flow.
    } // Ends this block.

    private bool TryGetClueReward(PuzzleStage stage, out PuzzleClueReward reward) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (puzzleClueRewards != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            foreach (var entry in puzzleClueRewards) // Loops through each matching puzzle object.
            { // Starts this block.
                if (entry == null || entry.stage != stage) // Checks whether this puzzle-state condition requires action.
                    continue; // Handles this part of the puzzle state flow.

                reward = entry; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            } // Ends this block.
        } // Ends this block.

        reward = null; // Handles this part of the puzzle state flow.
        return false; // Reports failure to the caller.
    } // Ends this block.

    private string BuildCluePopupText(PuzzleClueReward reward) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (reward != null && !string.IsNullOrWhiteSpace(reward.popupMessage)) // Checks whether this puzzle-state condition requires action.
            return reward.popupMessage.Trim(); // Returns the computed puzzle-state value to the caller.

        return $"Clue picked up: {FormatClueName(reward?.clueId)}"; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private static string FormatClueName(string clueId) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (string.IsNullOrWhiteSpace(clueId)) // Checks whether this puzzle-state condition requires action.
            return "New clue"; // Returns the computed puzzle-state value to the caller.

        return clueId.Replace('_', ' ').Trim(); // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    public void BroadcastStageTransition(PuzzleStage stage) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
                photonView.RPC(nameof(RPC_SetStageForAll), RpcTarget.AllBuffered, (int)stage); // Sends a synchronized RPC to Photon clients.

            return; // Leaves the current method early.
        } // Ends this block.

        SetCurrentStage(stage); // Handles this part of the puzzle state flow.
    } // Ends this block.

    // Use RPC to advance for everyone after delay (fair-turn compatible)
    public void Submit() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var result = activity2PuzzleManager.SubmitActivity2Stage(); // Handles this part of the puzzle state flow.

            if (result == Activity2PuzzleManager.Activity2SubmitResult.Incorrect) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                if (PuzzleScoreTracker.Instance != null) // Checks whether this puzzle-state condition requires action.
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage); // Updates or calls this puzzle-state object.

                RecordCurrentPuzzleAnswer(false); // Handles this part of the puzzle state flow.
                Debug.Log("Activity 2 stage incorrect."); // Writes puzzle-state diagnostic information to the Unity console.
                return; // Leaves the current method early.
            } // Ends this block.

            if (result == Activity2PuzzleManager.Activity2SubmitResult.Advanced) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
                photonView.RPC(nameof(RPC_Activity2GoToPart2), RpcTarget.All); // Sends a synchronized RPC to Photon clients.
                return; // Leaves the current method early.
            } // Ends this block.

            RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition)); // Handles this part of the puzzle state flow.
            return; // Leaves the current method early.
        } // Ends this block.

        // ? Special handling for Puzzle 6 multi-page diary
        if (CurrentStage == PuzzleStage.Puzzle6 && diaryPuzzleManager != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var result = diaryPuzzleManager.SubmitDiaryStage(); // Handles this part of the puzzle state flow.

            if (result == DiaryPuzzleManager.DiarySubmitResult.Incorrect) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                if (PuzzleScoreTracker.Instance != null) // Checks whether this puzzle-state condition requires action.
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage); // Updates or calls this puzzle-state object.

                RecordCurrentPuzzleAnswer(false); // Handles this part of the puzzle state flow.
                Debug.Log("Diary stage incorrect."); // Writes puzzle-state diagnostic information to the Unity console.
                return; // Leaves the current method early.
            } // Ends this block.

            if (result == DiaryPuzzleManager.DiarySubmitResult.Advanced) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
                photonView.RPC(nameof(RPC_DiaryAdvanceToNextStage), RpcTarget.All); // Sends a synchronized RPC to Photon clients.
                return; // IMPORTANT: do not advance puzzle stage yet // Handles this part of the puzzle state flow.
            } // Ends this block.

            // Completed final diary page -> proceed with normal stage-advance flow
            RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition)); // Handles this part of the puzzle state flow.
            return; // Leaves the current method early.
        } // Ends this block.

        if (CurrentStage == PuzzleStage.Puzzle11 && deathLapsesManager != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var result = deathLapsesManager.SubmitDeathLapsesStage(); // Handles this part of the puzzle state flow.

            if (result == DeathLapsesManager.DeathLapsesSubmitResult.Incorrect) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                if (PuzzleScoreTracker.Instance != null) // Checks whether this puzzle-state condition requires action.
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage); // Updates or calls this puzzle-state object.

                RecordCurrentPuzzleAnswer(false); // Handles this part of the puzzle state flow.
                Debug.Log("Death Lapses stage incorrect."); // Writes puzzle-state diagnostic information to the Unity console.
                return; // Leaves the current method early.
            } // Ends this block.

            if (result == DeathLapsesManager.DeathLapsesSubmitResult.Advanced) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
                photonView.RPC(nameof(RPC_DeathLapsesGoToStage2), RpcTarget.All); // Sends a synchronized RPC to Photon clients.
                return; // Leaves the current method early.
            } // Ends this block.

            RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition)); // Handles this part of the puzzle state flow.
            return; // Leaves the current method early.
        } // Ends this block.

        // ? Default flow for all other puzzles
        if (puzzleHandlers.TryGetValue(CurrentStage, out IPuzzle puzzle)) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            if (puzzle.IsSolved()) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                RecordCurrentPuzzleAnswer(true); // Handles this part of the puzzle state flow.
                StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition)); // Handles this part of the puzzle state flow.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                if (PuzzleScoreTracker.Instance != null) // Checks whether this puzzle-state condition requires action.
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage); // Updates or calls this puzzle-state object.

                RecordCurrentPuzzleAnswer(false); // Handles this part of the puzzle state flow.
                Debug.Log($"Puzzle {CurrentStage} incomplete or incorrect."); // Writes puzzle-state diagnostic information to the Unity console.
            } // Ends this block.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            Debug.LogWarning($"No handler found for puzzle stage {CurrentStage}"); // Writes a puzzle-state warning to the Unity console.
        } // Ends this block.
    } // Ends this block.



    private void RecordCurrentPuzzleAnswer(bool isCorrect) // Handles this part of the puzzle state flow.
    { // Starts this block.
        string answerSnapshot = BuildCurrentAnswerSnapshot(); // Captures the current player input for this submission.
        if (CurrentStage == PuzzleStage.Puzzle6 && diaryPuzzleManager != null) // Diary pages are separate analytics rows.
            answerSnapshot = $"__PUZZLE_PART={diaryPuzzleManager.CurrentStage}__|{answerSnapshot}";
        else if (CurrentStage == PuzzleStage.Puzzle11 && deathLapsesManager != null) // Death Lapses parts are separate analytics rows.
            answerSnapshot = $"__PUZZLE_PART={deathLapsesManager.CurrentStage}__|{answerSnapshot}";
        else if (CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null) // Puzzle 4 parts are separate analytics rows.
            answerSnapshot = $"__PUZZLE_PART={activity2PuzzleManager.CurrentPart}__|{answerSnapshot}";
        else if (CurrentStage == PuzzleStage.Puzzle10 && insulinPuzzleManager != null) // Puzzle 10 parts are separate analytics rows.
            answerSnapshot = $"__PUZZLE_PART={insulinPuzzleManager.CurrentPart}__|{answerSnapshot}";

        GameSessionCsvLogger.Instance?.RecordPuzzleAnswer(CurrentStage, isCorrect, answerSnapshot, currentSubmitOwnerActorNumber); // Records puzzle-session information to the CSV logger when available.
    } // Ends this block.

    public void RecordPuzzle10PartAnswer(bool isCorrect) // Records the Puzzle 10 dropdown result before moving to the card section.
    {
        if (CurrentStage == PuzzleStage.Puzzle10)
            RecordCurrentPuzzleAnswer(isCorrect);
    }

    private string BuildCurrentAnswerSnapshot() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (CurrentStage == PuzzleStage.Puzzle9 && crosswordManager != null) // Crossword exports wrong words with clue coordinates.
            return crosswordManager.GetWrongAnswerSnapshot();
        GameObject panel = GetPanelForStage(CurrentStage); // Stores or creates a Unity GameObject reference.
        if (CurrentStage == PuzzleStage.Puzzle6 && diaryPuzzleManager != null && diaryPuzzleManager.ActivePanel != null) // Diary exports only the active page.
            panel = diaryPuzzleManager.ActivePanel;
        if (CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null && activity2PuzzleManager.ActivePanel != null) // Puzzle 4 exports only the active section.
            panel = activity2PuzzleManager.ActivePanel;
        if (CurrentStage == PuzzleStage.Puzzle10 && insulinPuzzleManager != null && insulinPuzzleManager.ActivePanel != null) // Puzzle 10 exports only the active section.
            panel = insulinPuzzleManager.ActivePanel;
        if (panel == null) // Checks whether this puzzle-state condition requires action.
            panel = currentPuzzlePanel; // Handles this part of the puzzle state flow.

        if (panel == null) // Checks whether this puzzle-state condition requires action.
            return "No active puzzle panel"; // Returns the computed puzzle-state value to the caller.

        var snapshot = new StringBuilder(); // Handles this part of the puzzle state flow.
        bool puzzle4Part2 = CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null && activity2PuzzleManager.CurrentPart == 2;

        if (!puzzle4Part2)
        foreach (DropSlot slot in panel.GetComponentsInChildren<DropSlot>(true)) // Loops through each matching puzzle object.
        { // Starts this block.
            if (slot == null) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            List<DraggableCard> cards = slot.GetCardsInSlot(); // Stores a list of puzzle-related values.
            if (cards.Count == 0) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            var cardList = new StringBuilder(); // Handles this part of the puzzle state flow.
            foreach (DraggableCard card in cards) // Loops through each matching puzzle object.
            { // Starts this block.
                bool useImageName = (CurrentStage == PuzzleStage.Puzzle3
                    || (CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null && activity2PuzzleManager.CurrentPart == 1)
                    || (CurrentStage == PuzzleStage.Puzzle10 && insulinPuzzleManager != null && insulinPuzzleManager.CurrentPart == 2))
                    && card != null && !string.IsNullOrWhiteSpace(card.FrontImageName);
                string cardLabel = useImageName
                    ? card.FrontImageName.Trim()
                    : GetCardAnswerLabel(card); // Puzzle 3 uses the card's source image name.
                AppendSeparated(cardList, cardLabel, ", "); // Handles this part of the puzzle state flow.
            } // Ends this block.

            AppendSeparated(snapshot, $"{slot.name}=[{cardList}]", "; "); // Handles this part of the puzzle state flow.
        } // Ends this block.

        foreach (TMP_Dropdown dropdown in panel.GetComponentsInChildren<TMP_Dropdown>(true)) // Loops through each matching puzzle object.
        { // Starts this block.
            if (dropdown == null) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            string selectedOption = dropdown.value >= 0 && dropdown.value < dropdown.options.Count // Stores a puzzle-state text value.
                ? dropdown.options[dropdown.value].text // Handles this part of the puzzle state flow.
                : dropdown.value.ToString(); // Handles this part of the puzzle state flow.

            AppendSeparated(snapshot, $"{dropdown.name}={selectedOption}", "; "); // Handles this part of the puzzle state flow.
        } // Ends this block.

        if (puzzle4Part2 || CurrentStage != PuzzleStage.Puzzle4)
        foreach (TMP_InputField input in panel.GetComponentsInChildren<TMP_InputField>(true)) // Loops through each matching puzzle object.
        { // Starts this block.
            if (input == null) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            AppendSeparated(snapshot, $"{input.name}={input.text}", "; "); // Handles this part of the puzzle state flow.
        } // Ends this block.

        foreach (PhotonSyncedUIButtonColor buttonState in panel.GetComponentsInChildren<PhotonSyncedUIButtonColor>(true)) // Loops through each matching puzzle object.
        { // Starts this block.
            if (buttonState == null || !buttonState.IsSelected || IsManagedSubmitButton(buttonState.gameObject)) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            string buttonLabel = CurrentStage == PuzzleStage.Puzzle3
                ? GetImageName(buttonState.gameObject)
                : GetObjectLabel(buttonState.gameObject); // Puzzle 3 uses the selected card's image name.
            AppendSeparated(snapshot, $"SelectedButton={buttonLabel}", "; "); // Handles this part of the puzzle state flow.
        } // Ends this block.

        foreach (LetterCell cell in panel.GetComponentsInChildren<LetterCell>(true)) // Loops through each matching puzzle object.
        { // Starts this block.
            if (cell == null || !cell.IsSelected) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            AppendSeparated(snapshot, $"Cell({cell.gridPosition.x},{cell.gridPosition.y})={cell.GetLetter()}", "; "); // Handles this part of the puzzle state flow.
        } // Ends this block.

        return snapshot.Length > 0 ? snapshot.ToString() : "No answer selected"; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private static void AppendSeparated(StringBuilder builder, string value, string separator) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (string.IsNullOrWhiteSpace(value)) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        if (builder.Length > 0) // Checks whether this puzzle-state condition requires action.
            builder.Append(separator); // Updates or calls this puzzle-state object.

        builder.Append(value.Trim()); // Updates or calls this puzzle-state object.
    } // Ends this block.

    private static string GetCardAnswerLabel(DraggableCard card) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (card == null) // Checks whether this puzzle-state condition requires action.
            return "Unknown card"; // Returns the computed puzzle-state value to the caller.

        if (!string.IsNullOrWhiteSpace(card.symptomName)) // Checks whether this puzzle-state condition requires action.
            return card.symptomName.Trim(); // Returns the computed puzzle-state value to the caller.

        if (!string.IsNullOrWhiteSpace(card.stepName)) // Checks whether this puzzle-state condition requires action.
            return card.stepName.Trim(); // Returns the computed puzzle-state value to the caller.

        if (!string.IsNullOrWhiteSpace(card.lapsesAnswer)) // Checks whether this puzzle-state condition requires action.
            return card.lapsesAnswer.Trim(); // Returns the computed puzzle-state value to the caller.

        return GetObjectLabel(card.gameObject); // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private static string GetObjectLabel(GameObject target) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (target == null) // Checks whether this puzzle-state condition requires action.
            return "Unknown"; // Returns the computed puzzle-state value to the caller.

        TMP_Text text = target.GetComponentInChildren<TMP_Text>(true); // Stores a TextMeshPro text reference.
        if (text != null && !string.IsNullOrWhiteSpace(text.text)) // Checks whether this puzzle-state condition requires action.
            return text.text.Trim(); // Returns the computed puzzle-state value to the caller.

        return target.name; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private static string GetImageName(GameObject target) // Gets the image asset name used by a Puzzle 3 card.
    {
        if (target == null)
            return "Unknown";

        Image image = target.GetComponentInChildren<Image>(true);
        return image != null && image.sprite != null ? image.sprite.name : GetObjectLabel(target);
    }

    private IEnumerator DelayAndAdvance(PuzzleStage stage, float delay) // Runs a delayed puzzle-state coroutine.
    { // Starts this block.
        yield return new WaitForSeconds(delay); // Waits before continuing this coroutine.
        photonView.RPC(nameof(RPC_AdvanceStageForAll), RpcTarget.AllBuffered, (int)stage); // Sends a synchronized RPC to Photon clients.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_SetStageForAll(int stageInt) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        SetCurrentStage((PuzzleStage)stageInt); // Handles this part of the puzzle state flow.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_AdvanceStageForAll(int stageInt) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        var stage = (PuzzleStage)stageInt; // Handles this part of the puzzle state flow.
        HandlePuzzleCompletion(stage); // Handles this part of the puzzle state flow.
        RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.

        // === CHANGE: Do NOT bump the round here ===
        // Master only unlocks and checks if everyone has played. Round will bump inside MaybeAdvanceRoundIfEveryonePlayed().
        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.

            // release lock for next player
            var unlock = new Hashtable // Handles this part of the puzzle state flow.
            { // Starts this block.
                [RP_PUZZLE_LOCKED] = false, // Handles this part of the puzzle state flow.
                [RP_LOCK_OWNER] = -1 // Handles this part of the puzzle state flow.
            }; // Ends this initializer block.
            room.SetCustomProperties(unlock); // Writes updated custom properties to Photon.

            // If everyone has taken a turn, bump the round (which resets PP_PLAYED_ROUND on all via OnRoomPropertiesUpdate).
            MaybeAdvanceRoundIfEveryonePlayed(); // Handles this part of the puzzle state flow.
        } // Ends this block.
    } // Ends this block.

    private void SetCurrentStage(PuzzleStage stage) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (CurrentStage == stage) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        CurrentStage = stage; // Updates the current puzzle stage.
        SyncObjectivePointerToCurrentStage(); // Handles this part of the puzzle state flow.
        RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.
    } // Ends this block.

    public void SyncObjectivePointerToCurrentStage() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (ObjectivePointer.Instance == null) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        if (TryGetObjectivePathForStage(CurrentStage, out string pathId)) // Checks whether this puzzle-state condition requires action.
            ObjectivePointer.Instance.StartPath(pathId); // Updates or calls this puzzle-state object.
        else // Runs when the previous condition is false.
            ObjectivePointer.Instance.ClearTarget(); // Updates or calls this puzzle-state object.
    } // Ends this block.

    private bool TryGetObjectivePathForStage(PuzzleStage stage, out string pathId) // Handles this part of the puzzle state flow.
    { // Starts this block.
        switch (stage) // Selects behavior based on the current puzzle stage or state.
        { // Starts this block.
            case PuzzleStage.Puzzle3: // Handles this specific puzzle stage or switch value.
                pathId = "Canteen"; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            case PuzzleStage.Puzzle5: // Handles this specific puzzle stage or switch value.
                pathId = "Physician Room"; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            case PuzzleStage.Puzzle7: // Handles this specific puzzle stage or switch value.
                pathId = "Pharmacy"; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            case PuzzleStage.Puzzle9: // Handles this specific puzzle stage or switch value.
                pathId = "Nurse Education Room"; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            case PuzzleStage.Puzzle11: // Handles this specific puzzle stage or switch value.
                pathId = "Morturary"; // Handles this part of the puzzle state flow.
                return true; // Reports success to the caller.
            default: // Handles any switch value not matched above.
                pathId = null; // Handles this part of the puzzle state flow.
                return false; // Reports failure to the caller.
        } // Ends this block.
    } // Ends this block.

    private GameObject GetPanelForStage(PuzzleStage stage) => // Handles this part of the puzzle state flow.
        (stagePanels != null && stagePanels.TryGetValue(stage, out var panel)) ? panel : null; // Attempts to read a stored puzzle-state value.

    public void OpenCurrentStagePanel(int submitOwnerActorNumber = -1) // Handles this part of the puzzle state flow.
    { // Starts this block.
        int resolvedSubmitOwnerActorNumber = ResolveSubmitOwnerActorNumber(submitOwnerActorNumber); // Stores an integer puzzle-state value.
        currentSubmitOwnerActorNumber = resolvedSubmitOwnerActorNumber; // Updates which actor owns submit controls.
        MarkLocalPuzzleReentryAccess(CurrentStage, resolvedSubmitOwnerActorNumber); // Handles this part of the puzzle state flow.

        var panel = GetPanelForStage(CurrentStage); // Handles this part of the puzzle state flow.
        if (panel != null) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            panel.SetActive(true); // Shows or hides this puzzle-related UI object.
            currentPuzzlePanel = panel; // Updates the currently tracked puzzle panel.
            RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.
        } // Ends this block.
    } // Ends this block.
   
    public void CloseCurrentPuzzle() // Handles this part of the puzzle state flow.
    { // Starts this block.
        var panel = GetPanelForStage(CurrentStage); // Handles this part of the puzzle state flow.
        if (panel != null && panel.activeSelf) { panel.SetActive(false); Debug.Log($"Closed {panel.name}"); } // Checks whether this puzzle-state condition requires action.
        else Debug.Log("No current puzzle panel to close."); // Handles this part of the puzzle state flow.
    } // Ends this block.

    // Close a specific puzzle panel from a UI button (Inspector selects stage as int)
    public void ClosePuzzle(int stageInt) // Closes the current puzzle panel and releases local UI state.
    { // Starts this block.
        PuzzleStage stage = (PuzzleStage)stageInt; // Stores a puzzle stage value.
        var panel = GetPanelForStage(stage); // Handles this part of the puzzle state flow.

        if (panel != null && panel.activeSelf) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            panel.SetActive(false); // Shows or hides this puzzle-related UI object.
            Debug.Log($"Closed {stage} ({panel.name})"); // Writes puzzle-state diagnostic information to the Unity console.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            Debug.Log($"No active panel to close for stage: {stage}"); // Writes puzzle-state diagnostic information to the Unity console.
        } // Ends this block.
    } // Ends this block.

    // ==== Fair-turn system additions ====

    private void EnsureRoomProps() // Ensures required Photon room properties exist.
    { // Starts this block.
        if (!PhotonNetwork.IsMasterClient) return; // Checks whether this puzzle-state condition requires action.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.
        var changed = new Hashtable(); // Handles this part of the puzzle state flow.

        if (!room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED)) changed[RP_PUZZLE_LOCKED] = false; // Checks whether this puzzle-state condition requires action.
        if (!room.CustomProperties.ContainsKey(RP_LOCK_OWNER)) changed[RP_LOCK_OWNER] = -1; // Checks whether this puzzle-state condition requires action.
        if (!room.CustomProperties.ContainsKey(RP_ROUND)) changed[RP_ROUND] = 1; // Checks whether this puzzle-state condition requires action.

        if (changed.Count > 0) room.SetCustomProperties(changed); // Checks whether this puzzle-state condition requires action.
    } // Ends this block.

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) // Responds to Photon room-property changes.
    { // Starts this block.
        if (propertiesThatChanged.ContainsKey(RP_ROUND)) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var me = PhotonNetwork.LocalPlayer; // Handles this part of the puzzle state flow.
            var meProps = new Hashtable { [PP_PLAYED_ROUND] = false }; // Handles this part of the puzzle state flow.
            me.SetCustomProperties(meProps); // Writes updated custom properties to Photon.
        } // Ends this block.

        if (propertiesThatChanged.ContainsKey(RP_LOCK_OWNER)) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            int lockOwner = propertiesThatChanged[RP_LOCK_OWNER] is int owner ? owner : -1; // Stores an integer puzzle-state value.
            currentSubmitOwnerActorNumber = IsActorInRoom(lockOwner) ? lockOwner : -1; // Updates which actor owns submit controls.

            if (lockOwner < 0) // Checks whether this puzzle-state condition requires action.
                ClearLocalPuzzleAccess(); // Handles this part of the puzzle state flow.
        } // Ends this block.

        if (propertiesThatChanged.ContainsKey(RP_PUZZLE_LOCKED) && // Checks whether this puzzle-state condition requires action.
            propertiesThatChanged[RP_PUZZLE_LOCKED] is bool locked && // Handles this part of the puzzle state flow.
            !locked) // Handles this part of the puzzle state flow.
        { // Starts this block.
            ClearLocalPuzzleAccess(); // Handles this part of the puzzle state flow.
        } // Ends this block.

        RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.
    } // Ends this block.

    public override void OnPlayerLeftRoom(Player otherPlayer) // Handles cleanup when another player leaves the room.
    { // Starts this block.
        if (!PhotonNetwork.IsMasterClient) return; // Checks whether this puzzle-state condition requires action.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.

        int owner = -1; // Stores an integer puzzle-state value.
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER)) // Checks whether this puzzle-state condition requires action.
            owner = (int)room.CustomProperties[RP_LOCK_OWNER]; // Handles this part of the puzzle state flow.

        bool disconnectedPlayerOwnsSubmit = // Stores a boolean puzzle-state result.
            owner == otherPlayer.ActorNumber || // Handles this part of the puzzle state flow.
            currentSubmitOwnerActorNumber == otherPlayer.ActorNumber; // Updates which actor owns submit controls.

        if (disconnectedPlayerOwnsSubmit) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            int replacementOwner = GetRandomActiveActorNumber(otherPlayer.ActorNumber); // Stores an integer puzzle-state value.
            bool hasReplacementOwner = replacementOwner > 0; // Stores a boolean puzzle-state result.

            var ht = new Hashtable // Handles this part of the puzzle state flow.
            { // Starts this block.
                [RP_PUZZLE_LOCKED] = hasReplacementOwner, // Handles this part of the puzzle state flow.
                [RP_LOCK_OWNER] = replacementOwner // Handles this part of the puzzle state flow.
            }; // Ends this initializer block.
            room.SetCustomProperties(ht); // Writes updated custom properties to Photon.
            photonView.RPC(nameof(RPC_SetSubmitOwnerForAll), RpcTarget.All, replacementOwner); // Sends a synchronized RPC to Photon clients.
        } // Ends this block.

        MaybeAdvanceRoundIfEveryonePlayed(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_SetSubmitOwnerForAll(int actorNumber) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        currentSubmitOwnerActorNumber = IsActorInRoom(actorNumber) ? actorNumber : -1; // Updates which actor owns submit controls.
        RefreshSubmitButtonVisibility(); // Refreshes submit controls for the current ownership state.
    } // Ends this block.

    public bool CanLocalPlayerSubmitCurrentAttempt() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) // Checks whether this puzzle-state condition requires action.
            return true; // Reports success to the caller.

        currentSubmitOwnerActorNumber = ResolveSubmitOwnerActorNumber(currentSubmitOwnerActorNumber); // Updates which actor owns submit controls.

        if (currentSubmitOwnerActorNumber > 0) // Checks whether this puzzle-state condition requires action.
            return PhotonNetwork.LocalPlayer.ActorNumber == currentSubmitOwnerActorNumber; // Returns the computed puzzle-state value to the caller.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.
        if (room == null) // Checks whether this puzzle-state condition requires action.
            return true; // Reports success to the caller.

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) && // Stores a boolean puzzle-state result.
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED]; // Handles this part of the puzzle state flow.

        if (!locked) // Checks whether this puzzle-state condition requires action.
            return true; // Reports success to the caller.

        int owner = room.CustomProperties.ContainsKey(RP_LOCK_OWNER) // Stores an integer puzzle-state value.
            ? (int)room.CustomProperties[RP_LOCK_OWNER] // Handles this part of the puzzle state flow.
            : -1; // Handles this part of the puzzle state flow.

        return owner < 0 || owner == PhotonNetwork.LocalPlayer.ActorNumber; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private int ResolveSubmitOwnerActorNumber(int requestedOwnerActorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!PhotonNetwork.IsConnected) // Checks whether this puzzle-state condition requires action.
            return requestedOwnerActorNumber; // Returns the computed puzzle-state value to the caller.

        if (IsActorInRoom(requestedOwnerActorNumber)) // Checks whether this puzzle-state condition requires action.
            return requestedOwnerActorNumber; // Returns the computed puzzle-state value to the caller.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.
        if (room != null && // Checks whether this puzzle-state condition requires action.
            room.CustomProperties.ContainsKey(RP_LOCK_OWNER) && // Checks whether this puzzle-state key exists.
            room.CustomProperties[RP_LOCK_OWNER] is int lockOwner && // Updates or calls this puzzle-state object.
            IsActorInRoom(lockOwner)) // Handles this part of the puzzle state flow.
        { // Starts this block.
            return lockOwner; // Returns the computed puzzle-state value to the caller.
        } // Ends this block.

        return -1; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private bool IsActorInRoom(int actorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (actorNumber <= 0 || !PhotonNetwork.IsConnected) // Checks whether this puzzle-state condition requires action.
            return false; // Reports failure to the caller.

        foreach (var player in PhotonNetwork.PlayerList) // Loops through each matching puzzle object.
        { // Starts this block.
            if (player != null && !player.IsInactive && player.ActorNumber == actorNumber) // Checks whether this puzzle-state condition requires action.
                return true; // Reports success to the caller.
        } // Ends this block.

        return false; // Reports failure to the caller.
    } // Ends this block.

    private int GetRandomActiveActorNumber(int excludedActorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        var activePlayers = new List<Player>(); // Handles this part of the puzzle state flow.
        foreach (var player in PhotonNetwork.PlayerList) // Loops through each matching puzzle object.
        { // Starts this block.
            if (player != null && !player.IsInactive && player.ActorNumber != excludedActorNumber) // Checks whether this puzzle-state condition requires action.
                activePlayers.Add(player); // Adds this value to the puzzle-state collection.
        } // Ends this block.

        if (activePlayers.Count == 0) // Checks whether this puzzle-state condition requires action.
            return -1; // Returns the computed puzzle-state value to the caller.

        int randomIndex = UnityEngine.Random.Range(0, activePlayers.Count); // Stores an integer puzzle-state value.
        return activePlayers[randomIndex].ActorNumber; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private void RefreshSubmitButtonVisibility() // Updates submit-button visibility based on puzzle lock ownership.
    { // Starts this block.
        bool shouldShowSubmitButtons = CanLocalPlayerSubmitCurrentAttempt(); // Stores a boolean puzzle-state result.

        foreach (var panel in stagePanels.Values) // Loops through each matching puzzle object.
        { // Starts this block.
            if (panel == null) continue; // Checks whether this puzzle-state condition requires action.

            foreach (var button in panel.GetComponentsInChildren<Button>(true)) // Loops through each matching puzzle object.
            { // Starts this block.
                if (button == null || !IsManagedSubmitButton(button.gameObject)) // Checks whether this puzzle-state condition requires action.
                    continue; // Handles this part of the puzzle state flow.

                button.gameObject.SetActive(shouldShowSubmitButtons); // Shows or hides this puzzle-related UI object.
            } // Ends this block.
        } // Ends this block.

        pictureGridManager?.RefreshSubmitButtonVisibility(); // Calls this puzzle component only when it exists.
    } // Ends this block.

    private bool IsManagedSubmitButton(GameObject buttonObject) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (buttonObject == null) // Checks whether this puzzle-state condition requires action.
            return false; // Reports failure to the caller.

        if (pictureGridManager != null && pictureGridManager.SubmitButton == buttonObject) // Checks whether this puzzle-state condition requires action.
            return false; // Reports failure to the caller.

        return buttonObject.name.IndexOf("submit", StringComparison.OrdinalIgnoreCase) >= 0; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    // Gate attempt; called by triggers
    // Gate attempt; called by triggers
    public bool TryBeginAttempt(out string reason) // Handles this part of the puzzle state flow.
    { // Starts this block.
        reason = null; // Handles this part of the puzzle state flow.

        var me = PhotonNetwork.LocalPlayer; // Handles this part of the puzzle state flow.
        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) && // Stores a boolean puzzle-state result.
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED]; // Handles this part of the puzzle state flow.

        int owner = -1; // Stores an integer puzzle-state value.
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER)) // Checks whether this puzzle-state condition requires action.
            owner = (int)room.CustomProperties[RP_LOCK_OWNER]; // Handles this part of the puzzle state flow.

        bool iPlayed = me.CustomProperties != null && // Stores a boolean puzzle-state result.
                       me.CustomProperties.ContainsKey(PP_PLAYED_ROUND) && // Checks whether this puzzle-state key exists.
                       (bool)me.CustomProperties[PP_PLAYED_ROUND]; // Handles this part of the puzzle state flow.

        // ? 1) If the puzzle is locked by ME, always allow me to reopen/close/reopen.
        if (locked && owner == me.ActorNumber) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            MarkLocalActivePuzzleLock(me.ActorNumber); // Handles this part of the puzzle state flow.
            GameSessionCsvLogger.Instance?.RecordPuzzleEntered(CurrentStage, me.ActorNumber, me.NickName); // Records puzzle-session information to the CSV logger when available.
            return true; // don't change any props; I still hold the lock // Returns the computed puzzle-state value to the caller.
        } // Ends this block.

        if (localPlayerHasActivePuzzleLock && localActivePuzzleLockOwnerActorNumber == me.ActorNumber) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
            { // Starts this block.
                room.SetCustomProperties(new Hashtable // Writes updated custom properties to Photon.
                { // Starts this block.
                    [RP_PUZZLE_LOCKED] = true, // Handles this part of the puzzle state flow.
                    [RP_LOCK_OWNER] = me.ActorNumber // Handles this part of the puzzle state flow.
                }); // Handles this part of the puzzle state flow.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                photonView.RPC(nameof(RPC_RequestLockFromMaster), RpcTarget.MasterClient, me.ActorNumber); // Sends a synchronized RPC to Photon clients.
            } // Ends this block.

            GameSessionCsvLogger.Instance?.RecordPuzzleEntered(CurrentStage, me.ActorNumber, me.NickName); // Records puzzle-session information to the CSV logger when available.
            return true; // Reports success to the caller.
        } // Ends this block.

        if (locked && HasLocalPuzzleReentryAccess(CurrentStage, owner)) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            currentSubmitOwnerActorNumber = owner; // Updates which actor owns submit controls.
            GameSessionCsvLogger.Instance?.RecordPuzzleEntered(CurrentStage, me.ActorNumber, me.NickName); // Records puzzle-session information to the CSV logger when available.
            return true; // Reports success to the caller.
        } // Ends this block.

        // 2) If someone else holds it, block.
        if (locked && owner != me.ActorNumber) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            reason = "Another player is currently holding this puzzle."; // Handles this part of the puzzle state flow.
            return false; // Reports failure to the caller.
        } // Ends this block.

        // 3) If it's not locked and I've already taken my turn this round, block starting a (new) attempt.
        if (iPlayed) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            reason = "You�ve already taken your turn this round."; // Handles this part of the puzzle state flow.
            MaybeAdvanceRoundIfEveryonePlayed(); // Handles this part of the puzzle state flow.
            return false; // Reports failure to the caller.
        } // Ends this block.

        // 4) Otherwise acquire the lock and mark my turn as spent.
        if (PhotonNetwork.IsMasterClient) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var ht = new Hashtable // Handles this part of the puzzle state flow.
            { // Starts this block.
                [RP_PUZZLE_LOCKED] = true, // Handles this part of the puzzle state flow.
                [RP_LOCK_OWNER] = me.ActorNumber // Handles this part of the puzzle state flow.
            }; // Ends this initializer block.
            room.SetCustomProperties(ht); // Writes updated custom properties to Photon.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            photonView.RPC(nameof(RPC_RequestLockFromMaster), RpcTarget.MasterClient, me.ActorNumber); // Sends a synchronized RPC to Photon clients.
        } // Ends this block.

        me.SetCustomProperties(new Hashtable { [PP_PLAYED_ROUND] = true }); // Writes updated custom properties to Photon.
        MarkLocalActivePuzzleLock(me.ActorNumber); // Handles this part of the puzzle state flow.
        GameSessionCsvLogger.Instance?.RecordPuzzleEntered(CurrentStage, me.ActorNumber, me.NickName); // Records puzzle-session information to the CSV logger when available.
        return true; // Reports success to the caller.
    } // Ends this block.


    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_RequestLockFromMaster(int actorNumber) // Runs a synchronized Photon RPC action.
    { // Starts this block.
        if (!PhotonNetwork.IsMasterClient) return; // Checks whether this puzzle-state condition requires action.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) && // Stores a boolean puzzle-state result.
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED]; // Handles this part of the puzzle state flow.

        int owner = -1; // Stores an integer puzzle-state value.
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER)) // Checks whether this puzzle-state condition requires action.
            owner = (int)room.CustomProperties[RP_LOCK_OWNER]; // Handles this part of the puzzle state flow.

        if (!locked || owner == actorNumber) // Checks whether this puzzle-state condition requires action.
        { // Starts this block.
            var ht = new Hashtable { [RP_PUZZLE_LOCKED] = true, [RP_LOCK_OWNER] = actorNumber }; // Handles this part of the puzzle state flow.
            room.SetCustomProperties(ht); // Writes updated custom properties to Photon.
        } // Ends this block.
    } // Ends this block.

    private void MaybeAdvanceRoundIfEveryonePlayed() // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!PhotonNetwork.IsMasterClient) return; // Checks whether this puzzle-state condition requires action.

        foreach (var p in PhotonNetwork.PlayerList) // Loops through each matching puzzle object.
        { // Starts this block.
            if (!ShouldExpectPlayerTurn(p)) // Checks whether this puzzle-state condition requires action.
                continue; // Handles this part of the puzzle state flow.

            bool played = p.CustomProperties != null && // Stores a boolean puzzle-state result.
                          p.CustomProperties.ContainsKey(PP_PLAYED_ROUND) && // Checks whether this puzzle-state key exists.
                          (bool)p.CustomProperties[PP_PLAYED_ROUND]; // Handles this part of the puzzle state flow.
            if (!played) return; // someone still hasn't had a turn // Checks whether this puzzle-state condition requires action.
        } // Ends this block.

        var room = PhotonNetwork.CurrentRoom; // Handles this part of the puzzle state flow.
        int round = room.CustomProperties.ContainsKey(RP_ROUND) // Stores an integer puzzle-state value.
                    ? (int)room.CustomProperties[RP_ROUND] : 1; // Handles this part of the puzzle state flow.

        var ht = new Hashtable // Handles this part of the puzzle state flow.
        { // Starts this block.
            [RP_ROUND] = round + 1, // Handles this part of the puzzle state flow.
            [RP_PUZZLE_LOCKED] = false, // Handles this part of the puzzle state flow.
            [RP_LOCK_OWNER] = -1 // Handles this part of the puzzle state flow.
        }; // Ends this initializer block.
        room.SetCustomProperties(ht); // Writes updated custom properties to Photon.
    } // Ends this block.

    private bool ShouldExpectPlayerTurn(Player player) // Handles this part of the puzzle state flow.
    { // Starts this block.
        return player != null && !player.IsInactive; // Returns the computed puzzle-state value to the caller.
    } // Ends this block.

    private void MarkLocalActivePuzzleLock(int actorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (!IsLocalPlayerActor(actorNumber)) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        localPlayerHasActivePuzzleLock = true; // Updates local puzzle-lock ownership state.
        localActivePuzzleLockOwnerActorNumber = actorNumber; // Updates the local lock owner actor number.
        currentSubmitOwnerActorNumber = actorNumber; // Updates which actor owns submit controls.
    } // Ends this block.

    private void ClearLocalActivePuzzleLock() // Handles this part of the puzzle state flow.
    { // Starts this block.
        localPlayerHasActivePuzzleLock = false; // Updates local puzzle-lock ownership state.
        localActivePuzzleLockOwnerActorNumber = -1; // Updates the local lock owner actor number.
    } // Ends this block.

    private void MarkLocalPuzzleReentryAccess(PuzzleStage stage, int ownerActorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        if (ownerActorNumber <= 0) // Checks whether this puzzle-state condition requires action.
            return; // Leaves the current method early.

        localPlayerHasPuzzleReentryAccess = true; // Updates local puzzle re-entry access state.
        localPuzzleReentryStage = stage; // Updates which stage can be re-entered locally.
        localPuzzleReentryOwnerActorNumber = ownerActorNumber; // Updates the actor number tied to local re-entry.
    } // Ends this block.

    private bool HasLocalPuzzleReentryAccess(PuzzleStage stage, int ownerActorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        return localPlayerHasPuzzleReentryAccess && // Returns the computed puzzle-state value to the caller.
               localPuzzleReentryStage == stage && // Updates which stage can be re-entered locally.
               localPuzzleReentryOwnerActorNumber == ownerActorNumber && // Updates the actor number tied to local re-entry.
               ownerActorNumber > 0; // Handles this part of the puzzle state flow.
    } // Ends this block.

    private void ClearLocalPuzzleReentryAccess() // Handles this part of the puzzle state flow.
    { // Starts this block.
        localPlayerHasPuzzleReentryAccess = false; // Updates local puzzle re-entry access state.
        localPuzzleReentryStage = PuzzleStage.Puzzle0; // Updates which stage can be re-entered locally.
        localPuzzleReentryOwnerActorNumber = -1; // Updates the actor number tied to local re-entry.
    } // Ends this block.

    private void ClearLocalPuzzleAccess() // Handles this part of the puzzle state flow.
    { // Starts this block.
        ClearLocalActivePuzzleLock(); // Handles this part of the puzzle state flow.
        ClearLocalPuzzleReentryAccess(); // Handles this part of the puzzle state flow.
    } // Ends this block.

    private bool IsLocalPlayerActor(int actorNumber) // Handles this part of the puzzle state flow.
    { // Starts this block.
        return PhotonNetwork.LocalPlayer != null && // Returns the computed puzzle-state value to the caller.
               PhotonNetwork.LocalPlayer.ActorNumber == actorNumber; // Uses Photon networking state or operations.
    } // Ends this block.

    private IEnumerable<DraggableCard> FindCardsByBehavior(DraggableCard.DraggableCardBehavior behavior) // Handles this part of the puzzle state flow.
    { // Starts this block.
        foreach (var card in FindObjectsOfType<DraggableCard>()) // Loops through each matching puzzle object.
        { // Starts this block.
            if (card.Behavior == behavior) // Checks whether this puzzle-state condition requires action.
                yield return card; // Waits before continuing this coroutine.
        } // Ends this block.
    } // Ends this block.
} // Ends this block.
