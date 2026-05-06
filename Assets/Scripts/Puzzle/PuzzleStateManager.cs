using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;                      // keep Photon namespace
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable; // alias Photon Hashtable

public class PuzzleStateManager : MonoBehaviourPunCallbacks
{
    [Serializable]
    private class PuzzleClueReward
    {
        public PuzzleStage stage;
        public string clueId;
        [Tooltip("Optional popup override. Leave empty to use 'Clue picked up: <clue name>'.")]
        public string popupMessage;
    }

    [Header("puzzle trigger object")]
    [SerializeField] private GameObject puzzle1Object;
    [SerializeField] private GameObject puzzle2Object;
    [SerializeField] private GameObject puzzle3Object;
    [SerializeField] private GameObject puzzle4Object;
    [SerializeField] private GameObject puzzle5Object;

    [SerializeField] private float delayTransition;

    public static PuzzleStateManager Instance;

    [Header("Puzzle State")]
    public PuzzleStage CurrentStage = PuzzleStage.Puzzle0;

    [Header("Puzzle 1 Correct Symptoms")]
    public HashSet<string> correctSymptomsFromPuzzle1 = new();

    [Header("puzzle 1 ")]
    [SerializeField] private GameObject Puzzle1Panel; 
    [SerializeField] private SymptomsCardManager symptomsCardManager;

    [Header("puzzle 2")]
    [SerializeField] private GameObject Puzzle2Panel;
    [SerializeField] private BearCardManager bearCardManager;

    [Header("puzzle 3")]
    [SerializeField] private GameObject Puzzle3Panel;
    [SerializeField] private PictureGridManager pictureGridManager;

    [Header("puzzle 4")]
    [SerializeField] private Activity2PuzzleManager activity2PuzzleManager;
    [SerializeField] private GameObject Puzzle4Panel;
    [SerializeField] private GameObject Puzzle4Panel1;
    [SerializeField] private GameObject Puzzle4Panel2;

    [Header("puzzle 5")]
    [SerializeField] private WordSearchManager wordSearchManager;
    [SerializeField] private GameObject Puzzle5Panel;

    [Header("puzzle 6")]
    [SerializeField] private DiaryPuzzleManager diaryPuzzleManager;
    [SerializeField] private GameObject Puzzle6Panel;

    [Header("puzzle 7")]
    [SerializeField] private RiskTable risktableManager;
  
    [SerializeField] private GameObject Puzzle7Panel;

    [Header("puzzle 8")]
    [SerializeField] private PrescriptionManager prescriptionManager;
    [SerializeField] private GameObject Puzzle8Panel;

    [Header("puzzle 9")]
    [SerializeField] private CrosswordManager crosswordManager;
    [SerializeField] private GameObject Puzzle9Panel;
    [SerializeField] private GameObject Puzzle9ImageHint;
    [SerializeField] private GameObject Puzzle9AcrossHint;

    [Header("puzzle 10")]
    [SerializeField] private InsulinPuzzleManager insulinPuzzleManager;
    [SerializeField] private GameObject Puzzle10Panel;

    [Header("puzzle 11")]
    [SerializeField] private DeathLapsesManager deathLapsesManager;
    [SerializeField] private GameObject Puzzle11Panel;
    [SerializeField] private GameObject movementTargetPuzzle11;

    [Header("Puzzle Clue Rewards")]
    [SerializeField] private List<PuzzleClueReward> puzzleClueRewards = new();

    private Dictionary<PuzzleStage, IPuzzle> puzzleHandlers;



    // Tracks which panel belongs to which stage
    private readonly Dictionary<PuzzleStage, GameObject> stagePanels = new();
    private GameObject currentPuzzlePanel;
    private int currentSubmitOwnerActorNumber = -1;

    // ===== Room / Player property keys =====
    private const string RP_PUZZLE_LOCKED = "PuzzleLocked";   // bool
    private const string RP_LOCK_OWNER = "LockOwner";       // int ActorNumber
    private const string RP_ROUND = "Round";           // int; increments to signal a reset
    private const string PP_PLAYED_ROUND = "PlayedRound";     // bool on each player

    public enum PuzzleStage
    {
        Puzzle0 = 0,
        Puzzle1 = 1,
        Puzzle2 = 2,
        Puzzle3 = 3,
        Puzzle4 = 4,
        Puzzle5 = 5,
        Puzzle6 = 6,
        Puzzle7 = 7,
        Puzzle8 = 8,
        Puzzle9 = 9,
        Puzzle10 = 10,
        Puzzle11 = 11
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitStagePanels();
        DontDestroyOnLoad(gameObject); // optional if changing scenes
    }

