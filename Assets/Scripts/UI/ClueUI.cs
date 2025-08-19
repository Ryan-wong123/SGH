using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ClueEntry
{
    [Tooltip("Unique ID for this clue (e.g., 'clue_diary_page1')")]
    public string id;

    [Tooltip("Panel GameObject to toggle for this clue (child of mainRoot)")]
    public GameObject panelRoot;
}

public class ClueUI : MonoBehaviour
{
    [Header("Main Root UI Panel (all clue panels inside)")]
    [SerializeField] private GameObject mainRoot;

    [Header("Clue Panels (id → panel)")]
    [SerializeField] private List<ClueEntry> clues = new();

    [Header("General Close Button (inside mainRoot)")]
    [SerializeField] private Button closeButton;

    private readonly Dictionary<string, GameObject> _panels = new();
    private GameObject _currentPanel;
    private Action _onClosed;

    private void Awake()
    {
        // Map IDs to panels, hide them all
        _panels.Clear();
        foreach (var c in clues)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.id) || c.panelRoot == null) continue;

            if (_panels.ContainsKey(c.id))
                Debug.LogWarning($"Duplicate clue id '{c.id}' in ClueUI.");
            else
                _panels.Add(c.id, c.panelRoot);

            c.panelRoot.SetActive(false);
        }

        if (mainRoot != null) mainRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => Hide(true));
    }

    /// <summary>Show the panel for a clue by its id.</summary>
    public bool ShowById(string id, Action onClosed = null)
    {
        if (string.IsNullOrWhiteSpace(id) || !_panels.TryGetValue(id, out var panel))
        {
            Debug.LogWarning($"ClueUI: No panel found for id '{id}'.");
            return false;
        }

        // Hide any currently visible panel first
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        _currentPanel = panel;
        _onClosed = onClosed;

        if (mainRoot != null) mainRoot.SetActive(true);
        _currentPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        return true;
    }

    /// <summary>Hide the current clue panel + root. If userAction=true, invoke onClosed callback.</summary>
    public void Hide(bool userAction)
    {
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        if (mainRoot != null)
            mainRoot.SetActive(false);

        Time.timeScale = 1f;

        if (userAction && _onClosed != null)
        {
            var cb = _onClosed;
            _onClosed = null;
            cb.Invoke();
        }

        _currentPanel = null;
    }

    public bool HasClueId(string id) =>
        !string.IsNullOrWhiteSpace(id) && _panels.ContainsKey(id);
}
