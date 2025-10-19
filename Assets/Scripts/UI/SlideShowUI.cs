using UnityEngine;
using UnityEngine.UI;

public class SlideshowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image displayImage;    // The UI Image in the panel
    [SerializeField] private Button prevButton;     // Left arrow
    [SerializeField] private Button nextButton;     // Right arrow

    [Header("Images")]
    [SerializeField] private Sprite[] slideshowImages;  // Your image set

    private int currentIndex = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;    // Assign AudioSource component
    [SerializeField] private AudioClip onEnableClip;     // Clip to play when panel is shown

    private void OnEnable()
    {
        // Play the sound when the panel is enabled
        if (audioSource != null && onEnableClip != null)
        {
            audioSource.clip = onEnableClip;
            audioSource.Play();
        }
    }
    private void OnDisable()
    {
        // Stop sound when panel is disabled
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void Start()
    {
        if (slideshowImages.Length == 0)
        {
            Debug.LogWarning("Slideshow has no images assigned!");
            return;
        }

        // Show first image
        UpdateImage();

        // Hook up buttons
        prevButton.onClick.AddListener(ShowPrevImage);
        nextButton.onClick.AddListener(ShowNextImage);

        // Initialize button state
        UpdateButtonState();
    }

    private void ShowPrevImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateImage();
            UpdateButtonState();
        }
    }

    private void ShowNextImage()
    {
        if (currentIndex < slideshowImages.Length - 1)
        {
            currentIndex++;
            UpdateImage();
            UpdateButtonState();
        }
    }

    private void UpdateImage()
    {
        displayImage.sprite = slideshowImages[currentIndex];
    }

    private void UpdateButtonState()
    {
        // Disable prev button if at first image
        prevButton.interactable = currentIndex > 0;

        // Disable next button if at last image
        nextButton.interactable = currentIndex < slideshowImages.Length - 1;
    }
}
