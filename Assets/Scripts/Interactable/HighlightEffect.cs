using System.Collections; // Imports IEnumerator for Unity coroutines.
using System.Collections.Generic; // Imports List<T> for storing renderers and materials.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Renderer, Material, Color, and Vector3.

public class HighlightEffect : MonoBehaviour // Defines a component that highlights renderers and optionally scales the object.
{ // Opens the HighlightEffect class body.
    private List<Renderer> renderers = new(); // Stores the renderers that are allowed to receive the highlight effect.
    private List<Material[]> materials = new(); // Stores each renderer's material instances for emission color changes.
    private Vector3 originalScale; // Stores the object's starting local scale so it can be restored.
    private Coroutine scaleCoroutine; // Tracks the currently running scale animation coroutine.

    [Header("Highlight Settings")] // Groups the main highlight fields in the Inspector.
    [SerializeField] private Color highlightColor = Color.yellow; // Stores the emission color used while highlighted.
    [SerializeField] private float emissionIntensity = 1.5f; // Stores how strongly the highlight color is multiplied.
    [SerializeField] private float highlightScaleMultiplier = 1.1f; // Stores how much larger the object becomes while highlighted.
    [SerializeField] private float scaleSpeed = 5f; // Stores how quickly the object lerps toward its target scale.

    [Header("Optional Scaling")] // Groups the scale toggle in the Inspector.
    [Tooltip("If true, Highlight/Unhighlight will scale the object. If false, only emission highlighting is applied.")] // Shows Inspector help text for the scaling toggle.
    public bool enableScaling = true; // Stores whether highlighting should also animate the object's scale.

    [Header("Child Filtering")] // Groups child renderer filtering fields in the Inspector.
    [Tooltip("If true, only children in AllowedChildren will be highlighted. If false, all children EXCEPT those in ExcludedChildren will highlight.")] // Explains how whitelist mode changes renderer selection.
    [SerializeField] private bool useWhitelist = false; // Stores whether allowedChildren should be used as a whitelist.

    [Tooltip("Children explicitly allowed (used only if useWhitelist = true).")] // Shows Inspector help text for allowed child renderers.
    [SerializeField] private List<Renderer> allowedChildren = new(); // Stores renderers that may be highlighted when whitelist mode is enabled.

    [Tooltip("Children explicitly excluded (ignored if useWhitelist = true).")] // Shows Inspector help text for excluded child renderers.
    [SerializeField] private List<Renderer> excludedChildren = new(); // Stores renderers that should be skipped when whitelist mode is disabled.

    private void Awake() // Runs Unity initialization before the first frame.
    { // Opens the Awake method body.
        renderers.Clear(); // Clears any existing renderer cache.
        materials.Clear(); // Clears any existing material cache.

        var allRenderers = GetComponentsInChildren<Renderer>(includeInactive: true); // Finds every child renderer, including inactive ones.

        foreach (var r in allRenderers) // Loops through every renderer found under this object.
        { // Opens the renderer setup loop body.
            // Skip based on whitelist/exclude logic // Explains the filtering checks below.
            if (useWhitelist && !allowedChildren.Contains(r)) continue; // Skips renderers not listed in the whitelist.
            if (!useWhitelist && excludedChildren.Contains(r)) continue; // Skips renderers listed in the exclusion list.

            renderers.Add(r); // Adds the accepted renderer to the highlight renderer cache.

            // Get unique material instances // Notes that Renderer.materials creates material instances for editing.
            var mats = r.materials; // Gets this renderer's material instances.
            foreach (var m in mats) // Loops through every material on this renderer.
            { // Opens the material setup loop body.
                if (m != null) // Checks that the material slot is valid.
                { // Opens the valid-material block.
                    m.EnableKeyword("_EMISSION"); // Enables the shader emission keyword for this material.
                    m.SetColor("_EmissionColor", Color.black); // Starts the material with no visible emission highlight.
                } // Closes the valid-material block.
            } // Closes the material setup loop body.
            materials.Add(mats); // Stores this renderer's material array for later highlight changes.
        } // Closes the renderer setup loop body.

        originalScale = transform.localScale; // Records the initial local scale for highlight scaling and reset.
    } // Closes the Awake method body.

    private bool IsAlive => this && isActiveAndEnabled; // Returns true only when this component still exists and is enabled.

