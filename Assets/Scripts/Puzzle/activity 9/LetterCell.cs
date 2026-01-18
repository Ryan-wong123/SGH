using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class LetterCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    [Header("UI")]
    public TMP_Text letterText;
    public Image backgroundImage;

    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public bool IsPlayable { get; private set; }

    private WordSearchManager game;

    // Visual state
    private bool isSelected = false;
    private bool hasResult = false; // true when green/red applied

    // Photon
    private PhotonView pv;

    // Prevent feedback loops when applying remote updates
    private bool suppressNet;

    // ---- Init from manager ----
    public void Init(WordSearchManager mgr, Vector2Int pos)
    {
        game = mgr;
        gridPosition = pos;
    }

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    public void SetPlayable(bool playable)
    {
        IsPlayable = playable;
        if (backgroundImage != null) backgroundImage.raycastTarget = playable;

        // (Optional) If you want playable state synced too, uncomment:
        // if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        //     pv.RPC(nameof(RPC_SetPlayable), RpcTarget.Others, playable);
    }

    // ---- Content ----
    public void SetLetter(char c) => letterText.text = c.ToString();
    public void SetLetterString(string s) => letterText.text = s;
    public char GetLetter() => letterText.text.Length > 0 ? letterText.text[0] : ' ';

    // ---- Visuals ----
    public void SetSelectedVisual(bool selected)
    {
        isSelected = selected;
        if (hasResult) return; // keep green/red until cleared
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // yellow or white

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_SetSelectedVisual), RpcTarget.Others, selected);
        }
    }

    public void SetResultCorrect()
    {
        hasResult = true;
        if (backgroundImage != null) backgroundImage.color = Color.green;

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_SetResultCorrect), RpcTarget.Others);
        }
    }

    public void SetResultWrong()
    {
        hasResult = true;
        if (backgroundImage != null) backgroundImage.color = Color.red;

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_SetResultWrong), RpcTarget.Others);
        }
    }

    public void ClearResult()
    {
        hasResult = false;
        // Re-apply selection coloring
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white;

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_ClearResult), RpcTarget.Others);
        }
    }

    public void DisableCellUI()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
            backgroundImage.raycastTarget = false;
        }

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_DisableCellUI), RpcTarget.Others);
        }
    }

    public void SetTextColor(Color color)
    {
        if (letterText != null) letterText.color = color;

        // ✅ Sync to others (send floats because Color is not always safe in RPC across setups)
        if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        {
            pv.RPC(nameof(RPC_SetTextColor), RpcTarget.Others, color.r, color.g, color.b, color.a);
        }
    }

    // ─────────────────────────────
    // RPC RECEIVERS (remote applies)
    // ─────────────────────────────
    [PunRPC]
    private void RPC_SetSelectedVisual(bool selected)
    {
        suppressNet = true;
        SetSelectedVisual_NoNet(selected);
        suppressNet = false;
    }

    [PunRPC]
    private void RPC_SetResultCorrect()
    {
        suppressNet = true;
        SetResultCorrect_NoNet();
        suppressNet = false;
    }

    [PunRPC]
    private void RPC_SetResultWrong()
    {
        suppressNet = true;
        SetResultWrong_NoNet();
        suppressNet = false;
    }

    [PunRPC]
    private void RPC_ClearResult()
    {
        suppressNet = true;
        ClearResult_NoNet();
        suppressNet = false;
    }

    [PunRPC]
    private void RPC_DisableCellUI()
    {
        suppressNet = true;
        DisableCellUI_NoNet();
        suppressNet = false;
    }

    [PunRPC]
    private void RPC_SetTextColor(float r, float g, float b, float a)
    {
        suppressNet = true;
        if (letterText != null) letterText.color = new Color(r, g, b, a);
        suppressNet = false;
    }

    // Optional playable sync if you enable it above:
    // [PunRPC]
    // private void RPC_SetPlayable(bool playable)
    // {
    //     suppressNet = true;
    //     SetPlayable(playable);
    //     suppressNet = false;
    // }

    // ─────────────────────────────
    // Local-only helpers (same logic, no RPC)
    // ─────────────────────────────
    private void SetSelectedVisual_NoNet(bool selected)
    {
        isSelected = selected;
        if (hasResult) return;
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white;
    }

    private void SetResultCorrect_NoNet()
    {
        hasResult = true;
        if (backgroundImage != null) backgroundImage.color = Color.green;
    }

    private void SetResultWrong_NoNet()
    {
        hasResult = true;
        if (backgroundImage != null) backgroundImage.color = Color.red;
    }

    private void ClearResult_NoNet()
    {
        hasResult = false;
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white;
    }

    private void DisableCellUI_NoNet()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
            backgroundImage.raycastTarget = false;
        }
    }

    // ---- Pointer input ----
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return;
        game.BeginDrag(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsPlayable) return;
        if (Input.GetMouseButton(0))
            game.DragOver(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return;
        game.EndDrag();
    }
}
