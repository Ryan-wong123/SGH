

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleStateManager : MonoBehaviourPun
{
    public static PuzzleStateManager Instance;

    [Header("Puzzle State")]
    public bool isPuzzle2Active = false;

    [Header("Puzzle 1 Correct Symptoms")]
    public HashSet<string> correctSymptomsFromPuzzle1 = new();

    [SerializeField] private GameObject mainminiPuzzle1Panel1;
    [SerializeField] private GameObject miniPuzzle1Panel1;
    [SerializeField] private GameObject miniPuzzle1Panel2;
    [SerializeField] private GameObject miniPuzzle1Header1;
    [SerializeField] private GameObject miniPuzzle1Header2;

    [SerializeField] private GameObject puzzle1Object;

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

    public void MarkCorrect(string symptom)
    {
        correctSymptomsFromPuzzle1.Add(symptom);
    }

    public bool IsSymptomCorrect(string symptom)
    {
        return correctSymptomsFromPuzzle1.Contains(symptom);
    }

    public void StartPuzzle2()
    {
        isPuzzle2Active = true;
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


       // Mark in state manager
       StartPuzzle2();
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
            HandlePuzzle1Complete();
        }
    }


}
