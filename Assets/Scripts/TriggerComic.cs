using UnityEngine;

public class TriggerComic : MonoBehaviour
{
    [Header("Assign the UI or Objects to activate")]
    public GameObject[] objectsToActivate;   // array of objects

    private bool hasActivated = false;

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

            hasActivated = true; // prevent future activation
        }
    }
}
