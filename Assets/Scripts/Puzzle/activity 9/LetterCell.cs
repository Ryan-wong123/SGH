using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class LetterCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    public TMP_Text letterText;
    public Image backgroundImage;
    public Vector2Int gridPosition;  // (x,y) in grid array
    public bool isLocked = false;

    private WordSearchManager game;

    private void Start()
    {
        game = FindAnyObjectByType<WordSearchManager>();
    }

    public void SetLetter(char c) => letterText.text = c.ToString();

    public char GetLetter() => letterText.text.Length > 0 ? letterText.text[0] : ' ';

    public void Highlight(bool status, Color? customColor = null)
    {
        if (isLocked) return;
        backgroundImage.color = status ? (customColor ?? Color.yellow) : Color.white;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left: start a drag OR begin manual selection (first click)
            game.BeginPointer(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right: remove any stored selection that includes this cell
            game.TryRemoveSelectionContaining(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
            game.DragPreviewTo(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            game.EndPointer(this);
    }

    public void DisableCellUI() => backgroundImage.enabled = false;

    public void SetTextColor(Color color) => letterText.color = color;

    public void SetLetterString(string s) => letterText.text = s;
}
