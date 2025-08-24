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
    }

    private void ShowPrevImage()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = slideshowImages.Length - 1; // wrap around
        UpdateImage();
    }

    private void ShowNextImage()
    {
        currentIndex++;
        if (currentIndex >= slideshowImages.Length)
            currentIndex = 0; // wrap around
        UpdateImage();
    }

    private void UpdateImage()
    {
        displayImage.sprite = slideshowImages[currentIndex];
    }
}