    private void Start()
    {
        puzzleHandlers = new Dictionary<PuzzleStage, IPuzzle>
        {
            { PuzzleStage.Puzzle1, symptomsCardManager },
            { PuzzleStage.Puzzle2, bearCardManager },
            { PuzzleStage.Puzzle3, pictureGridManager },
            { PuzzleStage.Puzzle4, activity2PuzzleManager },
            { PuzzleStage.Puzzle5, wordSearchManager  },
            { PuzzleStage.Puzzle6, diaryPuzzleManager },
            { PuzzleStage.Puzzle7, risktableManager  },
            { PuzzleStage.Puzzle8, prescriptionManager  },
            { PuzzleStage.Puzzle9, crosswordManager },
            { PuzzleStage.Puzzle10, insulinPuzzleManager },
            { PuzzleStage.Puzzle11, deathLapsesManager }
        };




        EnsureRoomProps(); // initialize fair-turn props in room
        RefreshSubmitButtonVisibility();
    }

    private void InitStagePanels()
    {
        stagePanels.Clear();
        stagePanels[PuzzleStage.Puzzle1] = Puzzle1Panel;
        stagePanels[PuzzleStage.Puzzle2] = Puzzle2Panel;
        stagePanels[PuzzleStage.Puzzle3] = Puzzle3Panel;
        stagePanels[PuzzleStage.Puzzle4] = Puzzle4Panel;
        stagePanels[PuzzleStage.Puzzle5] = Puzzle5Panel;
        stagePanels[PuzzleStage.Puzzle6] = Puzzle6Panel;
        stagePanels[PuzzleStage.Puzzle7] = Puzzle7Panel;
        stagePanels[PuzzleStage.Puzzle8] = Puzzle8Panel;
        stagePanels[PuzzleStage.Puzzle9] = Puzzle9Panel;
        stagePanels[PuzzleStage.Puzzle10] = Puzzle10Panel;
        stagePanels[PuzzleStage.Puzzle11] = Puzzle11Panel;
    }



    // ===== Timer-driven reloads =====
    public void OnTimerExpired()
    {
        Debug.Log("Timer expired — reloading current puzzle stage.");
        switch (CurrentStage)
        {
            case PuzzleStage.Puzzle0: break;
            case PuzzleStage.Puzzle1: ReloadPuzzle1(); break;
            case PuzzleStage.Puzzle2: ReloadPuzzle2(); break;
            case PuzzleStage.Puzzle3: ReloadPuzzle3(); break;
            case PuzzleStage.Puzzle4: ReloadPuzzle4(); break;
            case PuzzleStage.Puzzle5: ReloadPuzzle5(); break;
            case PuzzleStage.Puzzle6: ReloadPuzzle6(); break;
            case PuzzleStage.Puzzle7: ReloadPuzzle7(); break;
            case PuzzleStage.Puzzle8: ReloadPuzzle8(); break; 
            case PuzzleStage.Puzzle9: ReloadPuzzle9(); break; 
            case PuzzleStage.Puzzle10: ReloadPuzzle10(); break; 
            case PuzzleStage.Puzzle11: ReloadPuzzle11(); break; 
        }
    }

