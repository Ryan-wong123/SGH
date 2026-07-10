using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Random, Time, and Mathf.
using UnityEngine.UI; // Imports Unity UI components such as Image.
using UnityEngine.SceneManagement; // Imports scene loading events and scene data types.
using Photon.Pun; // Imports Photon PUN networking and level loading progress.
using System.Collections; // Imports IEnumerator for coroutine support.
using TMPro; // Imports TextMeshPro UI text components.

public class LoadingScreenManager : MonoBehaviour // Defines a Unity component that controls the loading screen UI.
{ // Starts the LoadingScreenManager class body.
    [Header("UI References")] // Adds a labeled section for UI references in the Unity inspector.
    [SerializeField] private GameObject loadingPanel; // References the panel that contains the loading screen UI.
    [SerializeField] private Image progressBarFill; // References the image whose fill amount displays loading progress.
    [SerializeField] private TMP_Text tipText; // References the text label used to show a random loading tip.

    [Header("Config")] // Adds a labeled section for loading screen settings in the Unity inspector.
    [SerializeField] private float fadeDuration = 1f; // Sets how long fade animations should take.
    [SerializeField] private string[] loadingTips; // Stores the list of possible loading tips.

    private void Start() // Runs once when Unity starts this component.
    { // Starts the Start method body.
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribes to Unity's scene-loaded event.
        loadingPanel.SetActive(false); // Hides the loading panel at startup.
    } // Ends the Start method body.

    public void StartFadeAndLoad(string sceneName) // Starts the loading screen flow for a scene name.
    { // Starts the StartFadeAndLoad method body.
        StartCoroutine(FadeAndLoad(sceneName)); // Starts the coroutine that fades in and tracks load progress.
    } // Ends the StartFadeAndLoad method body.

    private IEnumerator FadeAndLoad(string sceneName) // Shows the loading screen and waits for Photon loading progress.
    { // Starts the FadeAndLoad coroutine body.
        loadingPanel.SetActive(true); // Shows the loading panel.
        tipText.text = loadingTips[Random.Range(0, loadingTips.Length)]; // Selects and displays a random loading tip.
        yield return StartCoroutine(FadeIn()); // Waits for the fade-in coroutine to finish.

        // While loading, show progress bar
        while (PhotonNetwork.LevelLoadingProgress < 1f) // Continues until Photon reports scene loading is complete.
        { // Starts the loading progress loop body.
            progressBarFill.fillAmount = PhotonNetwork.LevelLoadingProgress; // Updates the progress bar fill amount.
            yield return null; // Waits until the next frame before checking progress again.
        } // Ends the loading progress loop body.
    } // Ends the FadeAndLoad coroutine body.

    private IEnumerator FadeIn() // Runs the fade-in timing loop.
    { // Starts the FadeIn coroutine body.
        float elapsed = 0f; // Tracks how much fade time has passed.
        while (elapsed < fadeDuration) // Continues until the configured fade duration has elapsed.
        { // Starts the fade-in loop body.
            elapsed += Time.deltaTime; // Adds the time since the previous frame.
            float alpha = Mathf.Clamp01(elapsed / fadeDuration); // Calculates the fade-in alpha value between 0 and 1.
            yield return null; // Waits until the next frame.
        } // Ends the fade-in loop body.
    } // Ends the FadeIn coroutine body.

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Runs when Unity finishes loading a scene.
    { // Starts the OnSceneLoaded event handler body.
        if (this == null) return; // Stops if this component has already been destroyed.
        StartCoroutine(FadeOut()); // Starts the fade-out coroutine after the scene loads.
    } // Ends the OnSceneLoaded event handler body.

    private IEnumerator FadeOut() // Runs the fade-out timing loop.
    { // Starts the FadeOut coroutine body.
        float elapsed = 0f; // Tracks how much fade time has passed.
        while (elapsed < fadeDuration) // Continues until the configured fade duration has elapsed.
        { // Starts the fade-out loop body.
            elapsed += Time.deltaTime; // Adds the time since the previous frame.
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration); // Calculates the fade-out alpha value between 1 and 0.
            yield return null; // Waits until the next frame.
        } // Ends the fade-out loop body.

        loadingPanel.SetActive(false); // hide loading UI when done
    } // Ends the FadeOut coroutine body.
} // Ends the LoadingScreenManager class body.
