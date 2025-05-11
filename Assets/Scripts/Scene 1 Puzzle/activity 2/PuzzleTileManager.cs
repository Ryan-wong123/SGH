using UnityEngine;
using UnityEngine.UI;

public class PuzzleTileManager : MonoBehaviour
{
    public static PuzzleTileManager Instance;

    public PuzzleTile[] tiles; // assign these in the Inspector

    private void Awake()
    {
        Instance = this;
    }

    public bool CheckPuzzleSolved()
    {
        bool allCorrect = true;

        foreach (var tile in tiles)
        {
            if (!tile.IsCorrect())
            {
                Debug.LogWarning($"{tile.gameObject.name} is NOT correctly rotated. Current rotation is incorrect.");
                allCorrect = false;
            }
            else
            {
                Debug.Log($"{tile.gameObject.name} is correctly rotated.");
            }
        }

        return allCorrect;
    }
}
