

using System.Collections.Generic;
using UnityEngine;

public class PuzzleStateManager : MonoBehaviour
{
    public static PuzzleStateManager Instance;

    [Header("Puzzle State")]
    public bool isPuzzle2Active = false;

    [Header("Puzzle 1 Correct Symptoms")]
    public HashSet<string> correctSymptomsFromPuzzle1 = new();

    [SerializeField] private GameObject puzzle1Panel;
    [SerializeField] private GameObject puzzle2Panel;


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

        if (puzzle1Panel != null)
            puzzle1Panel.SetActive(false);

        // Enable puzzle 2 panel/UI
        if (puzzle2Panel != null)
            puzzle2Panel.SetActive(true);

       // Mark in state manager
       StartPuzzle2();
    }

    private void HandlePuzzle2Complete()
    {
        Debug.Log("Puzzle 2 complete! You win!");

        // Enable puzzle 2 panel/UI
        if (puzzle2Panel != null)
            puzzle2Panel.SetActive(false);
    }
}
