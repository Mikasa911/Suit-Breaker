using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public List<Card> cardList;

    [SerializeField] GameObject cardPrefab;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] public Button dealButton;
    [SerializeField] public UIManager uIManager;
    [SerializeField] Canvas gameSummary;
    bool previousButtonState;

    [Header("Timing")]
    private float turnTimeLimit = 2500f;

    private float timer = 0f;
    public int selectedCardId = -1;
    public GameObject selectedCardObject;

    public CardDistributer cardDistributer;
    private TableManager tableManager;
    [SerializeField] private FadeSpriteSwap SpriteChanger;
    private NetworkList<ulong> syncedClientIDs;

    private NetworkVariable<int> currentTurn = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Coroutine timerCoroutine;
    public NetworkList<ulong> getSyncedClients()
    {
        return syncedClientIDs;
    }
    public int GetCurrentTurn()
    {
        return currentTurn.Value;
    }
    int randomCardId;
    [SerializeField] int totalCoinsInMatch;
    private bool timeoutHandled = false;
    bool gameStarted = false;
    GameMode gameMode;
    [SerializeField] ToggleGroup toggleGroup;
    List<PlayerListData> playerListData = new();
    List<GameObject> PlayerItemObjects = new();
    [SerializeField] GameObject playerListDataPrefab;
    [SerializeField] GameObject playerTableObj;
    [SerializeField] HandleConnectedClients handleConnectedClients;
    public ulong LocalClientId => NetworkManager.Singleton.LocalClientId;

    public bool IsMyTurn =>
        currentTurn.Value >= 0 &&
        currentTurn.Value < syncedClientIDs.Count &&
        LocalClientId == syncedClientIDs[currentTurn.Value];

    private void Awake()
    {
        tableManager = GetComponent<TableManager>();
        Instance = this;

        syncedClientIDs = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Host only

        MyUtilities.ShuffleSeed = MyUtilities.UseRandomSeed
             ? new System.Random().Next(int.MinValue, int.MaxValue)
             : 10;

        Debug.Log($"Host decided seed: {MyUtilities.ShuffleSeed}");
    }

    public void DeativateDealButton()
    {
        previousButtonState = dealButton.interactable;
        dealButton.interactable = false;
    }
    public void ActivateDealButton()
    {
        dealButton.interactable = previousButtonState;
    }

    [ClientRpc]
    private void SubmitSeedClientRpc(int seed)
    {
        MyUtilities.ShuffleSeed = seed;
        MyUtilities.Shuffle(MyUtilities.cardsDatabase);
        MyUtilities.Shuffle(MyUtilities.Abilities);
        Debug.Log($"Shuffle Seed received: {seed}");
    }

    [ClientRpc]
    public void ChangeBackGroundClientRpc(int currentSuit)
    {
        SpriteChanger.ChangeSprite(currentSuit);
    }
    public void InitializeCardOnCurrentTurnClient(int cardsOnTable, int suit, ulong targetClientId)
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { targetClientId }
            }
        };

        //InitilaizePlayableCardsClientRpc(cardsOnTable, suit, rpcParams);
        InitilaizePlayableCardsClientRpc(cardsOnTable, suit);
    }

    [ClientRpc]
    public void DisplayCardsClientRpc()
    {
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        player.DisplayCards();
    }

    [ClientRpc]
    public void InitilaizePlayableCardsClientRpc(int cardsOnTable, int suit, ClientRpcParams rpcParams = default)
    {
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        player.InitializePlayableCards(cardsOnTable, suit);
    }
    private void OnEnable()
    {
        ClickableCardHandler.OnCardSelected += HandleCardSelection;
    }


    private void OnDisable()
    {
        ClickableCardHandler.OnCardSelected -= HandleCardSelection;
    }

    private void Start()
    {
        dealButton.onClick.AddListener(OnDealPressed);
        dealButton.interactable = false;
    }

    private void Update()
    {
        if (IsServer && currentTurn.Value != -1 && gameStarted)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f && !timeoutHandled)
            {
                timeoutHandled = true;   // 🔒 lock
                timer = 0f;
                HandleTurnTimeout();
            }
        }

        if (IsClient && dealButton != null)
        {
            dealButton.interactable = IsMyTurn && selectedCardId != -1;
        }
    }
    private void StartTurn()
    {
        timer = turnTimeLimit;
        timeoutHandled = false;   // 🔓 unlock for next turn
    }


    public void SetCurrentTurn(ulong clientId)
    {
        if (!IsServer) return;
        if (cardDistributer.playerCardMap.TryGetValue(clientId, out var cards) && cards.Count == 0)
        {
            EndTurn();
            return;
        }

        int index = syncedClientIDs.IndexOf(clientId);
        if (index != -1)
        {
            currentTurn.Value = index;
            timer = turnTimeLimit;
            UpdateTurnUIClientRpc(currentTurn.Value, timer);
            // Debug.Log("Client" + (index + 1));
        }
    }

    public ulong GetCurrentTurnClientId()
    {
        if (currentTurn.Value >= 0 && currentTurn.Value < syncedClientIDs.Count)
        {
            return syncedClientIDs[currentTurn.Value];
        }

        Debug.LogWarning("Invalid currentTurn index.");
        return ulong.MaxValue; // Or any default/fallback value
    }


    private void HandleCardSelection(int cardId, bool isUnplayable)
    {
        selectedCardId = cardId;
        if (!IsMyTurn || cardId == -1)
        {
            dealButton.interactable = false;
            return;
        }

        dealButton.interactable = true;
    }
    public void ShowPlayerWon(int rank, ulong targetClientId)
    {
        GetComponent<HandleConnectedClients>().ClientNames.TryGetValue(targetClientId, out string name);
        playerListData.Add(new PlayerListData
        {
            name = name,
            rank = rank,
            coins = totalCoinsInMatch / rank,
            diamonds = 0
        });
        int coinsToSend = totalCoinsInMatch / rank;
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { targetClientId }
            }
        };

        ShowPlayerWonClientRpc(rank, coinsToSend, rpcParams);
    }

    [ClientRpc]
    public void ShowPlayerWonClientRpc(int rank, int coins, ClientRpcParams rpcParams = default)
    {
        MyPlayerDataStatic.coins += coins;
        Debug.Log(coins);
        uIManager.WinText.text = "You Placed : " + rank + "and Won " + coins + " Coins";
        //uIManager.GameUI.SetActive(false);

    }


    private void OnDealPressed()
    {
        if (!IsMyTurn || selectedCardId == -1) return;

        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        var cardHandler = player.GetCardHandler(selectedCardId);
        if (cardHandler == null || cardHandler.cardIsUnplayable)
            return;
        player.playSFX.Deal();
        player.SetSelectedCardId(selectedCardId);
        player?.DestroySelectedCard();
        //player.DisplayCards();
        // Debug.Log($"[Client {LocalClientId}]  Sending selectedCardId: {selectedCardId} to server.");

        SubmitCardServerRpc(selectedCardId);
        selectedCardId = -1;
        // Destroy the visual card locally

        dealButton.interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EraseCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!cardDistributer.playerCardMap.TryGetValue(clientId, out var cards))
            return;

        Card cardToRemove = cards.Find(c => c.cardId == cardId);

        cards.Remove(cardToRemove);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        //Debug.Log($"[Server] Received card {cardId} from Client {senderClientId}");

        // Lookup card details from database
        Card card = MyUtilities.GetCardById(cardId);
        if (card != null)
        {
            // Notify all clients (except sender) about the played card
            var clientParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                        .Where(id => id != senderClientId)
                        .ToList()
                }
            };

            BroadcastCardPlayClientRpc(senderClientId, card.cardId);
        }

        ProcessCardFromClient(senderClientId, cardId);
    }
    [ClientRpc]
    public void ClearTableClientRpc(bool Hit = false, ulong targetClientId = ulong.MaxValue)
    {

        foreach (var cardObj in tableManager.playedCardObjects)
        {
            if (cardObj != null)
                Destroy(cardObj);
        }
        tableManager.playedCardObjects.Clear();
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        player.ClearTableVisual(Hit, targetClientId);
        player.DisplayCards();

    }
    [ClientRpc]
    private void BroadcastCardPlayClientRpc(ulong clientId, int cardID, ClientRpcParams clientRpcParams = default)
    {
        PlayerBehavior[] allPlayers = FindObjectsOfType<PlayerBehavior>();
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        for (int i = 0; i < player.rotated.Length; i++)
        {
            if (clientId == player.rotated[i])
            {
                Debug.Log(clientId + "  " + player.rotated[i] + "  CardID " + cardID);
                player.placeCardOnTable(i, MyUtilities.GetCardById(cardID), clientId);
                break;
            }
        }
    }



    [ClientRpc]
    public void StopGameClientRpc()
    {
        gameStarted = false;
        turnTimeLimit *= 100;
        handleConnectedClients.playerindex = 0;
        //handleConnectedClients.lobbyManager.gameObject.SetActive(true);
        // NetworkManager.Singleton.Shutdown();
        // Invoke("ReloadGame", 2f);

    }


    public void ShowSummary()
    {
        playerListData.Sort((a, b) => a.rank.CompareTo(b.rank));
        SendPlayerDataClientRpc(playerListData.ToArray());
    }

    [ClientRpc]
    void SendPlayerDataClientRpc(PlayerListData[] data)
    {
        playerListData.Clear();
        for (int i = 0; i < data.Length; i++)
        {
            playerListData.Add(data[i]);
        }
        ActivateGameSummary();
    }
    void ActivateGameSummary()
    {
        gameSummary.gameObject.SetActive(true);

        for (int i = 0; i < playerListData.Count; i++)
        {
            GameObject g = Instantiate(playerListDataPrefab, playerTableObj.transform);

            RectTransform rt = g.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 210 - (70 * i));

            PlayerRowUI p = g.GetComponent<PlayerRowUI>();
            p.SetData(playerListData[i]);
            PlayerItemObjects.Add(g);
        }

    }
    public void SetGameMode(string gameModeText)
    {
        if (gameModeText == "Special")
        {
            gameMode = GameMode.Special;
        }
        else
        {
            gameMode = GameMode.Classic;
        }
        /*
        Toggle activeToggle = toggleGroup.GetFirstActiveToggle();
        if (activeToggle.GetComponentInChildren<TextMeshProUGUI>().text == "Special")
        {
            gameMode = GameMode.Special;
        }
        else
        {
            gameMode = GameMode.Classic;
        }*/

    }
    public void StartGame(List<ulong> clientIds)
    {
        DisableCanvasClientRpc();
        playerListData.Clear();

        gameStarted = true;
        turnTimeLimit /= 100;
        cardDistributer = FindAnyObjectByType<CardDistributer>();

        SubmitSeedClientRpc(MyUtilities.ShuffleSeed);

        cardList = MyUtilities.cardsDatabase;
        cardDistributer.DistributeCards(cardList, clientIds);
        //if (HostSingleton.Instance.GameManager.GetGameMode() == GameMode.Special)

        if (gameMode == GameMode.Special)
        {
            EnableAbilityButtonsClientRpc();
            cardDistributer.DistributeAbilities(MyUtilities.Abilities, clientIds);
        }
        else
            DisableAbilityButtonsClientRpc();
        syncedClientIDs.Clear();
        foreach (var id in clientIds)
            syncedClientIDs.Add(id);

        currentTurn.Value = 0;
        timer = turnTimeLimit;
        UpdateTurnUIClientRpc(currentTurn.Value, timer);
    }

    [ClientRpc]
    private void DisableCanvasClientRpc()
    {
        foreach (GameObject g in PlayerItemObjects)
        {
            Destroy(g);
        }
        uIManager.WinText.text = "";
        gameSummary.gameObject.SetActive(false);
        handleConnectedClients.lobbyManager.gameObject.SetActive(false);
    }
    [ClientRpc]
    void EnableAbilityButtonsClientRpc()
    {
        Button[] AbilityButtons = FindObjectOfType<UIManager>().abilityButtons;
        foreach (Button b in AbilityButtons)

            b.gameObject.SetActive(true);
    }

    [ClientRpc]
    void DisableAbilityButtonsClientRpc()
    {
        Button[] AbilityButtons = FindObjectOfType<UIManager>().abilityButtons;
        foreach (Button b in AbilityButtons)

            b.gameObject.SetActive(false);
    }
    public ulong GetSyncedClientId()
    {
        return syncedClientIDs[currentTurn.Value];
    }
    public void EndTurn(bool isReverse = false)
    {

        int direction = isReverse ? -1 : 1;
        int count = syncedClientIDs.Count;

        // Move turn based on direction
        currentTurn.Value = (currentTurn.Value + direction + count) % count;

        // int safety = 0;
        int maxPlayers = count;

        while (!cardDistributer.playerCardMap.TryGetValue(syncedClientIDs[currentTurn.Value], out var cards) || cards.Count == 0)
        {
            currentTurn.Value = (currentTurn.Value + direction + maxPlayers) % maxPlayers;

            /*if (++safety >= maxPlayers)
            {
                List<ulong> clientIds = new List<ulong>();

                foreach (ulong id in syncedClientIDs)
                {
                    clientIds.Add(id);
                }

               // StartGame(clientIds);


                return; // no valid players left
            }*/
        }
        timer = turnTimeLimit;
        timeoutHandled = false;
        UpdateTurnUIClientRpc(currentTurn.Value, timer);
    }

    [ClientRpc]
    private void NotifyTurnTimeoutClientRpc(ClientRpcParams clientRpcParams = default)
    {
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        if (player.MyDeck.Count == 0)
        {
            EndTurn(tableManager.getReverse());
            return;
        }
        int randomIndex = UnityEngine.Random.Range(0, player.playableCardsID.Count);
        randomCardId = player.playableCardsID[randomIndex];
        if (selectedCardId == -1)
        {
            selectedCardId = randomCardId;

        }
        player.SetSelectedCardId(selectedCardId);
        OnDealPressed();
    }

    public void ReturnToLobby()
    {
        gameSummary.gameObject.SetActive(false);
    }
    private void HandleTurnTimeout()
    {
        ulong clientId = syncedClientIDs[currentTurn.Value];
        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

        NotifyTurnTimeoutClientRpc(clientParams);

        /*
                if (cardDistributer == null)
                    cardDistributer = FindObjectOfType<CardDistributer>();

                var cardList = cardDistributer.GetCardsForPlayer(clientId);

                if (cardList == null || cardList.Count == 0)
                {
                    Debug.LogWarning("[Server] No cards found for Client " + clientId + " during timeout.");
                    EndTurn(tableManager.getReverse());
                    return;
                }

                // int randomIndex = UnityEngine.Random.Range(0, cardList.Count);
                // int cardId = cardList[randomIndex].cardId;
                int cardId = randomCardId;

                Debug.Log("[Server] Timeout: Sending random card " + cardId + " for Client " + clientId);

                // Send cardId to the specific client
                var clientParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId }
                    }
                };
                BroadcastCardPlayClientRpc(clientId, cardId);
                // Process it normally
                ProcessCardFromClient(clientId, cardId);*/
    }



    private void ProcessCardFromClient(ulong clientId, int cardId)
    {
        Debug.Log("[Server] Processing card " + cardId + " from Client " + clientId);

        // TODO: Handle card logic (e.g., remove from deck, check for win, etc.)
        tableManager.ProcessCardDeal(cardId, clientId);
        // EndTurn(); // Move to next player
    }

   [ClientRpc]
