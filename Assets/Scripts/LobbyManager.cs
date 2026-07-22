using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] public Button StartBtn;
    [SerializeField] public Button ReadyBtn;
    [SerializeField] HandleConnectedClients handleConnectedClients;
    [SerializeField] GameManager gameManager;
    public Dictionary<ulong, bool> ClientReadyStatus = new Dictionary<ulong, bool>();
    public bool localIsReady = true;
    [SerializeField] public Slider slider;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI lobbyCodeText;
    public bool IsClient = false;
    [SerializeField] Button lastGameMode;
    [SerializeField] List<GameObject> hostOnlyObjects = new();

    void OnEnable()
    {
        if (IsClient)
        {
            ReadyBtn.image.color = Color.red;
            foreach (GameObject g in hostOnlyObjects)
            {
                Destroy(g);
            }
            localIsReady = false;
            //ReadyUnReady();
        }
        gameManager.SetGameMode("Special");
        lobbyCodeText.text = MyUtilities.LobbyCode;
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        slider.minValue = 2;
        slider.maxValue = 13;
        slider.value = 8;
        slider.wholeNumbers = true;
    }

    void OnSliderValueChanged(float value)
    {
        valueText.text = ((int)value).ToString();
    }
    public void ReadyUnReady()
    {
        localIsReady = !localIsReady;
        btnChangeColor();
        // Send intent to server
        handleConnectedClients.SendReadyStatusServerRpc(localIsReady);
        // Optional: optimistic UI (feels instant)
        UpdateReadyButtonUI(localIsReady);
    }
    public void InitializeClientReadyStatus()
    {
        ulong serverid = handleConnectedClients.getServerId();

        // Copy keys first
        List<ulong> keys = new List<ulong>(ClientReadyStatus.Keys);

        foreach (ulong key in keys)
        {
            ClientReadyStatus[key] = key == serverid;
        }

        handleConnectedClients.SendLobbySnapshotToClient();
    }

    public void SetMaxSliderValue(int count)
    {
        if (count >= 13)
            count = 13;
        slider.maxValue = count;
    }
    void btnChangeColor()
    {
        if (localIsReady)
            ReadyBtn.image.color = Color.white;
        else
            ReadyBtn.image.color = Color.red;
    }
    void UpdateReadyButtonUI(bool isReady)
    {
        var text = ReadyBtn.GetComponentInChildren<TextMeshProUGUI>();
        text.text = isReady ? "Ready" : "Not Ready";
    }

    public void SetGameMode(Button gameModeBtn)
    {

        if (lastGameMode != null)
            lastGameMode.interactable = true;
        gameModeBtn.interactable = false;
        lastGameMode = gameModeBtn;
        string gameModeText = gameModeBtn.GetComponentInChildren<TextMeshProUGUI>().text;
        gameManager.SetGameMode(gameModeText);
    }
    public void StartMatch()
    {
        bool canStart = true;
        foreach (var key in ClientReadyStatus)
        {
            if (key.Value != true)
            {
                canStart = false;
                break;
            }
        }
        if (canStart)
        {
            CardDistributer cardDistributer = FindAnyObjectByType<CardDistributer>();
            cardDistributer.cardsPerPlayer = (int)slider.value;
            handleConnectedClients.StartGameByLobby();
        }
    }
    public void CopyJoinCode()
    {
        GUIUtility.systemCopyBuffer = MyUtilities.LobbyCode;
    }

    public void KickClient(ulong clientID)
    {
        handleConnectedClients.KickClient(clientID);
    }
    public void ExitLobby()
    {
        if (!IsClient)
        {
            handleConnectedClients.SetgameStarted(false);
            handleConnectedClients.ExitClientRpc();
            HostSingleton.Instance.GameManager.ExitHostRelay();
        }
        else
        {
            ClientSingleton.Instance.GameManager.ExitClientRelay();
            SceneManager.LoadScene("Menu");
        }

    }

}