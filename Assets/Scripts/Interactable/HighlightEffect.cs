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

    private void Awake()
    {
        // Grab ALL renderers on this object and its children
        renderers.AddRange(GetComponentsInChildren<Renderer>(includeInactive: true));

        foreach (var r in renderers)
        {
            // Create unique instances so you don’t affect shared assets
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    mats[i].EnableKeyword("_EMISSION");
                    mats[i].SetColor("_EmissionColor", Color.black);
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
