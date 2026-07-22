using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

public class HandleConnectedClients : NetworkBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] public LobbyManager lobbyManager;
    [SerializeField] int clientCount;
    private List<ulong> connectedClientIds = new();
    private bool gameStarted = false;
    public NetworkVariable<int> connectedClientCount = new();
    public ulong[] rotated;
    [SerializeField] public TMP_InputField countTextField;

    [SerializeField] GameObject playerListPanel;
    [SerializeField] GameObject playerListItemPrefab;
    Dictionary<ulong, GameObject> playerItems = new();
    public Dictionary<ulong, string> ClientNames = new Dictionary<ulong, string>();
    public int playerindex = 0;

    private void OnEnable()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            lobbyManager.IsClient = true;
            lobbyManager.localIsReady = false;
            Destroy(lobbyManager.StartBtn.gameObject);
            Destroy(lobbyManager.slider.gameObject);
        }
        else
        {

            Destroy(lobbyManager.ReadyBtn.gameObject);
        }
        // Subscribe to callbacks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        // Add host to list on start
        if (NetworkManager.Singleton.IsHost)
        {
            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            if (!connectedClientIds.Contains(hostClientId))
            {
                connectedClientIds.Add(hostClientId);
                connectedClientCount.Value = connectedClientIds.Count;
                ClientNames[hostClientId] = MyPlayerDataStatic.playerName;
                lobbyManager.ClientReadyStatus[hostClientId] = true;
                CreatePlayerItem(hostClientId, MyPlayerDataStatic.playerName, true);
                //                Debug.Log($"[Host] Added self on enable (ClientID: {hostClientId}). Total: {connectedClientIds.Count}");
            }
        }
    }
    public void SetgameStarted(bool start)
    {
        gameStarted = start;
    }
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        // Start game only once when enough players are present
        if (!gameStarted) // change to ==4 if you want strict 4-player start
        {
            if (!connectedClientIds.Contains(clientId))
            {
                connectedClientIds.Add(clientId);
                connectedClientCount.Value = connectedClientIds.Count;
                lobbyManager.SetMaxSliderValue(52 / connectedClientIds.Count);
                SendLobbySnapshotToClient(clientId);
                Debug.Log($"Client {clientId} connected. Total: {connectedClientIds.Count}");
            }
        }
    }
    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (connectedClientIds.Contains(clientId))
        {
            RemovePlayerFromLobby(clientId);
            connectedClientIds.Remove(clientId);
            connectedClientCount.Value = connectedClientIds.Count;
            if (connectedClientCount.Value <= 1 && gameStarted)
            {
                //SceneManager.LoadScene("Menu");
                ulong lastPlayerId = connectedClientIds[0];
                if (gameStarted)
                {
                    gameManager.ShowPlayerWon(++MyUtilities.rank, lastPlayerId);
                    MyUtilities.rank = 0;
                    gameManager.ShowSummary();
                    gameManager.StopGameClientRpc();
                }

            }
            Debug.Log($"❌ Client {clientId} disconnected. Remaining: {connectedClientIds.Count}");

            if (gameStarted)
            {
                CardDistributer cardDistributer = FindAnyObjectByType<CardDistributer>();
                cardDistributer.playerCardMap.Remove(clientId);
                if (gameManager.GetCurrentTurnClientId() == clientId)
                {
                    gameManager.EndTurn();
                }
                TableManager tableManager = FindAnyObjectByType<TableManager>();
                if (tableManager.HasEveryPlayerPlayedAtLeastOneCard())
                {
                    tableManager.DiscardCardsOnTable();
                }

                // Continue game with remaining players
                Debug.Log("⚠️ Player left, continuing with remaining players...");
                SendPlayerOrderClientRpc(connectedClientIds.ToArray());
            }
        }
    }

    public void StartGameByLobby()
    {
        if (connectedClientCount.Value < 2)
            return;
        ActivateLayoutClientRpc(connectedClientCount.Value);
        //gameManager.SetGameMode();
        InitializeCardsClientRpc();
        disbalelobbyClientRpc();

        StartGameWithClients(connectedClientIds);
        CallInitializeNamesOnClients();

    }
    [ClientRpc]
    void InitializeCardsClientRpc()
    {
        MyUtilities.InitializeCards();
    }
    [ClientRpc]
    void ActivateLayoutClientRpc(int count)
    {
        UIManager uIManager = FindAnyObjectByType<UIManager>();
        uIManager.ConfigureTableLayout(count);
    }
    void CallInitializeNamesOnClients()
    {
        if (!IsServer) return;

        ulong[] ids = ClientNames.Keys.ToArray();

        FixedString64Bytes[] names = ids
            .Select(id => new FixedString64Bytes(ClientNames[id]))
            .ToArray();

        InitializeNameAndAvatarClientRpc(ids, names);
    }
    public void SendLobbySnapshotToClient(ulong? targetClientId = null)
    {
        if (!IsServer) return;

        ulong[] ids = ClientNames.Keys.ToArray();

        FixedString64Bytes[] names = ids
            .Select(id => new FixedString64Bytes(ClientNames[id]))
            .ToArray();

        bool[] ready = ids
            .Select(id => lobbyManager.ClientReadyStatus[id])
            .ToArray();

        if (targetClientId.HasValue)
        {
            // Send to ONE specific client
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { targetClientId.Value }
                }
            };

            ReceiveLobbySnapshotClientRpc(ids, names, ready, true, rpcParams);
        }
        else
        {
            // Send to ALL clients
            ReceiveLobbySnapshotClientRpc(ids, names, ready, false);
        }
    }



    // ======================================================
    // CLIENT: receive snapshot and build UI
    // ======================================================
    [ClientRpc]
    void ReceiveLobbySnapshotClientRpc(
     ulong[] clientIds,
     FixedString64Bytes[] clientNames,
     bool[] readyStates,
     bool isTargetedSnapshot,
     ClientRpcParams rpcParams = default)
    {
        foreach (var go in playerItems.Values)
            Destroy(go);

        playerItems.Clear();
        playerindex = 0; // 🔴 VERY IMPORTANT

        for (int i = 0; i < clientIds.Length; i++)
        {
            CreatePlayerItem(
                clientIds[i],
                clientNames[i].ToString(),
                readyStates[i]
            );
        }

        if (isTargetedSnapshot)
        {
            SendClientNameServerRpc(MyPlayerDataStatic.playerName);
        }
    }



    // ======================================================
    // CLIENT → SERVER: send name
    // ======================================================
    [ServerRpc(RequireOwnership = false)]
    void SendClientNameServerRpc(string clientName, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Prevent duplicates (important!)
        if (ClientNames.ContainsKey(senderId))
            return;

        ClientNames[senderId] = clientName;
        lobbyManager.ClientReadyStatus[senderId] = false;

        // Broadcast ONLY the new player
        AddSinglePlayerClientRpc(senderId, clientName, false);
    }

    // ======================================================
    // SERVER → CLIENT: add ONE new player
    // ======================================================
    [ClientRpc]
    void AddSinglePlayerClientRpc(ulong clientId, string clientName, bool ready)
    {
        if (playerItems.ContainsKey(clientId))
            return;

        CreatePlayerItem(clientId, clientName, ready);
    }
    public void RemovePlayerFromLobby(ulong clientId)
    {
        if (!IsServer) return;

        // Remove from server-side data
        if (ClientNames.ContainsKey(clientId))
            ClientNames.Remove(clientId);

        if (lobbyManager.ClientReadyStatus.ContainsKey(clientId))
            lobbyManager.ClientReadyStatus.Remove(clientId);

        // Notify all clients to rebuild UI
        RebuildLobbyClientRpc();
    }
    [ClientRpc]
    void RebuildLobbyClientRpc()
    {
        // Destroy all UI objects
        foreach (var go in playerItems.Values)
            Destroy(go);

        playerItems.Clear();
        playerindex = 0;

        // Ask server for fresh snapshot
        RequestLobbySnapshotServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    void RequestLobbySnapshotServerRpc(ServerRpcParams rpcParams = default)
    {
        SendLobbySnapshotToClient(rpcParams.Receive.SenderClientId);
    }

    // ======================================================
    // CLIENT → SERVER: ready toggle
    // ======================================================
    [ServerRpc(RequireOwnership = false)]
    public void SendReadyStatusServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        lobbyManager.ClientReadyStatus[senderId] = isReady;

        UpdateReadyStatusClientRpc(senderId, isReady);
    }

    // ======================================================
    // SERVER → CLIENT: update ready UI
    // ======================================================
    [ClientRpc]
    void UpdateReadyStatusClientRpc(ulong clientId, bool isReady)
    {
        if (!playerItems.TryGetValue(clientId, out GameObject go))
            return;
        PlayerListItem playerListItem = go.GetComponent<PlayerListItem>();
        //char index = playerListItem.name.text.ElementAt(0);
        //  $"{index}.<space=3px>{go.name}";
        // playerListItem.SetName($"{index}.<space=3px>{go.name}");
        playerListItem.ReadyUnready(isReady ? "Ready" : "Not Ready");
        playerListItem.SetColor(isReady);
    }
    public ulong getServerId()
    {
        return OwnerClientId;
    }
    // ======================================================
    // CLIENT: create UI item
    // ======================================================
    void CreatePlayerItem(ulong clientId, string playerName, bool isReady)
    {
        GameObject go = Instantiate(playerListItemPrefab, playerListPanel.transform);
        playerItems[clientId] = go;
        PlayerListItem playerListItem = go.GetComponent<PlayerListItem>();
        playerListItem.clientID = clientId;
        if (clientId == 0)
        {
            playerListItem.DestroyKickBtn();
        }
        go.transform.localPosition =
            new Vector3(-970, -530 - (70 * playerItems.Count), 0);

        TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
        playerindex++;
        playerListItem.SetName($"{playerindex}.<space=3px>{playerName}");
        playerListItem.ReadyUnready(isReady ? "Ready" : "Not Ready");
        playerListItem.SetColor(isReady);
        // Store name locally for ready updates
        go.name = playerName;
    }
    [ClientRpc]
    void disbalelobbyClientRpc()
    {
        lobbyManager.gameObject.SetActive(false);
    }
    void SendInitializeNamesToClients()
    {
        if (!IsServer) return;

        ulong[] ids = ClientNames.Keys.ToArray();

        FixedString64Bytes[] names = ids
            .Select(id => new FixedString64Bytes(ClientNames[id]))
            .ToArray();

        InitializeNameAndAvatarClientRpc(ids, names);
    }
    [ClientRpc]
    public void InitializeNameAndAvatarClientRpc(
    ulong[] clientIds,
    FixedString64Bytes[] clientNames)
    {
        var player = NetworkManager.Singleton
            .SpawnManager
            .GetLocalPlayerObject()
            ?.GetComponent<PlayerBehavior>();

        if (player == null)
            return;

        // Build a local dictionary from params
        Dictionary<ulong, string> nameDict = new Dictionary<ulong, string>();

        for (int i = 0; i < clientIds.Length; i++)
        {
            nameDict[clientIds[i]] = clientNames[i].ToString();
        }

        // 🔥 Pass data directly
        player.InitializePlayerNames(nameDict);
    }

    public void ReturnToLobby()
    {
        lobbyManager.gameObject.SetActive(true);
        lobbyManager.InitializeClientReadyStatus();
    }
    private void StartGameWithClients(List<ulong> clientIds)
    {
        gameStarted = true;
        lobbyManager.gameObject.SetActive(false);
        gameManager.StartGame(clientIds);

        // Assign avatar indexes
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[connectedClientIds[i]].PlayerObject;
            PlayerBehavior playerScript = playerObj.GetComponent<PlayerBehavior>();
            playerScript.avatarIndex.Value = i;
        }

        SendPlayerOrderClientRpc(connectedClientIds.ToArray());
    }

    public int GetConnectedClientCount()
    {
        return connectedClientCount.Value;
    }

    [ClientRpc]
    public void SendPlayerOrderClientRpc(ulong[] orderedClientIds)
    {
        var myId = NetworkManager.Singleton.LocalClientId;
        int myIndex = Array.IndexOf(orderedClientIds, myId);

        if (myIndex == -1)
        {
            Debug.LogWarning("⚠️ Local client not in ordered list (probably disconnected).");
            return;
        }

        // Rotate so local player is always at index 0
        rotated = new ulong[orderedClientIds.Length];

        for (int i = 0; i < orderedClientIds.Length; i++)
            rotated[i] = orderedClientIds[(myIndex + i) % orderedClientIds.Length];

        // ⭐ Send rotated order to each player's script
        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerBehavior>();
        if (localPlayer != null)
        {
            localPlayer.rotated = (ulong[])rotated.Clone();
            foreach (ulong cid in rotated)
            {
                Debug.Log(cid);
            }
        }

        // Update avatar positions
        var allPlayers = FindObjectsOfType<PlayerBehavior>();
        foreach (var player in allPlayers)
        {
            int displayIndex = Array.IndexOf(rotated, player.OwnerClientId);
            if (displayIndex >= 0)
                player.MoveAvatarToSlot(displayIndex);
        }
    }
    [ClientRpc]
    public void ExitClientRpc()
    {
        if (IsClient)
            ClientSingleton.Instance.GameManager.ExitClientRelay();
        SceneManager.LoadScene("Menu");
    }
    public void KickClient(ulong clientID)
    {
        if (!IsServer)
        {
            return;
        }
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientID }
            }
        };

        KickClientClientRpc(rpcParams);
    }
    [ClientRpc]
    void KickClientClientRpc(ClientRpcParams rpcParams = default)
    {
        lobbyManager.ExitLobby();
    }

}
