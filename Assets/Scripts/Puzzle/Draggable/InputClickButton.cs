using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InputClickButton : MonoBehaviour, IPointerClickHandler
{

    [Header("Trigger Button")]
    public PointerEventData.InputButton triggerButton = PointerEventData.InputButton.Left;

    [Header("What to call when triggered")]
    public UnityEvent onTriggered; // no params, fully generic

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == triggerButton)
        {
            onTriggered.Invoke();
        }
    }
}
