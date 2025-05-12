

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleStateManager : MonoBehaviourPun
{
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

    [SerializeField] private GameObject Puzzle3Panel;
    [SerializeField] private PictureGridManager pictureGridManager;

    [SerializeField] private GameObject Puzzle4Panel;

    [SerializeField] private GameObject puzzle1Object;
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
        Puzzle9
    }

    private void OnEnable()
    {
        DropSlot.OnPuzzle1Complete += HandlePuzzle1Complete;
        DropSlot.OnPuzzle2Complete += HandlePuzzle2Complete;
    }

    private void OnDisable()
    {
        DropSlot.OnPuzzle1Complete -= HandlePuzzle1Complete;
        DropSlot.OnPuzzle2Complete -= HandlePuzzle2Complete;
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
                ReloadPuzzle1();
                break;
            case PuzzleStage.Puzzle2:
                Debug.Log("Restarting Puzzle 2");
                break;
            case PuzzleStage.Puzzle3:
                Debug.Log("Restarting Puzzle 3");
                break;
            case PuzzleStage.Puzzle4:
                Debug.Log("Restarting Puzzle 4");
                break;
            case PuzzleStage.Puzzle5:
                Debug.Log("Restarting Puzzle 5");
                break;
            case PuzzleStage.Puzzle6:
                Debug.Log("Restarting Puzzle 6");
                break;
            case PuzzleStage.Puzzle7:
                Debug.Log("Restarting Puzzle 7");
                break;
            case PuzzleStage.Puzzle8:
                Debug.Log("Restarting Puzzle 8");
                break;
            case PuzzleStage.Puzzle9:
                Debug.Log("Restarting Puzzle 9");
                break;
        }
    }
    public void ReloadPuzzle1()
    {
        Debug.Log("Reloading Puzzle 1");

        // 1. Clear all drop slots
        DropSlot.ResetAllSlots();

        // 2. Reset draggable cards
        foreach (var card in FindObjectsOfType<DraggableCard>())
        {
            card.ResetCard(); // Reset position and parent
        }

        // 3. Reset puzzle tracking
        correctSymptomsFromPuzzle1.Clear();
        DropSlot.puzzle1CorrectCount = 0;

        // 4. Set UI and timer
        mainminiPuzzle1Panel1?.SetActive(false);
        CurrentStage = PuzzleStage.Puzzle1;

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

        Puzzle3Panel.SetActive(true);

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
        CurrentStage = PuzzleStage.Puzzle5;

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
        List<DraggableCard> allCards = new();

        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            allCards.AddRange(slot.GetCardsInSlot());
        }

        foreach (DraggableCard card in allCards)
        {
            // Ensure card is still actually inside a DropSlot
            if (card.transform.parent != null && card.transform.parent.GetComponent<DropSlot>() != null)
            {
                bool isCorrect = correctSymptomsFromPuzzle1.Contains(card.symptomName);
                var cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                    cardImage.color = isCorrect ? Color.green : Color.red;
            }
        }

        Debug.Log($"correct puzzle1 score: {DropSlot.puzzle1CorrectCount} ");
        if (DropSlot.puzzle1CorrectCount >= DropSlot.requiredCorrect)
        {
            foreach (DraggableCard card in allCards)
            {
                // Ensure card is still actually inside a DropSlot
                if (card.transform.parent != null && card.transform.parent.GetComponent<DropSlot>() != null)
                {
                    var cardImage = card.GetComponent<Image>();
                    if (cardImage != null)
                        cardImage.color = Color.white;
                }
            }
            HandlePuzzle1Complete();
        }
    }

    public void SubmitPuzzle2()
    {
        List<DraggableCard> allCards = new();

        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            allCards.AddRange(slot.GetCardsInSlot());
        }

        foreach (DraggableCard card in allCards)
        {
            // Ensure card is still in a slot
            if (card.transform.parent != null && card.transform.parent.GetComponent<DropSlot>() != null)
            {
                string bearColor = card.backSprite.name.ToLower();
                DropSlot slot = card.transform.parent.GetComponent<DropSlot>();
                bool isCorrect = slot != null && slot.acceptableBearColors.Contains(bearColor);

                var cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                    cardImage.color = isCorrect ? Color.green : Color.red;
            }
        }

        Debug.Log($"correct puzzle2 score: {DropSlot.puzzle2CorrectCount} ");
        if (DropSlot.puzzle2CorrectCount >= DropSlot.requiredCorrect)
        {
            HandlePuzzle2Complete();
        }
    }

    public void SubmitPuzzle3()
    {
        if (pictureGridManager != null)
        {
           if(pictureGridManager.CheckAnswers())
            {
                HandlePuzzle3Complete(); // Call this after checking
            }

            // Optionally check if the selected answers were correct before continuing
            // You can add logic here based on the result if needed
            Debug.Log("Puzzle 3 answer check completed.");
        }
        else
        {
            Debug.LogWarning("PictureGridManager reference is missing!");
        }
    }

    public void SubmitPuzzle4()
    {
        bool isCorrect = PuzzleTileManager.Instance.CheckPuzzleSolved();
        if (isCorrect)
        {
            HandlePuzzle4Complete();
        }
        else
        {
            Debug.Log("Not all tiles are correctly rotated.");
            // Optional: feedback UI
        }
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
            //case PuzzleStage.Puzzle5:
            //    SubmitPuzzle5();
            //    break;
            //case PuzzleStage.Puzzle6:
            //    SubmitPuzzle6();
            //    break;
            //case PuzzleStage.Puzzle7:
            //    SubmitPuzzle7();
            //    break;
            //case PuzzleStage.Puzzle8:
            //    SubmitPuzzle8();
            //    break;
            //case PuzzleStage.Puzzle9:
            //    SubmitPuzzle9();
            //    break;
            default:
                Debug.LogWarning("Unknown puzzle stage!");
                break;
        }
    }
}
