using UnityEngine;
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    public PuzzleTile[] tiles; // assign these in the Inspector

    private void Awake()
    {
        Instance = this;
    }

    public bool CheckPuzzleSolved()
    {
        foreach (var tile in tiles)
        {
            if (!tile.IsCorrect())
            {
                return false;
            }
        }
        return true;
    }
}
