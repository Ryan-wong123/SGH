using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlideshowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [System.Serializable]
    public struct SlideData
    {
        public Sprite image;
        public float duration;   // how long this slide stays (seconds)
        public bool isPortrait;    // true = portrait, false = landscape
    }

    [Header("Slides")]
    [SerializeField] private SlideData[] slides;

    private int currentIndex = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip onEnableClip;

    [Header("Autoplay")]
    [SerializeField] private bool autoPlay = false;
    [SerializeField] private bool loop = false;
    [SerializeField] private float defaultSlideDuration = 2f;

    [Header("Image Layout")]
    [SerializeField] private Vector2 portraitSize = new Vector2(600, 900);
    [SerializeField] private Vector2 landscapeSize = new Vector2(900, 600);
    [SerializeField] private bool preserveAspect = true;

    private Coroutine autoPlayRoutine;

    private void OnEnable()
    {
        // Play audio when panel is enabled
        if (audioSource && onEnableClip)
        {
            audioSource.clip = onEnableClip;
            audioSource.Play();
        }

        // Start autoplay (if enabled and we have slides)
        if (autoPlay && slides.Length > 0)
            StartAutoPlay();
    }

    private void OnDisable()
    {
        // Stop audio
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        // Stop autoplay coroutine
        if (autoPlayRoutine != null)
        {
            StopCoroutine(autoPlayRoutine);
            autoPlayRoutine = null;
        }
    }

    private void Start()
    {
        if (slides.Length == 0)
        {
            Debug.LogWarning("No slides assigned!");
            return;
        }

        // Show first slide
        UpdateImage();

        // Hook up buttons
        prevButton.onClick.AddListener(ShowPrevImage);
        nextButton.onClick.AddListener(ShowNextImage);

        // Initial button state
        UpdateButtonState();
    }

    private void ShowPrevImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateImage();
            UpdateButtonState();

            // Reset autoplay timer after manual navigation
            if (autoPlay)
                StartAutoPlay();
        }
    }

    private void ShowNextImage()
    {
        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            UpdateImage();
            UpdateButtonState();

            // Reset autoplay timer after manual navigation
            if (autoPlay)
                StartAutoPlay();
        }
    }

    private void UpdateImage()
    {
        SlideData slide = slides[currentIndex];

        displayImage.sprite = slide.image;
        displayImage.preserveAspect = preserveAspect;

        RectTransform rt = displayImage.rectTransform;

        if (slide.isPortrait)
        {
            rt.sizeDelta = portraitSize;
        }
        else
        {
            rt.sizeDelta = landscapeSize;
        }
    }

    private void UpdateButtonState()
    {
        // Prev disabled at first slide (unless you want looping via buttons)
        prevButton.interactable = currentIndex > 0;

        if (!loop)
        {
            // Next disabled at last slide
            nextButton.interactable = currentIndex < slides.Length - 1;
        }
        else
        {
            // If looping, always allow
            prevButton.interactable = true;
            nextButton.interactable = true;
        }
    }

    // ---------- AUTOPLAY ----------

    private void StartAutoPlay()
    {
        if (slides.Length == 0)
            return;

        // Stop any existing autoplay coroutine so we restart delay
        if (autoPlayRoutine != null)
            StopCoroutine(autoPlayRoutine);

        autoPlayRoutine = StartCoroutine(AutoPlayRoutine());
    }

    private IEnumerator AutoPlayRoutine()
    {
        while (true)
        {
            // Get this slide's duration; fall back to default if not set or <= 0
            float duration = slides[currentIndex].duration;
            if (duration <= 0f)
                duration = defaultSlideDuration;

            // IMPORTANT: Use real-time so it still runs if Time.timeScale == 0
            yield return new WaitForSecondsRealtime(duration);

            // Move to next slide
            if (currentIndex < slides.Length - 1)
            {
                currentIndex++;
                UpdateImage();
                UpdateButtonState();
            }
            else
            {
                if (loop)
                {
                    currentIndex = 0;
                    UpdateImage();
                    UpdateButtonState();
                }
                else
                {
                    // End of slideshow, stop autoplay
                    autoPlayRoutine = null;
                    yield break;
                }
            }
        }
    }
}