    public void Highlight() // Applies the highlight effect.
    { // Opens the Highlight method body.
        if (!IsAlive) return; // Stops if the component has been destroyed or disabled.

        foreach (var mats in materials) // Loops through every cached material array.
        { // Opens the material-array loop body.
            foreach (var mat in mats) // Loops through every material in the current array.
            { // Opens the material loop body.
                if (mat != null) // Checks that the material still exists.
                    mat.SetColor("_EmissionColor", highlightColor * emissionIntensity); // Sets the emission color to the bright highlight color.
            } // Closes the material loop body.
        } // Closes the material-array loop body.

        if (enableScaling) // Checks whether the object should scale while highlighted.
        { // Opens the scaling-enabled block.
            RestartScale(originalScale * highlightScaleMultiplier); // Starts scaling toward the enlarged highlight size.
        } // Closes the scaling-enabled block.
        else // Handles highlight mode without scaling.
        { // Opens the scaling-disabled block.
            StopScaleAndSnapToOriginal(); // Stops any scaling animation and restores the original scale.
        } // Closes the scaling-disabled block.
    } // Closes the Highlight method body.

    public void Unhighlight() // Removes the highlight effect.
    { // Opens the Unhighlight method body.
        if (!IsAlive) return; // Stops if the component has been destroyed or disabled.

        foreach (var mats in materials) // Loops through every cached material array.
        { // Opens the material-array loop body.
            foreach (var mat in mats) // Loops through every material in the current array.
            { // Opens the material loop body.
                if (mat != null) // Checks that the material still exists.
                    mat.SetColor("_EmissionColor", Color.black); // Clears the emission highlight color.
            } // Closes the material loop body.
        } // Closes the material-array loop body.

        if (enableScaling) // Checks whether scale animation is enabled.
        { // Opens the scaling-enabled block.
            RestartScale(originalScale); // Starts scaling back to the original size.
        } // Closes the scaling-enabled block.
        else // Handles unhighlight mode without scaling.
        { // Opens the scaling-disabled block.
            StopScaleAndSnapToOriginal(); // Stops any scaling animation and restores the original scale.
        } // Closes the scaling-disabled block.
    } // Closes the Unhighlight method body.

    private void RestartScale(Vector3 targetScale) // Restarts the scale coroutine toward a new target scale.
    { // Opens the RestartScale method body.
        if (!enableScaling) // Checks whether scaling has been disabled.
        { // Opens the disabled-scaling guard block.
            StopScaleAndSnapToOriginal(); // Stops current scaling and restores the original scale.
            return; // Stops because no new scaling coroutine should run.
        } // Closes the disabled-scaling guard block.

        if (scaleCoroutine != null) { StopCoroutine(scaleCoroutine); scaleCoroutine = null; } // Stops any previous scale coroutine and clears its reference.
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale)); // Starts a new coroutine toward the requested target scale.
    } // Closes the RestartScale method body.

    private void StopScaleAndSnapToOriginal() // Stops scale animation and restores the starting scale.
    { // Opens the scale reset method body.
        if (scaleCoroutine != null) // Checks whether a scale coroutine is currently running.
        { // Opens the active-coroutine block.
            StopCoroutine(scaleCoroutine); // Stops the running scale coroutine.
            scaleCoroutine = null; // Clears the coroutine reference after stopping it.
        } // Closes the active-coroutine block.

        if (this) // Checks that the Unity object still exists.
            transform.localScale = originalScale; // Restores the object's original local scale.
    } // Closes the scale reset method body.

    private IEnumerator ScaleTo(Vector3 targetScale) // Smoothly scales the object toward a target local scale.
    { // Opens the ScaleTo coroutine body.
        while (this && Vector3.Distance(transform.localScale, targetScale) > 0.01f) // Continues until the object exists and is close to the target scale.
        { // Opens the scale loop body.
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed); // Moves the scale partway toward the target this frame.
            yield return null; // Waits one frame before continuing the scale animation.
        } // Closes the scale loop body.

        if (this) transform.localScale = targetScale; // Snaps exactly to the target scale if the object still exists.
    } // Closes the ScaleTo coroutine body.

    private void OnDisable() // Runs when Unity disables this component or GameObject.
    { // Opens the OnDisable method body.
        foreach (var mats in materials) // Loops through every cached material array.
        { // Opens the material-array cleanup loop body.
            foreach (var mat in mats) // Loops through every material in the current array.
            { // Opens the material cleanup loop body.
                if (mat != null) // Checks that the material still exists.
                    mat.SetColor("_EmissionColor", Color.black); // Clears the emission highlight before disabling.
            } // Closes the material cleanup loop body.
        } // Closes the material-array cleanup loop body.

        StopAllCoroutines(); // Stops all coroutines running on this component.
        scaleCoroutine = null; // Clears the tracked scale coroutine reference.

        if (this) // Checks that the Unity object still exists.
            transform.localScale = originalScale; // Restores the original local scale before disabling.
    } // Closes the OnDisable method body.
} // Closes the HighlightEffect class body.
