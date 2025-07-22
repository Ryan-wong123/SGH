using System.Collections;
using UnityEngine;

public class HighlightEffect : MonoBehaviour
{
    private Renderer rend;
    private Material material;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 1.5f;
    [SerializeField] private float highlightScaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 5f;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            material = rend.material;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
        originalScale = transform.localScale;
    }

    public void Highlight()
    {
        if (material != null)
            material.SetColor("_EmissionColor", highlightColor * emissionIntensity);

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * highlightScaleMultiplier));
    }

    public void Unhighlight()
    {
        if (material != null)
            material.SetColor("_EmissionColor", Color.black);

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = targetScale;
    }
}
