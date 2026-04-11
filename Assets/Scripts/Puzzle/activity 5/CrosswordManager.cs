using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CrosswordManager : MonoBehaviour, IPuzzle, IOnEventCallback
{
    private const byte CrosswordTextSyncEventCode = 41;
    private const string CrosswordCellPropertyPrefix = "P5CW_";

    public GameObject cellPrefab;
    public Transform gridParent; // A GridLayoutGroup
    public int gridSize = 13;

    private CrosswordCell[,] grid;
    private readonly Dictionary<TMP_InputField, UnityAction<string>> inputListeners = new Dictionary<TMP_InputField, UnityAction<string>>();
    private readonly Dictionary<int, int> lastSeqBySender = new Dictionary<int, int>();
    private bool suppressInputSync;
    private int localSeq;

    private char[,] solution = new char[13, 13]
    {
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','l','\0','\0','\0' },   // Row 0
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','a','\0','\0','\0' },   // Row 1
        { '\0','\0','l','i','s','p','r','o','\0','n','p','h','\0' },           // Row 2
        { '\0','\0','\0','\0','\0','\0','e','\0','\0','t','\0','\0','\0' },    // Row 3
        { '\0','\0','\0','\0','\0','\0','g','\0','\0','u','\0','\0','\0' },    // Row 4
        { '\0','\0','\0','\0','g','l','u','l','i','s','i','n','e' },           // Row 5
        { '\0','\0','\0','\0','\0','\0','l','\0','\0','\0','\0','\0','\0'},    // Row 6      
        { '\0','\0','\0','\0','\0','\0','a','s','p','a','r','t','\0' },        // Row 7
        { 'd','e','t','e','m','i','r','\0','\0','\0','\0','o','\0' },          // Row 8
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','u','\0' },   // Row 9
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','j','\0' },   // Row 10
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','e','\0' },   // Row 11
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','o','\0' },   // Row 12                                                 
    };



    void Start()
    {
        GenerateGrid();
        ApplyRoomSyncedTexts();
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        RegisterExistingGridListeners();
        ApplyRoomSyncedTexts();
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        UnregisterGridListeners();
    }

    void GenerateGrid()
    {
        grid = new CrosswordCell[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                CrosswordCell cell = cellGO.GetComponent<CrosswordCell>();

                int visualY = gridSize - 1 - y; // flip vertically to match Unity's layout
                cell.position = new Vector2Int(x, visualY);

                char letter = solution[y, x];
                bool active = letter != '\0';
                cell.SetInteractable(active);
                grid[y, x] = cell;

                if (active && cell.inputField != null)
                    RegisterCellListener(cell, x, y);
            }
        }
    }

    private void RegisterCellListener(CrosswordCell cell, int x, int y)
    {
        TMP_InputField inputField = cell.inputField;
        if (inputField == null || inputListeners.ContainsKey(inputField))
            return;

        UnityAction<string> listener = value => OnCellValueChanged(x, y, value);
        inputField.onValueChanged.AddListener(listener);
        inputListeners[inputField] = listener;
    }

    private void RegisterExistingGridListeners()
    {
        if (grid == null)
            return;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CrosswordCell cell = grid[y, x];
                if (cell == null || cell.inputField == null || !cell.isActiveAndEnabled || !cell.inputField.interactable)
                    continue;

                RegisterCellListener(cell, x, y);
            }
        }
    }

    private void UnregisterGridListeners()
    {
        foreach (var entry in inputListeners)
        {
            if (entry.Key != null)
                entry.Key.onValueChanged.RemoveListener(entry.Value);
        }

        inputListeners.Clear();
    }

    private void OnCellValueChanged(int x, int y, string value)
    {
        if (suppressInputSync || !PhotonNetwork.InRoom)
            return;

        localSeq++;
        SyncCellTextProperty(x, y, value);

        object[] content =
        {
            x,
            y,
            value ?? string.Empty,
            PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1,
            localSeq
        };

        PhotonNetwork.RaiseEvent(
            CrosswordTextSyncEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    private void SyncCellTextProperty(int x, int y, string value)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [BuildCellPropertyKey(x, y)] = value ?? string.Empty
        });
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CrosswordTextSyncEventCode)
            return;

        if (!(photonEvent.CustomData is object[] data) || data.Length < 5)
            return;

        int x = (int)data[0];
        int y = (int)data[1];
        string text = (string)data[2];
        int actor = (int)data[3];
        int seq = (int)data[4];

        int senderKey = (actor * 1000) + (y * gridSize) + x;
        if (lastSeqBySender.TryGetValue(senderKey, out int lastSeq) && seq <= lastSeq)
            return;

        lastSeqBySender[senderKey] = seq;
        ApplyRemoteText(x, y, text);
    }

    private void ApplyRemoteText(int x, int y, string text)
    {
        if (grid == null || y < 0 || y >= gridSize || x < 0 || x >= gridSize)
            return;

        CrosswordCell cell = grid[y, x];
        if (cell == null || cell.inputField == null)
            return;

        suppressInputSync = true;
        cell.inputField.text = text;
        suppressInputSync = false;
    }

    private void ApplyRoomSyncedTexts()
    {
        if (grid == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CrosswordCell cell = grid[y, x];
                if (cell == null || cell.inputField == null || !cell.inputField.interactable)
                    continue;

                string key = BuildCellPropertyKey(x, y);
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
                    continue;

                ApplyRemoteText(x, y, PhotonNetwork.CurrentRoom.CustomProperties[key] as string ?? string.Empty);
            }
        }
    }

    private void ClearSyncedCellTexts()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable clearedValues = new Hashtable();

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (solution[y, x] == '\0')
                    continue;

                clearedValues[BuildCellPropertyKey(x, y)] = string.Empty;
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(clearedValues);
    }

    private static string BuildCellPropertyKey(int x, int y)
    {
        return $"{CrosswordCellPropertyPrefix}{x}_{y}";
    }



    public bool CheckPuzzle()
    {
        bool correct = true;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (solution[y, x] == '\0') continue;

                char expected = solution[y, x];
                char typed = grid[y, x].GetLetter();

                if (char.ToUpper(typed) != char.ToUpper(expected))
                {
                    correct = false;
                    grid[y, x].inputField.SetSyncedFeedbackColor(Color.red);
                }
                else
                {
                    grid[y, x].inputField.SetSyncedFeedbackColor(Color.green);
                }
            }
        }

        return correct; // return result
    }
    public bool IsSolved()
    {
        return CheckPuzzle();
    }
    public void ResetPuzzle()
    {
        UnregisterGridListeners();
        lastSeqBySender.Clear();
        localSeq = 0;
        ClearSyncedCellTexts();

        // Destroy old grid first
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Recreate the grid fresh
        GenerateGrid();

        Debug.Log("Crossword puzzle reset.");
    }



}
