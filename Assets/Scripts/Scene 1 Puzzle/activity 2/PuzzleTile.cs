using UnityEngine;
using UnityEngine.UI;

public class PuzzleTile : MonoBehaviour
{
    private int currentRotation = 0; // 0, 90, 180, 270
    private const int rotationStep = 90;

    private RectTransform rectTransform;

    [Tooltip("Set the correct rotation angle here (0, 90, 180, 270)")]
    public int correctRotation = 0;

    private void Awake()
    {
        rectTransform = GetComponentInChildren<RectTransform>();
        // Random initial rotation
        currentRotation = 90 * Random.Range(0, 4);
        rectTransform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }

    public void RotateTile()
    {
        currentRotation = (currentRotation + rotationStep) % 360;
        rectTransform.rotation = Quaternion.Euler(0, 0, currentRotation);
        PuzzleManager.Instance.CheckPuzzleSolved();
    }

    public bool IsCorrect()
    {
        Debug.Log($"{gameObject.name} is {currentRotation == correctRotation}");
        return currentRotation == correctRotation;
    }
}
