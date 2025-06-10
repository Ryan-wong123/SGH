using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class LetterCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    public TMP_Text letterText;
    public Image backgroundImage;
    public Vector2Int gridPosition;
    public bool isLocked = false;

    private WordSearchManager game;

    private void Start()
    {
        game = FindObjectOfType<WordSearchManager>();
    }

    public void SetLetter(char c)
    {
        letterText.text = c.ToString();
    }

    public char GetLetter()
    {
        return letterText.text[0];
    }

    public void Highlight(bool status)
    {
        if (isLocked) return;
        backgroundImage.color = status ? Color.yellow : Color.white;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            game.StartSelection(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0)) // ✅ Important: only act while dragging
            game.UpdateSelection(this);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            game.EndSelection();
    }
}
