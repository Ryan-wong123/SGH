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

    public void ResetPuzzle()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            float currentRotation = 90 * Random.Range(0, 4);
            tiles[i].transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
        }


        Debug.Log("Tile Rotation Puzzle reset.");
    }

}
