using TMPro;
using UnityEngine;

public class TriggerComic : MonoBehaviour
{
    [Header("Assign the UI or Objects to activate")]
    public GameObject[] objectsToActivate;   // array of objects

    private bool hasActivated = false;

    [SerializeField] private TextMeshProUGUI textUI = null; // Reference to the TextMeshProUGUI component
    [SerializeField] private string newText; // The new text to display

    private void OnTriggerEnter(Collider other)
    {
        if (!hasActivated && other.CompareTag("Player"))
        {
            // Activate all objects in the array
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null)
                    obj.SetActive(true);
            }

            if(textUI != null)
            {
                textUI.text = newText; // Update the text
            }

            hasActivated = true; // prevent future activation
        }
    }
}
