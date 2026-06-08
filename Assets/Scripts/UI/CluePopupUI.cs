using System.Collections; // Imports IEnumerator support for coroutines.
using System.Collections.Generic; // Imports Queue collection support.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, CanvasGroup, and Time.
using TMPro; // if you use TextMeshPro; replace with UnityEngine.UI.Text otherwise // Imports TextMeshPro text support.

public class CluePopupUI : MonoBehaviour // Declares the component that queues and animates clue popup messages.
{ // Starts the CluePopupUI class body.
    public static CluePopupUI Instance; // Stores the shared CluePopupUI singleton instance.

    [Header("UI")] // Groups popup UI references in the Unity inspector.
    [SerializeField] private CanvasGroup panel;  // the popup root with CanvasGroup // References the popup root used for visibility and fading.
    [SerializeField] private TMP_Text message;   // or UnityEngine.UI.Text // References the label that displays popup text.

    [Header("Animation")] // Groups popup animation settings in the Unity inspector.
    [SerializeField] private float fadeInDuration = 0.15f; // Controls how long the popup takes to fade in.
    [SerializeField] private float holdDuration = 1.0f; // Controls how long the popup remains fully visible.
    [SerializeField] private float fadeOutDuration = 0.25f; // Controls how long the popup takes to fade out.
    [SerializeField] private float slidePixels = 20f;   // move up slightly during show // Controls how far the popup slides during fade-in.

    private readonly Queue<string> queue = new(); // Stores pending popup messages in display order.
    private bool isShowing; // Tracks whether the popup queue coroutine is currently running.

    private void Awake() // Runs before Start to initialize singleton and hidden UI state.
    { // Starts the Awake method body.
        if (Instance == null) Instance = this; // Registers this component as the singleton if none exists.
        else { Destroy(gameObject); return; } // Destroys duplicate popup UI objects and exits setup.

        if (panel != null) // Checks whether the popup panel reference is assigned.
        { // Starts the panel initialization block.
            panel.alpha = 0f; // Makes the popup fully transparent at startup.
            panel.gameObject.SetActive(false); // Hides the popup GameObject at startup.
        } // Ends the panel initialization block.
    } // Ends the Awake method body.

    /// <summary>Queue and show a popup message.</summary>
    public void Show(string text) // Queues a popup message for display.
    { // Starts the Show method body.
        if (string.IsNullOrWhiteSpace(text)) return; // Ignores empty popup messages.
        queue.Enqueue(text); // Adds the message to the popup queue.
        if (!isShowing) StartCoroutine(RunQueue()); // Starts queue playback if no popup is currently showing.
    } // Ends the Show method body.

    /// <summary>Show a popup message after a real-time delay.</summary>
    public void ShowDelayed(string text, float delaySeconds) // Queues a popup message after a real-time delay.
    { // Starts the ShowDelayed method body.
        if (string.IsNullOrWhiteSpace(text)) return; // Ignores empty delayed popup messages.

        if (delaySeconds <= 0f) // Checks whether the message should show immediately.
        { // Starts the immediate-show block.
            Show(text); // Queues the message without starting a delay coroutine.
            return; // Leaves because the message was already queued.
        } // Ends the immediate-show block.

        StartCoroutine(DelayedShow(text, delaySeconds)); // Starts a coroutine that queues the message after the delay.
    } // Ends the ShowDelayed method body.

    private IEnumerator RunQueue() // Plays queued popup messages one at a time.
    { // Starts the RunQueue coroutine body.
        isShowing = true; // Marks the popup queue as active.
        while (queue.Count > 0) // Runs while messages remain in the queue.
        { // Starts the queue loop body.
            string text = queue.Dequeue(); // Takes the next popup message from the queue.
            yield return StartCoroutine(PlayOnce(text)); // Waits for this popup animation to complete.
        } // Ends the queue loop body.
        isShowing = false; // Marks the popup queue as inactive after all messages play.
    } // Ends the RunQueue coroutine body.

    private IEnumerator DelayedShow(string text, float delaySeconds) // Waits before queueing a popup message.
    { // Starts the DelayedShow coroutine body.
        yield return new WaitForSecondsRealtime(delaySeconds); // Waits using real time so pause state does not affect the delay.
        Show(text); // Queues the popup message after the delay finishes.
    } // Ends the DelayedShow coroutine body.

    private IEnumerator PlayOnce(string text) // Animates one popup message from fade-in to fade-out.
    { // Starts the PlayOnce coroutine body.
        if (panel == null) yield break; // Stops if no popup panel is assigned.

        panel.gameObject.SetActive(true); // Shows the popup GameObject before animating.
        if (message != null) message.text = text; // Updates the popup message text when the label exists.

        // prepare positions
        var rt = panel.transform as RectTransform; // Reads the panel RectTransform for anchored-position animation.
        Vector2 startPos = rt.anchoredPosition; // Stores the panel's original anchored position.
        Vector2 fromPos = startPos - new Vector2(0f, slidePixels); // Calculates the slide-in starting position.
        Vector2 toPos = startPos; // Stores the slide-in ending position.

        // fade in + slide
        float t = 0f; // Initializes the fade-in elapsed time.
        while (t < fadeInDuration) // Runs until the fade-in duration is complete.
        { // Starts the fade-in loop body.
            t += Time.unscaledDeltaTime; // Advances elapsed time using unscaled time.
            float k = Mathf.Clamp01(t / fadeInDuration); // Converts elapsed time into a 0-to-1 animation amount.
            panel.alpha = k; // Fades the popup in.
            rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, k); // Slides the popup toward its final position.
            yield return null; // Waits one frame before continuing the animation.
        } // Ends the fade-in loop body.
        panel.alpha = 1f; // Ensures the popup is fully visible after fade-in.
        rt.anchoredPosition = toPos; // Ensures the popup ends at its intended position.

        // hold
        float h = 0f; // Initializes the fully-visible hold timer.
        while (h < holdDuration) // Runs while the popup should remain visible.
        { // Starts the hold loop body.
            h += Time.unscaledDeltaTime; // Advances hold time using unscaled time.
            yield return null; // Waits one frame before continuing the hold.
        } // Ends the hold loop body.

        // fade out
        t = 0f; // Resets elapsed time for fade-out.
        while (t < fadeOutDuration) // Runs until the fade-out duration is complete.
        { // Starts the fade-out loop body.
            t += Time.unscaledDeltaTime; // Advances fade-out time using unscaled time.
            float k = Mathf.Clamp01(t / fadeOutDuration); // Converts elapsed time into a 0-to-1 fade amount.
            panel.alpha = 1f - k; // Fades the popup out.
            yield return null; // Waits one frame before continuing the fade.
        } // Ends the fade-out loop body.
        panel.alpha = 0f; // Ensures the popup is fully transparent after fade-out.
        panel.gameObject.SetActive(false); // Hides the popup GameObject after the animation.

        // reset position
        rt.anchoredPosition = startPos; // Restores the popup's original anchored position.
    } // Ends the PlayOnce coroutine body.
} // Ends the CluePopupUI class body.