private void UpdateTurnUIClientRpc(int turnIndex, float timeLeft)
{
    if (timerCoroutine != null)
        StopCoroutine(timerCoroutine);

    timerCoroutine = StartCoroutine(CountdownTimerUI(turnIndex, timeLeft));

    ulong activeClientId = syncedClientIDs[turnIndex];

    var player = NetworkManager.Singleton
        .SpawnManager
        .GetLocalPlayerObject()
        ?.GetComponent<PlayerBehavior>();

    if (player == null)
        return;

    // 🔥 Disable all indicators first
    foreach (var indicator in uIManager.turnIndicators)
        indicator.gameObject.SetActive(false);

    // 🔥 Find which visual slot matches the active client
    for (int i = 0; i < player.rotated.Length; i++)
    {
        if (player.rotated[i] == activeClientId)
        {
            uIManager.turnIndicators[i].StartTimer();
            break;
        }
    }
}
    [ClientRpc]
    public void SyncDeckClientRpc(int[] cardIds, ClientRpcParams clientRpcParams = default)
    {
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();
        if (player != null && player.IsOwner)
        {
            player.SyncDeckFromServer(new List<int>(cardIds));
        }
    }

    private IEnumerator CountdownTimerUI(int turnIndex, float time)
    {
        while (time > 0f)
        {
            bool myTurn = turnIndex < syncedClientIDs.Count && LocalClientId == syncedClientIDs[turnIndex];
            turnText.text = myTurn ? $"🃏 Your Turn ({Mathf.CeilToInt(time)}s)" : $" Client {turnIndex + 1}'s Turn ({Mathf.CeilToInt(time)}s)";
            yield return new WaitForSeconds(1f);
            time -= 1f;
        }
    }
}
