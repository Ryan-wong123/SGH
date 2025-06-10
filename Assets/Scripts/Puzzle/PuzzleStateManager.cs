

using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleStateManager : MonoBehaviourPun
{
    [SerializeField] private float delayTransition;

    public static PuzzleStateManager Instance;

    [Header("Puzzle State")]
    public PuzzleStage CurrentStage = PuzzleStage.Puzzle1;

    [Header("Puzzle 1 Correct Symptoms")]
    public HashSet<string> correctSymptomsFromPuzzle1 = new();

    [SerializeField] private GameObject mainminiPuzzle1Panel1;
    [SerializeField] private GameObject miniPuzzle1Panel1;
    [SerializeField] private GameObject miniPuzzle1Panel2;
    [SerializeField] private GameObject miniPuzzle1Header1;
    [SerializeField] private GameObject miniPuzzle1Header2;
    [SerializeField] private SymptomsCardManager symptomsCardManager;

    [SerializeField] private GameObject Puzzle3Panel;
    [SerializeField] private PictureGridManager pictureGridManager;

    [SerializeField] private Activity2PuzzleManager activity2PuzzleManager;
    [SerializeField] private GameObject Puzzle4Panel;

    [SerializeField] private CrosswordManager crosswordManager;
    [SerializeField] private GameObject Puzzle5Panel;

    [SerializeField] private GameObject puzzle1Object;
    [SerializeField] private GameObject puzzle2Object;
    [SerializeField] private GameObject puzzle3Object;
    [SerializeField] private GameObject puzzle4Object;
    [SerializeField] private GameObject puzzle5Object;

    [SerializeField] private DiaryPuzzleManager diaryPuzzleManager;
    [SerializeField] private GameObject Puzzle6Panel;

    [SerializeField] private PrescriptionManager prescriptionManager;
    [SerializeField] private GameObject Puzzle7Panel;

    [SerializeField] private InsulinPuzzleManager insulinPuzzleManager;
    [SerializeField] private GameObject Puzzle8Panel;

    [SerializeField] private DeathLapsesManager deathLapsesManager;
    [SerializeField] private GameObject Puzzle11Panel;
    
    public enum PuzzleStage
    {
        Puzzle1,
        Puzzle2,
        Puzzle3,
        Puzzle4,
        Puzzle5,
        Puzzle6,
        Puzzle7,
        Puzzle8,
        Puzzle9,
        Puzzle10,
        Puzzle11
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); // optional if changing scenes
    }
    public void OnTimerExpired()
    {
        Debug.Log("Timer expired — reloading current puzzle stage.");

        switch (CurrentStage)
        {
            case PuzzleStage.Puzzle1:
                ReloadPuzzle1and2();
                break;
            case PuzzleStage.Puzzle2:
                ReloadPuzzle1and2();
                break;
            case PuzzleStage.Puzzle3:
                ReloadPuzzle3();
                break;
            case PuzzleStage.Puzzle4:
                ReloadPuzzle3();
                ReloadPuzzle4();
                break;
            case PuzzleStage.Puzzle5:
                ReloadPuzzle5();
                break;
            case PuzzleStage.Puzzle6:
                ReloadPuzzle5();
                ReloadPuzzle6();
                break;
            case PuzzleStage.Puzzle7:
                ReloadPuzzle7();
                break;
            case PuzzleStage.Puzzle8:
                ReloadPuzzle7();
                break;
            case PuzzleStage.Puzzle9:
                Debug.Log("Restarting Puzzle 9");
                break;
        }
    }

    public void ReloadPuzzle1and2()
    {
        Debug.Log("Reloading Puzzle 1 and 2");

        // 1. Clear all drop slots
        DropSlot.ResetAllSlots(); // removes child cards from slots

        // 2. Reset draggable card visuals
        foreach (var card in FindObjectsOfType<Activity1DraggableCard>())
        {
            Image img = card.GetComponent<Image>();
            if (img != null) img.color = Color.white;

            card.ResetCard(); // Snap back to original position
        }
        foreach (Activity1DraggableCard card in FindObjectsOfType<Activity1DraggableCard>())
        {
            card.ShowFront(); // ✅ Show symptom sprite again
        }

        // 4. Set UI and timer
        miniPuzzle1Panel1?.SetActive(true);
        miniPuzzle1Header1?.SetActive(true);
        miniPuzzle1Panel2?.SetActive(false);
        miniPuzzle1Header2?.SetActive(false);
        mainminiPuzzle1Panel1?.SetActive(false);
    
        CurrentStage = PuzzleStage.Puzzle1;

        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }
    public void ReloadPuzzle3()
    {
        Debug.Log("Reloading Puzzle 3");

        // 1. Reset the Picture Grid puzzle data
        if (pictureGridManager != null)
        {
            pictureGridManager.ResetPuzzle(); // ← You must implement this in PictureGridManager
        }

        // 2. Show Puzzle 3 Panel, hide Puzzle 4 if necessary
        if (Puzzle3Panel != null) Puzzle3Panel.SetActive(false);

        // 3. Update stage
        CurrentStage = PuzzleStage.Puzzle3;

        // 4. Restart timer
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }
    public void ReloadPuzzle4()
    {
        Debug.Log("Reloading Puzzle 4");

        // 1. Reset the tile rotation puzzle
        if (PuzzleTileManager.Instance != null)
        {
            PuzzleTileManager.Instance.ResetPuzzle(); // ← You must implement this in PuzzleTileManager
        }

        // 2. Show Puzzle 4 Panel, hide Puzzle 5 if necessary
        if (Puzzle4Panel != null) Puzzle4Panel.SetActive(false);

        // 3. Update stage
        CurrentStage = PuzzleStage.Puzzle3;

        // 4. Restart timer
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }
    public void ReloadPuzzle5()
    {
        Debug.Log("Reloading Puzzle 5");

        // 1. Reset crossword puzzle state
        if (crosswordManager != null)
        {
            crosswordManager.ResetPuzzle(); // You must implement this method in CrosswordManager
        }

        // 2. Show Puzzle 5 Panel, hide Puzzle 6 Panel if needed
        if (Puzzle5Panel != null) Puzzle5Panel.SetActive(false);

        // 3. Update current stage
        CurrentStage = PuzzleStage.Puzzle5;

        // 4. Restart timer
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }
    public void ReloadPuzzle6()
    {
        Debug.Log("Reloading Puzzle 6");

        // 1. Reset diary puzzle
        if (diaryPuzzleManager != null)
        {
            diaryPuzzleManager.ResetPuzzle(); // You must implement this method in DiaryPuzzleManager
        }

        // 2. Show Puzzle 6 panel
        if (Puzzle6Panel != null) Puzzle6Panel.SetActive(false);

        // 3. Update current stage
        CurrentStage = PuzzleStage.Puzzle5;

        // 4. Restart timer
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }

    public void ReloadPuzzle7()
    {
        Debug.Log("Reloading Puzzle 7");

        // 1. Reset prescription inut field
        if (prescriptionManager != null)
        {
            prescriptionManager.ResetPuzzle(); // You must implement this method in prescriptionManager
        }

        if (Puzzle7Panel != null) Puzzle7Panel.SetActive(false);

        // 3. Update current stage
        CurrentStage = PuzzleStage.Puzzle7;

        // 4. Restart timer
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
                timerManager.StartTimer();
        }
    }


    public void MarkCorrect(string symptom)
    {
        correctSymptomsFromPuzzle1.Add(symptom);
    }

    public bool IsSymptomCorrect(string symptom)
    {
        return correctSymptomsFromPuzzle1.Contains(symptom);
    }

    private void HandlePuzzle1Complete()
    {
        Debug.Log("Puzzle 1 complete! Now starting Puzzle 2...");
        // Reset card visuals before showing Puzzle 2
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                Image img = card.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        foreach (Activity1DraggableCard card in FindObjectsOfType<Activity1DraggableCard>())
        {
            card.ShowBack(); // Show bear sprite
        }

        if (miniPuzzle1Panel1 != null)
            miniPuzzle1Panel1.SetActive(false);

        // Enable puzzle 2 panel/UI
        if (miniPuzzle1Panel2 != null)
            miniPuzzle1Panel2.SetActive(true);

        miniPuzzle1Header1.SetActive(false);
        miniPuzzle1Header2.SetActive(true);
       
        CurrentStage = PuzzleStage.Puzzle2;
    }

    private void HandlePuzzle2Complete()
    {
        Debug.Log("Puzzle 2 complete! You win!");

        // Enable puzzle 2 panel/UI
        if (miniPuzzle1Panel2 != null)
            miniPuzzle1Panel2.SetActive(false);

        miniPuzzle1Header2.SetActive(false);

        // Enable puzzle 2 panel/UI
        if (mainminiPuzzle1Panel1 != null)
            mainminiPuzzle1Panel1.SetActive(false);

        // Ask the Master Client to destroy it
        if (puzzle1Object != null)
        {
            PhotonView puzzlePV = puzzle1Object.GetComponent<PhotonView>();
            if (puzzlePV != null)
            {
                photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
            }
            else
            {
                Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }
        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, "Time Crystal");

        CurrentStage = PuzzleStage.Puzzle3;

        ResetTimer();
       
    }

    public void HandlePuzzle3Complete()
    {
        Debug.Log("Puzzle 3 completed correctly!");
        Puzzle3Panel.SetActive(false);
        Puzzle4Panel.SetActive(true);
        CurrentStage = PuzzleStage.Puzzle4;
    }

    public void HandlePuzzle4Complete()
    {
        Debug.Log("Puzzle 4 completed correctly!");
        Puzzle4Panel.SetActive(false);

        // Ask the Master Client to destroy it
        if (puzzle2Object != null)
        {
            PhotonView puzzlePV = puzzle2Object.GetComponent<PhotonView>();
            if (puzzlePV != null)
            {
                photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
            }
            else
            {
                Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }
        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, "Time Crystal2");

        CurrentStage = PuzzleStage.Puzzle5;

        ResetTimer();

    }

    public void HandlePuzzle5Complete()
    {
        Debug.Log("Puzzle 5 completed correctly!");
        Puzzle5Panel.SetActive(false);
        Puzzle6Panel.SetActive(true);
        CurrentStage = PuzzleStage.Puzzle6;

        ResetTimer();

    }
    public void HandlePuzzle6Complete()
    {
        Debug.Log("Puzzle 6 completed correctly!");
        Puzzle6Panel.SetActive(false);

        // Ask the Master Client to destroy it
        if (puzzle3Object != null)
        {
            PhotonView puzzlePV = puzzle3Object.GetComponent<PhotonView>();
            if (puzzlePV != null)
            {
                photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
            }
            else
            {
                Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }
        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, "Time Crystal3");

        CurrentStage = PuzzleStage.Puzzle7;

        ResetTimer();

    }

    public void HandlePuzzle7Complete()
    {
        Debug.Log("Puzzle 7 completed correctly!");
        Puzzle7Panel.SetActive(false);
        Puzzle8Panel.SetActive(true);

        CurrentStage = PuzzleStage.Puzzle8;

        ResetTimer();

    }

    public void HandlePuzzle8Complete()
    {
        Debug.Log("Puzzle 8 completed correctly!");
        Puzzle8Panel.SetActive(false);

        CurrentStage = PuzzleStage.Puzzle9;

        // Ask the Master Client to destroy it
        if (puzzle4Object != null)
        {
            PhotonView puzzlePV = puzzle4Object.GetComponent<PhotonView>();
            if (puzzlePV != null)
            {
                photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
            }
            else
            {
                Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }
        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, "Time Crystal4");

        ResetTimer();
    }

    public void HandlePuzzle11Complete()
    {
        Debug.Log("Puzzle 11 completed correctly!");
        Puzzle11Panel.SetActive(false);


        // Ask the Master Client to destroy it
        if (puzzle5Object != null)
        {
            PhotonView puzzlePV = puzzle5Object.GetComponent<PhotonView>();
            if (puzzlePV != null)
            {
                photonView.RPC(nameof(RPC_DestroyPuzzleObjectByID), RpcTarget.AllBuffered, puzzlePV.ViewID);
            }
            else
            {
                Debug.LogError("Puzzle object does not have a PhotonView component!");
            }
        }
        photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, "Time Crystal5");

        ResetTimer();
    }


    public void ResetTimer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameTimerManager timerManager = FindObjectOfType<GameTimerManager>();
            if (timerManager != null)
            {
                Debug.Log("Reset timer");
                timerManager.StartTimer(); // This already syncs the timer

                PhotonView timerView = timerManager.GetComponent<PhotonView>();
                if (timerView != null)
                {
                    timerView.RPC("HideTimeWarning", RpcTarget.All);
                }
                else
                {
                    Debug.LogError("GameTimerManager has no PhotonView attached!");
                }
            }
        }
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

    [PunRPC]
    void RPC_AddSharedInventoryItem(string itemName)
    {
        StartCoroutine(DelayedAddItem(itemName));
    }

    IEnumerator DelayedAddItem(string itemName)
    {
        while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
            yield return null;

        InventorySystem.Instance.AddItem(itemName);
        InventoryUIManager.Instance.UpdateInventoryUI();
    }

    public void SubmitPuzzle1()
    {
        if (symptomsCardManager.AreAllPuzzle1CardsCorrect())
        {
            DelayThen(HandlePuzzle1Complete, delayTransition);
        }
        else
        {
            Debug.Log("Puzzle 1 incomplete or incorrect.");
        }
    }

    public void SubmitPuzzle2()
    {
        if (symptomsCardManager.AreAllPuzzle2CardsCorrect())
        {
            DelayThen(HandlePuzzle2Complete, delayTransition);
        }
        else
        {
            Debug.Log("Puzzle 2 incomplete or incorrect.");
        }
    }


    public void SubmitPuzzle3()
    {
        if (pictureGridManager != null)
        {
           if(pictureGridManager.CheckAnswers())
            {
                DelayThen(HandlePuzzle3Complete, delayTransition);
            }
            Debug.Log("Puzzle 3 answer check completed.");
        }
        else
        {
            Debug.LogWarning("PictureGridManager reference is missing!");
        }
    }

    public void SubmitPuzzle4()
    {
        
        if (activity2PuzzleManager.AreAllPuzzle4CardsCorrect())
        {
            DelayThen(HandlePuzzle4Complete, delayTransition);
        }
        else
        {
            Debug.Log("Not all tiles are correctly rotated.");
        }
    }

    public void SubmitPuzzle5()
    {
        if (crosswordManager != null)
        {
            if(crosswordManager.CheckPuzzle())
            {
                DelayThen(HandlePuzzle5Complete, delayTransition);
            }
        }
        else
        {
            Debug.LogWarning("CrosswordManager is not assigned!");
        }
    }

    public void SubmitPuzzle6()
    {
        if (diaryPuzzleManager != null)
        {
            if (diaryPuzzleManager.SubmitDiary())
            {
                DelayThen(HandlePuzzle6Complete, delayTransition);
            }
        }
        else
        {
            Debug.LogWarning("diaryPuzzleManager is not assigned!");
        }
    }

    public void SubmitPuzzle7()
    {
        if (prescriptionManager != null)
        {
            if (prescriptionManager.CheckPrescriptionAmount())
            {
                DelayThen(HandlePuzzle7Complete, delayTransition);
            }
        }
        else
        {
            Debug.LogWarning("prescriptionManager is not assigned!");
        }
    }

    public void SubmitPuzzle8()
    {
        if (insulinPuzzleManager != null)
        {
            if (insulinPuzzleManager.AreAllPuzzle8CardsCorrect())
            {
                DelayThen(HandlePuzzle8Complete, delayTransition);
            }
        }
        else
        {
            Debug.LogWarning("insulinPuzzleManager is not assigned!");
        }
    }

    public void SubmitPuzzle11()
    {
        if (deathLapsesManager != null)
        {
          
            if (deathLapsesManager.AreAllPuzzle11CardsCorrect())
            {
                DelayThen(HandlePuzzle11Complete, delayTransition);
            }
        }
        else
        {
            Debug.LogWarning("deathLapsesManager is not assigned!");
        }
    }

    public void DelayThen(Action callback, float delay = 10f)
    {
        StartCoroutine(DelayedCallback(callback, delay));
    }

    private IEnumerator DelayedCallback(Action callback, float delay)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    public void Submit()
    {
        switch (CurrentStage)
        {
            case PuzzleStage.Puzzle1:
                SubmitPuzzle1();
                break;

            case PuzzleStage.Puzzle2:
                SubmitPuzzle2();
                break;

            case PuzzleStage.Puzzle3:
                SubmitPuzzle3();
                break;
            case PuzzleStage.Puzzle4:
                SubmitPuzzle4();
                break;
            case PuzzleStage.Puzzle5:
                SubmitPuzzle5();
                break;
            case PuzzleStage.Puzzle6:
                SubmitPuzzle6();
                break;
            case PuzzleStage.Puzzle7:
                SubmitPuzzle7();
                break;
            case PuzzleStage.Puzzle8:
                SubmitPuzzle8();
                break;
            //case PuzzleStage.Puzzle9:
            //    SubmitPuzzle9();
            //    break;
            //case PuzzleStage.Puzzle10:
            //    SubmitPuzzle10();
            //    break;
            case PuzzleStage.Puzzle11:
                SubmitPuzzle11();
                break;
            default:
                Debug.LogWarning("Unknown puzzle stage!");
                break;
        }
    }
}
