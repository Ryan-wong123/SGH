

using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

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
        photonView.RPC(nameof(RPC_DestroyPuzzle1Object), RpcTarget.AllBuffered);

    }

    [PunRPC]
    private void RPC_DestroyPuzzle1Object()
    {
        if (puzzle1Object != null)
        {
            puzzle1Object.SetActive(false);
            Debug.Log("Puzzle 1 object destroyed across all players.");
        }
    }

}
