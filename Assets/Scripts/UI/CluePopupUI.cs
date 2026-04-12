using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // if you use TextMeshPro; replace with UnityEngine.UI.Text otherwise

public class CluePopupUI : MonoBehaviour
{
    public static CluePopupUI Instance;

    [Header("UI")]
    [SerializeField] private CanvasGroup panel;  // the popup root with CanvasGroup
    [SerializeField] private TMP_Text message;   // or UnityEngine.UI.Text

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float slidePixels = 20f;   // move up slightly during show

    private readonly Queue<string> queue = new();
    private bool isShowing;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }
    }

    /// <summary>Queue and show a popup message.</summary>
    public void Show(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        queue.Enqueue(text);
        if (!isShowing) StartCoroutine(RunQueue());
    }

    /// <summary>Show a popup message after a real-time delay.</summary>
    public void ShowDelayed(string text, float delaySeconds)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (delaySeconds <= 0f)
        {
            Show(text);
            return;
        }

        StartCoroutine(DelayedShow(text, delaySeconds));
    }

    private IEnumerator RunQueue()
    {
        isShowing = true;
        while (queue.Count > 0)
        {
            string text = queue.Dequeue();
            yield return StartCoroutine(PlayOnce(text));
        }
        isShowing = false;
    }

    private IEnumerator DelayedShow(string text, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        Show(text);
    }

    private IEnumerator PlayOnce(string text)
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true);
        if (message != null) message.text = text;

        // prepare positions
        var rt = panel.transform as RectTransform;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 fromPos = startPos - new Vector2(0f, slidePixels);
        Vector2 toPos = startPos;

        // fade in + slide
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeInDuration);
            panel.alpha = k;
            rt.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            yield return null;
        }
        panel.alpha = 1f;
        rt.anchoredPosition = toPos;

        // hold
        float h = 0f;
        while (h < holdDuration)
        {
            h += Time.unscaledDeltaTime;
            yield return null;
        }

        // fade out
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            panel.alpha = 1f - k;
            yield return null;
        }
        panel.alpha = 0f;
        panel.gameObject.SetActive(false);

        // reset position
        rt.anchoredPosition = startPos;
    }
}
