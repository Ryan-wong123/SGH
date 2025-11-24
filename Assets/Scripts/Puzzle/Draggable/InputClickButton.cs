using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InputClickButton : MonoBehaviour, IPointerClickHandler
{

    [Header("Trigger Button")]
    public PointerEventData.InputButton triggerButton = PointerEventData.InputButton.Left;

    [Header("Card Sprite (Optional)")]
    public Sprite cardSprite; // assign the card image here

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == triggerButton)
        {
            // Directly open popup, no bridging script required
            CardPopupManager.Instance.ShowCard(cardSprite);
        }
    }
}