    public void ReloadPuzzle1()
    {
        Debug.Log("Reloading Puzzle 1 and 2");

        DropSlot.ResetAllSlots();

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity1))
        {
            card.ResetSyncedFeedbackColor();
            card.ResetCard();
            card.ShowFront();
        }

        CurrentStage = PuzzleStage.Puzzle1;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle2()
    {
        Debug.Log("Reloading Puzzle 1 and 2");

        DropSlot.ResetAllSlots();

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity1_2))
        {
            card.ResetSyncedFeedbackColor();
            card.ResetCard();
            card.ShowFront();
        }

        CurrentStage = PuzzleStage.Puzzle2;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }


    public void ReloadPuzzle3()
    {
        Debug.Log("Reloading Puzzle 3");
        pictureGridManager?.ResetPuzzle();
        CurrentStage = PuzzleStage.Puzzle3;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle4()
    {
        Debug.Log("Reloading Puzzle 4");
        DropSlot.ResetAllSlots();
        PuzzleTileManager.Instance?.ResetPuzzle();

        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity2))
        {
            card.ResetSyncedFeedbackColor();
            card.ResetCard();
        }
        activity2PuzzleManager.ResetActivity2Part2();
        Puzzle4Panel1?.SetActive(true);
        Puzzle4Panel2?.SetActive(false);
        CurrentStage = PuzzleStage.Puzzle4; 

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle5()
    {
        Debug.Log("Reloading Puzzle 5");
        wordSearchManager?.ResetPuzzle();
        CurrentStage = PuzzleStage.Puzzle5;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle6()
    {
        Debug.Log("Reloading Puzzle 6");
        diaryPuzzleManager?.ResetPuzzle();
        CurrentStage = PuzzleStage.Puzzle6; // fix

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle7()
    {
        Debug.Log("Reloading Puzzle 7");
        risktableManager?.ResetPuzzle();
        CurrentStage = PuzzleStage.Puzzle7;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle8()
    {
        Debug.Log("Reloading Puzzle 8");
        prescriptionManager?.ResetPuzzle();
        CurrentStage = PuzzleStage.Puzzle8;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }

    public void ReloadPuzzle9()
    {
        Debug.Log("Reloading Puzzle 9");
        crosswordManager?.ResetPuzzle();

        CurrentStage = PuzzleStage.Puzzle9;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }


    public void ReloadPuzzle10()
    {
        Debug.Log("Reloading Puzzle 10");
        DropSlot.ResetAllSlots();
        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity6))
        {
            card.ResetSyncedFeedbackColor();
            card.ResetCard();
        }
        insulinPuzzleManager?.ResetPuzzle();

        CurrentStage = PuzzleStage.Puzzle10;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }


    public void ReloadPuzzle11()
    {
        Debug.Log("Reloading Puzzle 11");
        DropSlot.ResetAllSlots();
        foreach (var card in FindCardsByBehavior(DraggableCard.DraggableCardBehavior.Activity9))
        {
            card.ResetSyncedFeedbackColor();
            card.ResetCard();
        }
        deathLapsesManager?.ResetPuzzle();

        CurrentStage = PuzzleStage.Puzzle11;

        if (PhotonNetwork.IsMasterClient)
            FindObjectOfType<GameTimerManager>()?.StartTimer();
    }


    // ===== Stage completion handlers =====
    public void MarkCorrect(string symptom) => correctSymptomsFromPuzzle1.Add(symptom);
    public bool IsSymptomCorrect(string symptom) => correctSymptomsFromPuzzle1.Contains(symptom);

    private void HandlePuzzle1Complete()
    {
        Debug.Log("Puzzle 1 complete! Now starting Puzzle 2...");

        Puzzle1Panel?.SetActive(false);
        Puzzle2Panel?.SetActive(true);

        CurrentStage = PuzzleStage.Puzzle2;
    }

    private void HandlePuzzle2Complete()
    {
        Debug.Log("Puzzle 2 complete! You win!");

        Puzzle2Panel?.SetActive(false);

        GetKeyUponCompletion(puzzle1Object, "Time Crystal1");

        CurrentStage = PuzzleStage.Puzzle3;
        ResetTimer();

        ObjectivePointer.Instance.StartPath("Canteen");

    }

    public void HandlePuzzle3Complete()
    {
        Debug.Log("Puzzle 3 completed correctly!");
        Puzzle3Panel?.SetActive(false);
        Puzzle4Panel?.SetActive(true);
        CurrentStage = PuzzleStage.Puzzle4;
    }

    public void HandlePuzzle4Complete()
    {
        Debug.Log("Puzzle 4 completed correctly!");
        Puzzle4Panel?.SetActive(false);


        GetKeyUponCompletion(puzzle2Object, "Time Crystal2");

        CurrentStage = PuzzleStage.Puzzle5;
        ResetTimer();

        ObjectivePointer.Instance.StartPath("Physician Room");
    }

    public void HandlePuzzle5Complete()
    {
        Debug.Log("Puzzle 5 completed correctly!");
        Puzzle5Panel?.SetActive(false);
        //Puzzle6Panel?.SetActive(true);
        CurrentStage = PuzzleStage.Puzzle6;
        ResetTimer();
    }
    [PunRPC]
    private void RPC_DiaryGoToStage(int newStage)
    {
        if (diaryPuzzleManager != null)
            diaryPuzzleManager.GoToStage(newStage);
    }

    [PunRPC]
    private void RPC_DiaryAdvanceToNextStage()
    {
        diaryPuzzleManager?.GoToNextStage();
    }

    [PunRPC]
    private void RPC_Activity2GoToPart2()
    {
        activity2PuzzleManager?.GoToPart2();
    }

    [PunRPC]
    private void RPC_InsulinGoToPart2()
    {
        insulinPuzzleManager?.GoToPart2();
    }

    [PunRPC]
    private void RPC_DeathLapsesGoToStage2()
    {
        deathLapsesManager?.GoToStage2();
    }

    public void BroadcastInsulinPart2Transition()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_InsulinGoToPart2), RpcTarget.All);
        else
            insulinPuzzleManager?.GoToPart2();
    }

    public void HandlePuzzle6Complete()
    {
        Debug.Log("Puzzle 6 completed correctly!");
        Puzzle6Panel?.SetActive(false);

        GetKeyUponCompletion(puzzle3Object, "Time Crystal3");

        CurrentStage = PuzzleStage.Puzzle7;
        ResetTimer();
        ObjectivePointer.Instance.StartPath("Pharmacy");
    }

    public void HandlePuzzle7Complete()
    {
        Debug.Log("Puzzle 7 completed correctly!");
        Puzzle7Panel?.SetActive(false);
        Puzzle8Panel?.SetActive(true);

        CurrentStage = PuzzleStage.Puzzle8;
        ResetTimer();
    }

    public void HandlePuzzle8Complete()
    {
        Debug.Log("Puzzle 8 completed correctly!");
        Puzzle8Panel?.SetActive(false);

        CurrentStage = PuzzleStage.Puzzle9;

        GetKeyUponCompletion(puzzle4Object, "Time Crystal4");
        ResetTimer();
        ObjectivePointer.Instance.StartPath("Nurse Education Room");
    }

    public void HandlePuzzle9Complete()
    {
        Debug.Log("Puzzle 9 completed correctly!");
        Puzzle9AcrossHint?.SetActive(false);
        Puzzle9ImageHint?.SetActive(true);

        CurrentStage = PuzzleStage.Puzzle10;
        ResetTimer();
    }

    public void HandlePuzzle10Complete()
    {
        Debug.Log("Puzzle 10 completed correctly!");
        Puzzle10Panel?.SetActive(false);

        CurrentStage = PuzzleStage.Puzzle11;
        ResetTimer();
       
        ObjectivePointer.Instance.StartPath("Morturary");
    }

    public void HandlePuzzle11Complete()
    {
        Debug.Log("Puzzle 11 completed correctly!");
        Puzzle11Panel?.SetActive(false);

        GetKeyUponCompletion(puzzle5Object, "Time Crystal5");
        ResetTimer();
        movementTargetPuzzle11?.GetComponent<MovementTargetForward>().Interact();
      

        //if (PuzzleScoreTracker.Instance != null)
        //    PuzzleScoreTracker.Instance.ShowEndScreen();
    }

    public void ResetTimer()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var timerManager = FindObjectOfType<GameTimerManager>();
        if (timerManager == null) return;

        Debug.Log("Reset timer");
        timerManager.StartTimer();

        PhotonView timerView = timerManager.GetComponent<PhotonView>();
        if (timerView != null) timerView.RPC("HideTimeWarning", RpcTarget.All);
        else Debug.LogError("GameTimerManager has no PhotonView attached!");
    }

    private void GetKeyUponCompletion(GameObject puzzleTriggerObject, string keyName)
    {
        if (puzzleTriggerObject != null)
        {
            var trigger = puzzleTriggerObject.GetComponent<PuzzleTrigger>();

            // ✅ Only destroy if trigger is NOT flagged as persistent
            if (trigger == null || !trigger.dontDestroyOnCompletion)
            {
                PhotonView puzzlePV = puzzleTriggerObject.GetComponent<PhotonView>();
                if (puzzlePV != null)
                    photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
                else
                    Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }

        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, keyName);
    }


    [PunRPC]
    private void RPC_DestroyPuzzleObjectByID(int photonViewID)
    {
        PhotonView targetView = PhotonView.Find(photonViewID);
        if (targetView != null)
        {
            targetView.gameObject.SetActive(false);
            Debug.Log($"Destroyed object {targetView.gameObject.name} across all players.");
        }
        else
        {
            Debug.LogWarning($"No PhotonView found with ID {photonViewID}.");
        }
    }

    [PunRPC] private void RPC_AddSharedInventoryItem(string itemName) => StartCoroutine(DelayedAddItem(itemName));
    [PunRPC] private void RPC_AddSharedClue(string clueId) => StartCoroutine(DelayedAddClue(clueId));
    [PunRPC] private void RPC_ShowSharedCluePopup(string popupText) => CluePopupUI.Instance?.Show(popupText);

    private IEnumerator DelayedAddItem(string itemName)
    {
        while (InventorySystem.Instance == null || InventoryUIManager.Instance == null) yield return null;
        InventorySystem.Instance.AddItem(itemName);
        InventoryUIManager.Instance.UpdateInventoryUI();
    }

    private IEnumerator DelayedAddClue(string clueId)
    {
        while (ClueSystem.Instance == null)
            yield return null;

        ClueSystem.Instance.AddClue(clueId);
    }

    public void DelayThen(Action callback, float delay = 10f) => StartCoroutine(DelayedCallback(callback, delay));
    private IEnumerator DelayedCallback(Action callback, float delay) { yield return new WaitForSeconds(delay); callback?.Invoke(); }

    private void HandlePuzzleCompletion(PuzzleStage stage)
    {
        switch (stage)
        {
            case PuzzleStage.Puzzle0: Debug.Log("Opening comic closed. Waiting for Puzzle 1."); break;
            case PuzzleStage.Puzzle1: HandlePuzzle1Complete(); break;
            case PuzzleStage.Puzzle2: HandlePuzzle2Complete(); break;
            case PuzzleStage.Puzzle3: HandlePuzzle3Complete(); break;
            case PuzzleStage.Puzzle4: HandlePuzzle4Complete(); break;
            case PuzzleStage.Puzzle5: HandlePuzzle5Complete(); break;
            case PuzzleStage.Puzzle6: HandlePuzzle6Complete(); break;
            case PuzzleStage.Puzzle7: HandlePuzzle7Complete(); break;
            case PuzzleStage.Puzzle8: HandlePuzzle8Complete(); break;
            case PuzzleStage.Puzzle9: HandlePuzzle9Complete(); break;
            case PuzzleStage.Puzzle10: HandlePuzzle10Complete(); break;
            case PuzzleStage.Puzzle11: HandlePuzzle11Complete(); break;
            default: Debug.LogWarning("Unhandled puzzle completion"); break;
        }

        AwardClueReward(stage);
    }

    private void AwardClueReward(PuzzleStage stage)
    {
        if (!TryGetClueReward(stage, out var reward))
            return;

        string clueId = reward.clueId?.Trim();
        if (string.IsNullOrWhiteSpace(clueId))
            return;

        string popupText = BuildCluePopupText(reward);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            CluePopupUI.Instance?.Show(popupText);
            photonView.RPC(nameof(RPC_AddSharedClue), RpcTarget.AllBuffered, clueId);
            photonView.RPC(nameof(RPC_ShowSharedCluePopup), RpcTarget.Others, popupText);
            return;
        }

        CluePopupUI.Instance?.Show(popupText);
        StartCoroutine(DelayedAddClue(clueId));
    }

    private bool TryGetClueReward(PuzzleStage stage, out PuzzleClueReward reward)
    {
        if (puzzleClueRewards != null)
        {
            foreach (var entry in puzzleClueRewards)
            {
                if (entry == null || entry.stage != stage)
                    continue;

                reward = entry;
                return true;
            }
        }

        reward = null;
        return false;
    }

    private string BuildCluePopupText(PuzzleClueReward reward)
    {
        if (reward != null && !string.IsNullOrWhiteSpace(reward.popupMessage))
            return reward.popupMessage.Trim();

        return $"Clue picked up: {FormatClueName(reward?.clueId)}";
    }

    private static string FormatClueName(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId))
            return "New clue";

        return clueId.Replace('_', ' ').Trim();
    }

    public void BroadcastStageTransition(PuzzleStage stage)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(RPC_SetStageForAll), RpcTarget.AllBuffered, (int)stage);

            return;
        }

        SetCurrentStage(stage);
    }

    // Use RPC to advance for everyone after delay (fair-turn compatible)
    public void Submit()
    {
        if (CurrentStage == PuzzleStage.Puzzle4 && activity2PuzzleManager != null)
        {
            var result = activity2PuzzleManager.SubmitActivity2Stage();

            if (result == Activity2PuzzleManager.Activity2SubmitResult.Incorrect)
            {
                if (PuzzleScoreTracker.Instance != null)
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage);

                Debug.Log("Activity 2 stage incorrect.");
                return;
            }

            if (result == Activity2PuzzleManager.Activity2SubmitResult.Advanced)
            {
                photonView.RPC(nameof(RPC_Activity2GoToPart2), RpcTarget.All);
                return;
            }

            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition));
            return;
        }

        // ✅ Special handling for Puzzle 6 multi-page diary
        if (CurrentStage == PuzzleStage.Puzzle6 && diaryPuzzleManager != null)
        {
            var result = diaryPuzzleManager.SubmitDiaryStage();

            if (result == DiaryPuzzleManager.DiarySubmitResult.Incorrect)
            {
                if (PuzzleScoreTracker.Instance != null)
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage);

                Debug.Log("Diary stage incorrect.");
                return;
            }

            if (result == DiaryPuzzleManager.DiarySubmitResult.Advanced)
            {
                photonView.RPC(nameof(RPC_DiaryAdvanceToNextStage), RpcTarget.All);
                return; // IMPORTANT: do not advance puzzle stage yet
            }

            // Completed final diary page -> proceed with normal stage-advance flow
            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition));
            return;
        }

        if (CurrentStage == PuzzleStage.Puzzle11 && deathLapsesManager != null)
        {
            var result = deathLapsesManager.SubmitDeathLapsesStage();

            if (result == DeathLapsesManager.DeathLapsesSubmitResult.Incorrect)
            {
                if (PuzzleScoreTracker.Instance != null)
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage);

                Debug.Log("Death Lapses stage incorrect.");
                return;
            }

            if (result == DeathLapsesManager.DeathLapsesSubmitResult.Advanced)
            {
                photonView.RPC(nameof(RPC_DeathLapsesGoToStage2), RpcTarget.All);
                return;
            }

            StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition));
            return;
        }

        // ✅ Default flow for all other puzzles
        if (puzzleHandlers.TryGetValue(CurrentStage, out IPuzzle puzzle))
        {
            if (puzzle.IsSolved())
            {
                StartCoroutine(DelayAndAdvance(CurrentStage, delayTransition));
            }
            else
            {
                if (PuzzleScoreTracker.Instance != null)
                    PuzzleScoreTracker.Instance.MarkWrong(CurrentStage);

                Debug.Log($"Puzzle {CurrentStage} incomplete or incorrect.");
            }
        }
        else
        {
            Debug.LogWarning($"No handler found for puzzle stage {CurrentStage}");
        }
    }



    private IEnumerator DelayAndAdvance(PuzzleStage stage, float delay)
    {
        yield return new WaitForSeconds(delay);
        photonView.RPC(nameof(RPC_AdvanceStageForAll), RpcTarget.AllBuffered, (int)stage);
    }

    [PunRPC]
    private void RPC_SetStageForAll(int stageInt)
    {
        SetCurrentStage((PuzzleStage)stageInt);
    }

    [PunRPC]
    private void RPC_AdvanceStageForAll(int stageInt)
    {
        var stage = (PuzzleStage)stageInt;
        HandlePuzzleCompletion(stage);
        RefreshSubmitButtonVisibility();

        // === CHANGE: Do NOT bump the round here ===
        // Master only unlocks and checks if everyone has played. Round will bump inside MaybeAdvanceRoundIfEveryonePlayed().
        if (PhotonNetwork.IsMasterClient)
        {
            var room = PhotonNetwork.CurrentRoom;

            // release lock for next player
            var unlock = new Hashtable
            {
                [RP_PUZZLE_LOCKED] = false,
                [RP_LOCK_OWNER] = -1
            };
            room.SetCustomProperties(unlock);

            // If everyone has taken a turn, bump the round (which resets PP_PLAYED_ROUND on all via OnRoomPropertiesUpdate).
            MaybeAdvanceRoundIfEveryonePlayed();
        }
    }

    private void SetCurrentStage(PuzzleStage stage)
    {
        if (CurrentStage == stage)
            return;

        CurrentStage = stage;
        RefreshSubmitButtonVisibility();
    }

    private GameObject GetPanelForStage(PuzzleStage stage) =>
        (stagePanels != null && stagePanels.TryGetValue(stage, out var panel)) ? panel : null;

    public void OpenCurrentStagePanel(int submitOwnerActorNumber = -1)
    {
        int resolvedSubmitOwnerActorNumber = ResolveSubmitOwnerActorNumber(submitOwnerActorNumber);
        currentSubmitOwnerActorNumber = resolvedSubmitOwnerActorNumber;

        var panel = GetPanelForStage(CurrentStage);
        if (panel != null)
        {
            panel.SetActive(true);
            currentPuzzlePanel = panel;
            RefreshSubmitButtonVisibility();
        }
    }
   
    public void CloseCurrentPuzzle()
    {
        var panel = GetPanelForStage(CurrentStage);
        if (panel != null && panel.activeSelf) { panel.SetActive(false); Debug.Log($"Closed {panel.name}"); }
        else Debug.Log("No current puzzle panel to close.");
    }

    // Close a specific puzzle panel from a UI button (Inspector selects stage as int)
    public void ClosePuzzle(int stageInt)
    {
        PuzzleStage stage = (PuzzleStage)stageInt;
        var panel = GetPanelForStage(stage);

        if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
            Debug.Log($"Closed {stage} ({panel.name})");
        }
        else
        {
            Debug.Log($"No active panel to close for stage: {stage}");
        }
    }

    // ==== Fair-turn system additions ====

    private void EnsureRoomProps()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var room = PhotonNetwork.CurrentRoom;
        var changed = new Hashtable();

        if (!room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED)) changed[RP_PUZZLE_LOCKED] = false;
        if (!room.CustomProperties.ContainsKey(RP_LOCK_OWNER)) changed[RP_LOCK_OWNER] = -1;
        if (!room.CustomProperties.ContainsKey(RP_ROUND)) changed[RP_ROUND] = 1;

        if (changed.Count > 0) room.SetCustomProperties(changed);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RP_ROUND))
        {
            var me = PhotonNetwork.LocalPlayer;
            var meProps = new Hashtable { [PP_PLAYED_ROUND] = false };
            me.SetCustomProperties(meProps);
        }

        if (propertiesThatChanged.ContainsKey(RP_LOCK_OWNER))
        {
            int lockOwner = propertiesThatChanged[RP_LOCK_OWNER] is int owner ? owner : -1;
            currentSubmitOwnerActorNumber = IsActorInRoom(lockOwner) ? lockOwner : -1;
        }

        RefreshSubmitButtonVisibility();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var room = PhotonNetwork.CurrentRoom;

        int owner = -1;
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER))
            owner = (int)room.CustomProperties[RP_LOCK_OWNER];

        bool disconnectedPlayerOwnsSubmit =
            owner == otherPlayer.ActorNumber ||
            currentSubmitOwnerActorNumber == otherPlayer.ActorNumber;

        if (disconnectedPlayerOwnsSubmit)
        {
            int replacementOwner = GetRandomActiveActorNumber(otherPlayer.ActorNumber);
            bool hasReplacementOwner = replacementOwner > 0;

            var ht = new Hashtable
            {
                [RP_PUZZLE_LOCKED] = hasReplacementOwner,
                [RP_LOCK_OWNER] = replacementOwner
            };
            room.SetCustomProperties(ht);
            photonView.RPC(nameof(RPC_SetSubmitOwnerForAll), RpcTarget.All, replacementOwner);
        }

        MaybeAdvanceRoundIfEveryonePlayed();
    }

    [PunRPC]
    private void RPC_SetSubmitOwnerForAll(int actorNumber)
    {
        currentSubmitOwnerActorNumber = IsActorInRoom(actorNumber) ? actorNumber : -1;
        RefreshSubmitButtonVisibility();
    }

    public bool CanLocalPlayerSubmitCurrentAttempt()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
            return true;

        currentSubmitOwnerActorNumber = ResolveSubmitOwnerActorNumber(currentSubmitOwnerActorNumber);

        if (currentSubmitOwnerActorNumber > 0)
            return PhotonNetwork.LocalPlayer.ActorNumber == currentSubmitOwnerActorNumber;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null)
            return true;

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) &&
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED];

        if (!locked)
            return true;

        int owner = room.CustomProperties.ContainsKey(RP_LOCK_OWNER)
            ? (int)room.CustomProperties[RP_LOCK_OWNER]
            : -1;

        return owner < 0 || owner == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    private int ResolveSubmitOwnerActorNumber(int requestedOwnerActorNumber)
    {
        if (!PhotonNetwork.IsConnected)
            return requestedOwnerActorNumber;

        if (IsActorInRoom(requestedOwnerActorNumber))
            return requestedOwnerActorNumber;

        var room = PhotonNetwork.CurrentRoom;
        if (room != null &&
            room.CustomProperties.ContainsKey(RP_LOCK_OWNER) &&
            room.CustomProperties[RP_LOCK_OWNER] is int lockOwner &&
            IsActorInRoom(lockOwner))
        {
            return lockOwner;
        }

        return -1;
    }

    private bool IsActorInRoom(int actorNumber)
    {
        if (actorNumber <= 0 || !PhotonNetwork.IsConnected)
            return false;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != null && !player.IsInactive && player.ActorNumber == actorNumber)
                return true;
        }

        return false;
    }

    private int GetRandomActiveActorNumber(int excludedActorNumber)
    {
        var activePlayers = new List<Player>();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != null && !player.IsInactive && player.ActorNumber != excludedActorNumber)
                activePlayers.Add(player);
        }

        if (activePlayers.Count == 0)
            return -1;

        int randomIndex = UnityEngine.Random.Range(0, activePlayers.Count);
        return activePlayers[randomIndex].ActorNumber;
    }

    private void RefreshSubmitButtonVisibility()
    {
        bool shouldShowSubmitButtons = CanLocalPlayerSubmitCurrentAttempt();

        foreach (var panel in stagePanels.Values)
        {
            if (panel == null) continue;

            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button == null || !IsManagedSubmitButton(button.gameObject))
                    continue;

                button.gameObject.SetActive(shouldShowSubmitButtons);
            }
        }

        pictureGridManager?.RefreshSubmitButtonVisibility();
    }

    private bool IsManagedSubmitButton(GameObject buttonObject)
    {
        if (buttonObject == null)
            return false;

        if (pictureGridManager != null && pictureGridManager.SubmitButton == buttonObject)
            return false;

        return buttonObject.name.IndexOf("submit", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Gate attempt; called by triggers
    // Gate attempt; called by triggers
    public bool TryBeginAttempt(out string reason)
    {
        reason = null;

        var me = PhotonNetwork.LocalPlayer;
        var room = PhotonNetwork.CurrentRoom;

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) &&
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED];

        int owner = -1;
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER))
            owner = (int)room.CustomProperties[RP_LOCK_OWNER];

        bool iPlayed = me.CustomProperties != null &&
                       me.CustomProperties.ContainsKey(PP_PLAYED_ROUND) &&
                       (bool)me.CustomProperties[PP_PLAYED_ROUND];

        // ✅ 1) If the puzzle is locked by ME, always allow me to reopen/close/reopen.
        if (locked && owner == me.ActorNumber)
        {
            return true; // don't change any props; I still hold the lock
        }

        // 2) If someone else holds it, block.
        if (locked && owner != me.ActorNumber)
        {
            reason = "Another player is currently holding this puzzle.";
            return false;
        }

        // 3) If it's not locked and I've already taken my turn this round, block starting a (new) attempt.
        if (iPlayed)
        {
            reason = "You’ve already taken your turn this round.";
            MaybeAdvanceRoundIfEveryonePlayed();
            return false;
        }

        // 4) Otherwise acquire the lock and mark my turn as spent.
        if (PhotonNetwork.IsMasterClient)
        {
            var ht = new Hashtable
            {
                [RP_PUZZLE_LOCKED] = true,
                [RP_LOCK_OWNER] = me.ActorNumber
            };
            room.SetCustomProperties(ht);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestLockFromMaster), RpcTarget.MasterClient, me.ActorNumber);
        }

        me.SetCustomProperties(new Hashtable { [PP_PLAYED_ROUND] = true });
        return true;
    }


    [PunRPC]
    private void RPC_RequestLockFromMaster(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var room = PhotonNetwork.CurrentRoom;

        bool locked = room.CustomProperties.ContainsKey(RP_PUZZLE_LOCKED) &&
                      (bool)room.CustomProperties[RP_PUZZLE_LOCKED];

        int owner = -1;
        if (room.CustomProperties.ContainsKey(RP_LOCK_OWNER))
            owner = (int)room.CustomProperties[RP_LOCK_OWNER];

        if (!locked || owner == actorNumber)
        {
            var ht = new Hashtable { [RP_PUZZLE_LOCKED] = true, [RP_LOCK_OWNER] = actorNumber };
            room.SetCustomProperties(ht);
        }
    }

    private void MaybeAdvanceRoundIfEveryonePlayed()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            bool played = p.CustomProperties != null &&
                          p.CustomProperties.ContainsKey(PP_PLAYED_ROUND) &&
                          (bool)p.CustomProperties[PP_PLAYED_ROUND];
            if (!played) return; // someone still hasn't had a turn
        }

        var room = PhotonNetwork.CurrentRoom;
        int round = room.CustomProperties.ContainsKey(RP_ROUND)
                    ? (int)room.CustomProperties[RP_ROUND] : 1;

        var ht = new Hashtable
        {
            [RP_ROUND] = round + 1,
            [RP_PUZZLE_LOCKED] = false,
            [RP_LOCK_OWNER] = -1
        };
        room.SetCustomProperties(ht);
    }

    private IEnumerable<DraggableCard> FindCardsByBehavior(DraggableCard.DraggableCardBehavior behavior)
    {
        foreach (var card in FindObjectsOfType<DraggableCard>())
        {
            if (card.Behavior == behavior)
                yield return card;
        }
    }
}
