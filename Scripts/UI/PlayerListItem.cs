using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    public ulong clientID = ulong.MaxValue;
    [SerializeField] public Button kickBtn;
    [SerializeField] TextMeshProUGUI readyText;
    [SerializeField] TextMeshProUGUI nameText;
    LobbyManager lobbyManager;
    void Start()
    {
        lobbyManager = FindAnyObjectByType<LobbyManager>();
        if (lobbyManager.IsClient)
        {
            DestroyKickBtn();
        }
    }
    public void ReadyUnready(string ready)
    {
        readyText.text = ready;
    }
    public void SetName(string name)
    {
        nameText.text = name;
    }
    public void SetColor(bool isReady)
    {
        readyText.color=isReady?Color.green:Color.red;
    }
    public void DestroyKickBtn()
    {
        if (kickBtn != null)
        {
            Destroy(kickBtn.gameObject);
        }
    }
    public void KickClient()
    {
        if (clientID != ulong.MaxValue)
        {
            lobbyManager.KickClient(clientID);
        }
    }
}
