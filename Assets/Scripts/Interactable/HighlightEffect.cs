using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightEffect : MonoBehaviour
{
    private List<Renderer> renderers = new();
    private List<Material[]> materials = new();
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 1.5f;
    [SerializeField] private float highlightScaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 5f;

    [Header("Child Filtering")]
    [Tooltip("If true, only children in AllowedChildren will be highlighted. If false, all children EXCEPT those in ExcludedChildren will highlight.")]
    [SerializeField] private bool useWhitelist = false;

    [Tooltip("Children explicitly allowed (used only if useWhitelist = true).")]
    [SerializeField] private List<Renderer> allowedChildren = new();

    [Tooltip("Children explicitly excluded (ignored if useWhitelist = true).")]
    [SerializeField] private List<Renderer> excludedChildren = new();

    private void Awake()
    {
        renderers.Clear();
        materials.Clear();

        var allRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (var r in allRenderers)
        {
            // Skip based on whitelist/exclude logic
            if (useWhitelist && !allowedChildren.Contains(r)) continue;
            if (!useWhitelist && excludedChildren.Contains(r)) continue;

            renderers.Add(r);

            // Get unique material instances
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m != null)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", Color.black);
                }
            }
            materials.Add(mats);
        }

        originalScale = transform.localScale;
    }

    private bool IsAlive => this && isActiveAndEnabled;

    public void Highlight()
    {
        if (!IsAlive) return;

        foreach (var mats in materials)
        {
            foreach (var mat in mats)
            {
                if (mat != null)
                    mat.SetColor("_EmissionColor", highlightColor * emissionIntensity);
            }
        }

        RestartScale(originalScale * highlightScaleMultiplier);
    }

    public void Unhighlight()
    {
        if (!IsAlive) return;

        foreach (var mats in materials)
        {
            foreach (var mat in mats)
            {
                if (mat != null)
                    mat.SetColor("_EmissionColor", Color.black);
            }
        }

        RestartScale(originalScale);
    }

    private void RestartScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null) { StopCoroutine(scaleCoroutine); scaleCoroutine = null; }
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale));
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (this && Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }

        if (this) transform.localScale = targetScale;
    }

    private void OnDisable()
    {
        foreach (var mats in materials)
        {
            foreach (var mat in mats)
            {
                if (mat != null)
                    mat.SetColor("_EmissionColor", Color.black);
            }
        }

        StopAllCoroutines();
        scaleCoroutine = null;

        if (this)
            transform.localScale = originalScale;
    }
}
