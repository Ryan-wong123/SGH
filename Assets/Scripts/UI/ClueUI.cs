using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // <- for TMP_Text

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
    public static ClueUI Instance; // singleton for easy access from ClueSystem

    [Header("Main Root UI Panel (all clue panels inside)")]
    [SerializeField] private GameObject mainRoot;

    [Header("Clue Panels (id → panel)")]
    [SerializeField] private List<ClueEntry> clues = new();

    [Header("General Close Button (inside mainRoot)")]
    [SerializeField] private Button closeButton;

    [Header("Unread Badge (on your Clue button)")]
    [Tooltip("Red dot (or container) that shows the unread number.")]
    [SerializeField] private GameObject unreadBadgeRoot;
    [SerializeField] private TMP_Text unreadText; // number label

    [Header("Optional: extra badge panel to auto-close")]
    [Tooltip("If you have a small dropdown/panel tied to the badge, assign it so we can SetActive(false).")]
    [SerializeField] private GameObject badgePanelRoot;

    private readonly Dictionary<string, GameObject> _panels = new();
    private GameObject _currentPanel;
    private Action _onClosed;

    // unread counter
    private int _unreadCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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

        UpdateUnreadUI(); // ensure correct initial state
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

        // You wanted it to reset when opening as well
        ResetUnreadCount();

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

        // You also wanted it to reset when closing
        ResetUnreadCount();
        // Force-close any optional badge panel
        if (badgePanelRoot != null && badgePanelRoot.activeSelf)
            badgePanelRoot.SetActive(false);
    }

    public bool HasClueId(string id) =>
        !string.IsNullOrWhiteSpace(id) && _panels.ContainsKey(id);

    // ---------- Unread badge API (called by ClueSystem) ----------

    public void IncrementUnread()
    {
        _unreadCount++;
        UpdateUnreadUI();
    }

    private void ResetUnreadCount()
    {
        _unreadCount = 0;
        UpdateUnreadUI();
    }

    private void UpdateUnreadUI()
    {
        bool show = _unreadCount > 0;

        if (unreadBadgeRoot != null)
            unreadBadgeRoot.SetActive(show);

        if (unreadText != null)
            unreadText.text = _unreadCount.ToString();
    }
}
