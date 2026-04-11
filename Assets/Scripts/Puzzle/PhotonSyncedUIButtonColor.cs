using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class PhotonSyncedUIButtonColor : MonoBehaviourPun
{
    public event Action<PhotonSyncedUIButtonColor> StateChanged;

    public enum VisualState
    {
        Normal = 0,
        Selected = 1,
        Correct = 2,
        Wrong = 3
    }

    [Header("Group (kept for compatibility, NOT used in multi-select)")]
    [SerializeField] private bool autoGroupByParent = true;
    [SerializeField] private string groupId = "";

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    [Header("Behavior")]
    [Tooltip("Clicking selected button again will deselect (multi-select toggle)")]
    [SerializeField] private bool allowToggleOff = true;

    private Button button;
    private Image image;

    // RESULT state lives per-button (synced)
    [SerializeField] private VisualState resultState = VisualState.Normal;

    // MULTI-SELECT state lives per-button (synced)
    [SerializeField] private bool isSelected = false;

    public bool IsSelected => isSelected;
    public VisualState CurrentResultState => resultState;

    // GROUP selection state (kept for compatibility, not used for multi-select)
    private static readonly Dictionary<string, List<PhotonSyncedUIButtonColor>> Groups = new();
    private static readonly Dictionary<string, int> SelectedViewId = new(); // -1 = none

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        // kept (doesn't harm) – but groupId won't control selection anymore
        if (autoGroupByParent && string.IsNullOrEmpty(groupId))
            groupId = BuildStableGroupId();

        button.onClick.AddListener(OnLocalClick);

        // kept to not break any existing expectations, but NOT used for selection enforcement
        Register();

        ApplyVisual();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnLocalClick);
        Unregister();
    }

    // ─────────────────────────────
    // CLICK (MULTI-SELECT TOGGLE)
    // ─────────────────────────────
    private void OnLocalClick()
    {
        // Multi-select behavior: toggle THIS button only.
        // Correct/Wrong overrides visuals but selection can still be toggled underneath (kept).
        bool nextSelected = isSelected;

        if (allowToggleOff && isSelected)
            nextSelected = false;
        else
            nextSelected = true;

        if (!PhotonNetwork.InRoom)
        {
            isSelected = nextSelected;
            NotifyStateChanged();
            return;
        }

        // Sync only THIS button selection state
        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, nextSelected);
    }

    [PunRPC]
    private void RPC_SetSelected(bool selected)
    {
        isSelected = selected;

        // If you are using resultState.Selected to represent selection,
        // keep it consistent (so older code that checks resultState still works).
        if (resultState == VisualState.Selected && !isSelected)
            resultState = VisualState.Normal;

        NotifyStateChanged();
    }

    // ─────────────────────────────
    // RESULT STATE (Correct/Wrong/Normal)
    // ─────────────────────────────
    public void MarkCorrectAndSync()
    {
        SetResultAndSync(VisualState.Correct);
    }

    public void MarkWrongAndSync()
    {
        SetResultAndSync(VisualState.Wrong);
    }

    public void ClearResultAndSync()
    {
        SetResultAndSync(VisualState.Normal);
    }

    public void ResetStateAndSync()
    {
        if (!PhotonNetwork.InRoom)
        {
            isSelected = false;
            resultState = VisualState.Normal;
            NotifyStateChanged();
            return;
        }

        photonView.RPC(nameof(RPC_SetState), RpcTarget.AllViaServer, false, (int)VisualState.Normal);
    }

    public void MarkSelectedAndSync()
    {
        // In multi-select, "selected" is a separate flag.
        // Keep resultState as Normal (or Selected if you prefer), but selection visual is driven by isSelected.
        if (!PhotonNetwork.InRoom)
        {
            isSelected = true;
            NotifyStateChanged();
            return;
        }

        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, true);
    }

    public void ClearSelectedAndSync()
    {
        if (!PhotonNetwork.InRoom)
        {
            isSelected = false;
            NotifyStateChanged();
            return;
        }

        photonView.RPC(nameof(RPC_SetSelected), RpcTarget.AllViaServer, false);
    }

    private void SetResultAndSync(VisualState newState)
    {
        if (!PhotonNetwork.InRoom)
        {
            resultState = newState;

            // if setting to Normal, keep selection highlight if selected
            NotifyStateChanged();
            return;
        }

        photonView.RPC(nameof(RPC_SetResultState), RpcTarget.AllViaServer, (int)newState);
    }

    [PunRPC]
    private void RPC_SetResultState(int state)
    {
        resultState = (VisualState)state;

        // If external scripts used "Selected" resultState previously, map it to isSelected
        if (resultState == VisualState.Selected)
            isSelected = true;

        NotifyStateChanged();
    }

    [PunRPC]
    private void RPC_SetState(bool selected, int state)
    {
        isSelected = selected;
        resultState = (VisualState)state;
        NotifyStateChanged();
    }

    // ─────────────────────────────
    // VISUAL RESOLUTION
    // Priority: Correct/Wrong overrides Selected/Normal
    // In multi-select: selection comes from `isSelected` (per-button)
    // ─────────────────────────────
    private void ApplyVisual()
    {
        VisualState finalState;

        if (resultState == VisualState.Correct || resultState == VisualState.Wrong)
        {
            finalState = resultState;
        }
        else
        {
            finalState = isSelected ? VisualState.Selected : VisualState.Normal;
        }

        SetVisual(finalState);
    }

    private void SetVisual(VisualState state)
    {
        switch (state)
        {
            case VisualState.Selected: image.color = selectedColor; break;
            case VisualState.Correct: image.color = correctColor; break;
            case VisualState.Wrong: image.color = wrongColor; break;
            default: image.color = normalColor; break;
        }
    }

    private void NotifyStateChanged()
    {
        ApplyVisual();
        StateChanged?.Invoke(this);
    }

    // ─────────────────────────────
    // GROUP REGISTRATION (kept; harmless)
    // ─────────────────────────────
    private void Register()
    {
        if (!Groups.TryGetValue(groupId, out var list))
        {
            list = new List<PhotonSyncedUIButtonColor>();
            Groups[groupId] = list;
        }

        if (!list.Contains(this))
            list.Add(this);

        if (!SelectedViewId.ContainsKey(groupId))
            SelectedViewId[groupId] = -1;
    }

    private void Unregister()
    {
        if (Groups.TryGetValue(groupId, out var list))
            list.Remove(this);
    }

    // Optional: kept for compatibility. In multi-select, this simply clears THIS button.
    public void ClearSelectionAndSync()
    {
        ClearSelectedAndSync();
    }

    // ─────────────────────────────
    // STABLE AUTO GROUP ID (kept)
    // ─────────────────────────────
    private string BuildStableGroupId()
    {
        var parts = new Stack<string>();
        Transform t = transform.parent; // group by parent container
        while (t != null)
        {
            parts.Push(t.name);
            t = t.parent;
        }

        string path = string.Join("/", parts.ToArray());
        return $"Scene:{gameObject.scene.name}|{path}";
    }
}
