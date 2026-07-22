using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerBehavior : NetworkBehaviour
{
    public NetworkVariable<int> avatarIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    public static Dictionary<ulong, PlayerBehavior> Players = new();
    private GameManager gameManager;
    private TableManager tableManager;
    [Header("Audio Settings")]
    [SerializeField] public PlaySFX playSFX;

    [Header("Avatar Settings")]
    Sprite avatar;
    [SerializeField] private Vector2 myAvatarSize = new Vector2(50f, 50f);
    [SerializeField] private Vector2 otherAvatarSize = new Vector2(30f, 30f);

    [Header("Card Settings")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform myDeckPivotPoint;
    [SerializeField] private float cardSpacing = 60f;
    float widthRatio;
    float heightRatio;
    public ulong[] rotated;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private UIInteractionBlocker interactionBlocker;
    [SerializeField] GameObject cardStash;
    [SerializeField] float animationDelay = 2f;
    [SerializeField] public List<Card> MyDeck = new();
    List<string> myAbilties = new();
    private List<GameObject> spawnedCards = new();
    public List<GameObject> playedCardObjects = new();
    public List<int> playableCardsID;
    int maxGapCardCount = 8;
    int currentSuit;
    int cardsOnTableCount;
    [SerializeField] float REF_SCALE = 0.48f;
    [SerializeField] float spaceScale;
    float NATIVE_WIDTH = 432f;
    float NATIVE_HEIGHT = 576f;

    float REF_WIDTH = 1920f;
    float REF_HEIGHT = 1080f;
    private readonly Dictionary<string, int> suitPriority = new()
    {
        { "Club", 0 },
        { "Diamond", 1 },
        { "Heart", 2 },
        { "Spade", 3 }
    };
    [SerializeField] public Button[] AbilityButtons;
    public NetworkVariable<int> selectedCardId = new NetworkVariable<int>(
     -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
 );
    private GameObject selectedCardObject;
    private int runningCardAnimations = 0;
    string myName;
    private Coroutine clearRoutine;


    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        tableManager = FindObjectOfType<TableManager>();


    }

    public override void OnNetworkSpawn()
    {
       // MyUtilities.InitializeCards();
        Players[OwnerClientId] = this;


        if (IsLocalPlayer)
            StartCoroutine(AssignAbilityButtons());

        avatarIndex.OnValueChanged += OnAvatarIndexChanged;

        if (avatarIndex.Value >= 0)
            SetAvatar(avatarIndex.Value);
    }
    public override void OnNetworkDespawn()
    {
        // Remove from static player list
        if (Players.ContainsKey(OwnerClientId))
            Players.Remove(OwnerClientId);

        // Unsubscribe from NetworkVariable events
        avatarIndex.OnValueChanged -= OnAvatarIndexChanged;

        // Reset local-player-only data
        if (IsLocalPlayer)
        {
            ResetAbilityButtons();
        }
    }
    void ResetAbilityButtons()
    {
        if (AbilityButtons == null)
            return;

        foreach (Button b in AbilityButtons)
        {
            b.onClick.RemoveAllListeners();
        }

        AbilityButtons = null;

        Debug.Log("Ability buttons reset after disconnect");
    }

    IEnumerator AssignAbilityButtons()
    {
        UIManager uiManager = null;

        // Wait until UIManager is found
        while (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            yield return null;
        }

        AbilityButtons = uiManager.abilityButtons;

        foreach (Button b in AbilityButtons)
        {
            b.onClick.AddListener(() => PowerActivate(b));
        }
        Debug.Log("Ability buttons assigned to local player");
    }

    private void OnAvatarIndexChanged(int oldVal, int newVal)
    {
        SetAvatar(newVal);
    }
    public void InitializePlayableCards(int cardsOnTable, int suit)
    {

        playableCardsID.Clear();
        GameManager game = FindAnyObjectByType<GameManager>();
        int nonMatchingSuitCards = 0;
        //Debug.Log(game.GetCurrentTurn());

        if (game != null)
        {
            if (spawnedCards.Count != 0)
            {
                foreach (GameObject g in spawnedCards)
                {

                    var s = g.GetComponent<ClickableCardHandler>();
                    if (s.cardSuit == suit || cardsOnTable == 0 || MyUtilities.GetCardById(s.cardId).Ability == "Wildcard")
                    {
                        s.MakeCardPlayable();
                        playableCardsID.Add(s.cardId);

                    }
                    else
                    {

                        //s.ChangeState(0);
                        s.MakeCardUnplayable();
                        nonMatchingSuitCards++;
                    }
                }
                if (nonMatchingSuitCards == spawnedCards.Count)
                {
                    foreach (GameObject g in spawnedCards)
                    {
                        var s = g.GetComponent<ClickableCardHandler>();
                        //s.ChangeState(1);
                        s.MakeCardPlayable();
                        if (!playableCardsID.Contains(s.cardId))
                            playableCardsID.Add(s.cardId);
                    }
                }
            }

        }
    }
    public ClickableCardHandler GetCardHandler(int cardId)
    {
        foreach (var g in spawnedCards)
        {
            var h = g.GetComponent<ClickableCardHandler>();
            if (h != null && h.cardId == cardId)
                return h;
        }
        return null;
    }
    void Update()
    {

    }

    private void SetAvatar(int index)
    {
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (index < 0 || index >= uiManager.avatars.Length) return;

        avatar = uiManager.avatars[index];

        Debug.Log($"[Client {OwnerClientId}] Avatar set to index {index}");
    }


    public void MoveAvatarToSlot(int slotIndex)
    {

        //avatar.rectTransform.position = uiManager.avatarSlots[slotIndex].position;
        //uiManager.avatarSlots[slotIndex].GetComponent<Image>().sprite = avatar.sprite;
    }
    void AdjustSize()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        /*cardWidth= (screenSize.x * 0.07f)*3/2;  // 8% of screen width
        cardHeight = ((screenSize.y * 0.14f)*2/3)*3/2;  // 20% of screen height*/
        // cardWidth = 228f;
        //cardHeight = 324f;
        float widthRatio = Screen.width / REF_WIDTH;
        float heightRatio = Screen.height / REF_HEIGHT;

    }


    public void AssignMyDeck(int[] cardIds)
    {
        MyDeck.Clear();

        foreach (int id in cardIds)
        {
            Card card = MyUtilities.GetCardById(id);
            if (card != null)
            {
                MyDeck.Add(card);
                Debug.Log(card.cardId);
            }
            else
            {
                
                Debug.LogError($"Card ID {id} not found in CardDatabase!");
            }
        }
        MyUtilities.cardsDatabase.Count();

        Debug.Log($"MyDeck now has {MyDeck.Count} cards.");
        DisplayCards();
    }
    public void PowerActivate(Button powerButton)
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        if (selectedCardId.Value == -1 || gameManager.selectedCardId == -1)
            return;

        gameManager.selectedCardId = -1;
        string abilitytext = powerButton.GetComponentInChildren<TextMeshProUGUI>().text;
        Card card = MyUtilities.GetCardById(selectedCardId.Value);
        MyDeck.Remove(card);
        powerButton.gameObject.SetActive(false);
        myAbilties.Remove(abilitytext);
        if (abilitytext == "Erase")
        {
            gameManager.EraseCardServerRpc(selectedCardId.Value);
            DestroySelectedCard();
            return;
        }
        if (abilitytext == "Twin")
        {
            MyDeck.Add(card);
            RequestCloneCardServerRpc(card.cardId);

            return;
        }
        SubmitAbilityServerRpc(card.cardId, MyUtilities.GetAbilityIndex(abilitytext));
        GetSuitAndCardsOnTableCountServerRpc();
    }
    [ServerRpc]
    void RequestCloneCardServerRpc(int sourceCardId, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Card source = MyUtilities.GetCardById(sourceCardId);
        if (source == null)
            return;

        Card clone = MyUtilities.CreateCard(
            source.cardValue,
            source.cardRank,
            source.suit,
            MyUtilities.GetAbilityIndex(source.Ability)
        );

        MyUtilities.cardsDatabase.Add(clone);

        // 4️⃣ Ensure sender has a deck
        if (!gameManager.cardDistributer.playerCardMap.TryGetValue(senderClientId, out var deck))
        {
            deck = new List<Card>();
            gameManager.cardDistributer.playerCardMap[senderClientId] = deck;
        }

        // 5️⃣ Add ONLY the clone to sender’s deck
        deck.Add(clone);
        ulong clientId = rpcParams.Receive.SenderClientId;

        GiveCardToClientClientRpc(
            clientId,
            clone.cardId,
            clone.cardValue,
            clone.cardRank,
            clone.suit,
            clone.Ability
        );
    }
    [ClientRpc]
    void GiveCardToClientClientRpc(
        ulong targetClient,
        int cardId,
        int value,
        int rank,
        string suit,
        string ability,
        ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient)
            return;

        Card clone = new Card
        {
            cardId = cardId,
            cardValue = value,
            cardRank = rank,
            suit = suit,
            Ability = ability,
            cardSprite = MyUtilities.GetSprite(value, suit)
        };

        MyUtilities.cardsDatabase.Add(clone);
        MyDeck.Add(clone);

        DisplayCards();
    }

    [ServerRpc]
    void SubmitAbilityServerRpc(int cardId, int abilityIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        int newCardId = MyUtilities.SetCardAbility(cardId, abilityIndex);
        // 4️⃣ Ensure sender has a deck
        if (!gameManager.cardDistributer.playerCardMap.TryGetValue(senderClientId, out var deck))
        {
            deck = new List<Card>();
            gameManager.cardDistributer.playerCardMap[senderClientId] = deck;
        }

        // 5️⃣ Add ONLY the clone to sender’s deck
        deck.Add(MyUtilities.GetCardById(newCardId));
        deck.Remove(MyUtilities.GetCardById(cardId));
        Card card = MyUtilities.GetCardById(newCardId);

        SyncAbilityClientRpc(
            senderClientId,     // 👈 IMPORTANT
            card.cardId,
            card.cardValue,
            card.cardRank,
            card.suit,
            card.Ability
        );
    }
    [ClientRpc]
    void SyncAbilityClientRpc(
    ulong ownerClientId,
    int cardId,
    int value,
    int rank,
    string suit,
    string ability)
    {
        // 1️⃣ Ensure card exists in database (ALL clients)
        if (MyUtilities.GetCardById(cardId) == null)
        {
            Card card = new Card
            {
                cardId = cardId,
                cardValue = value,
                cardRank = rank,
                suit = suit,
                Ability = ability,
                cardSprite = MyUtilities.GetSprite(value, suit)
            };

            MyUtilities.cardsDatabase.Add(card);
        }

        // 2️⃣ ONLY the requesting client updates deck + UI
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            Card ownedCard = MyUtilities.GetCardById(cardId);

            MyDeck.Add(ownedCard);
            DisplayCards(); // ✅ CORRECT PLACE
        }
    }


    [ServerRpc(RequireOwnership = false)]
    void GetSuitAndCardsOnTableCountServerRpc(ServerRpcParams rpcParams = default)
    {
        TableManager table = FindAnyObjectByType<TableManager>();

        if (table == null)
        {
            Debug.LogError("TableManager not found on server");
            return;
        }

        var targetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId }
            }
        };

        ReceiveSuitAndCardsOnTableCountClientRpc(table.cardsOnTable.Count, table.currentSuit, targetClient
        );
    }

    [ClientRpc]
    void ReceiveSuitAndCardsOnTableCountClientRpc(int count, int suit, ClientRpcParams clientRpcParams = default)
    {
        cardsOnTableCount = count;
        currentSuit = suit;

        Debug.Log("Suit and cardCount received on client : " + currentSuit + "  " + cardsOnTableCount);
        DisplayCards();
        InitializePlayableCards(cardsOnTableCount, currentSuit);
        selectedCardId.Value = -1;
    }

    [ServerRpc(RequireOwnership = false)]
    void SyncAbilityServerRpc(int cardId, int abilityIndex, ServerRpcParams rpcParams = default)
    {
        if (!MyUtilities.cardsDatabase.Exists(c => c.cardId == cardId))
        {

        }
        else
        {
            MyUtilities.SetCardAbility(cardId, abilityIndex);
        }
        // Apply on server 

        ulong senderId = rpcParams.Receive.SenderClientId;

        // Collect all clients except sender
        List<ulong> targetClients = new List<ulong>();
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != senderId)
                targetClients.Add(clientId);
        }

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targetClients.ToArray()
            }
        };

        // Send to everyone except caller
        SyncAbilityClientRpc(cardId, abilityIndex, clientRpcParams);
    }

    [ClientRpc]
    void SyncAbilityClientRpc(
     int cardId,
     int abilityIndex,
     ClientRpcParams clientRpcParams = default)
    {
        if (!MyUtilities.cardsDatabase.Exists(c => c.cardId == cardId))
            return;
        int newID = MyUtilities.SetCardAbility(cardId, abilityIndex);
        Debug.Log(MyUtilities.GetCardById(newID).Ability);
    }

    public void AssignMyAbilities(int[] Abilities)
    {
        myAbilties.Clear();
        foreach (int i in Abilities)
        {
            myAbilties.Add(MyUtilities.Abilities[i]);
        }
        AssignAbilitiesToButtons();
    }
    void AssignAbilitiesToButtons()
    {
        for (int i = 0; i < myAbilties.Count; i++)
        {
            AbilityButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = myAbilties[i];
            AbilityButtons[i].GetComponentInChildren<Image>().sprite = MyUtilities.GetAbilityButtonSprite(myAbilties[i]);
        }
    }
    public void DisplayCards()
    {
        AdjustSize();

        foreach (GameObject card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        // 🔹 Sort deck
        MyDeck.Sort((a, b) =>
        {
            int suitComp = suitPriority[a.suit].CompareTo(suitPriority[b.suit]);
            return suitComp == 0 ? b.cardRank.CompareTo(a.cardRank) : suitComp;
        });

        int cardCount = MyDeck.Count;
        if (cardCount == 0) return;

        // 🔹 CONFIG
        float maxTotalWidth = Screen.width * 1.6f;

        // 🔹 Base spacing (designer-controlled)
        float effectiveSpacing = cardSpacing;
        float maxAllowedSpacing = cardSpacing;

        // 🔹 Step 1: Max allowed spacing (based on X cards)
        if (maxGapCardCount > 1)
        {
            float maxWidthAtX = (maxGapCardCount - 1) * cardSpacing;
            if (maxWidthAtX > maxTotalWidth)
                maxAllowedSpacing = maxTotalWidth / (maxGapCardCount - 1);
        }

        // 🔹 Step 2: Clamp spacing
        if (cardCount > 1)
        {
            float requiredWidth = (cardCount - 1) * maxAllowedSpacing;

            if (requiredWidth > maxTotalWidth)
                effectiveSpacing = maxTotalWidth / (cardCount - 1);
            else
                effectiveSpacing = maxAllowedSpacing;
        }

        // 🔹 Uniform scale
        widthRatio = Screen.width / REF_WIDTH;
        heightRatio = Screen.height / REF_HEIGHT;
        float uniformScale = REF_SCALE * Mathf.Min(widthRatio, heightRatio);

        // 🔹 APPLY SCALE ONCE (CRITICAL FIX)
        effectiveSpacing *= uniformScale;

        // 🔹 Proper centering (spacing-based, consistent)
        float totalWidth = (cardCount - 1) * effectiveSpacing;
        float startX = -totalWidth / 2f;

        // 🔹 Instantiate cards
        for (int i = 0; i < cardCount; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, myDeckPivotPoint);

            Image img = cardObj.GetComponent<Image>();
            img.sprite = MyDeck[i].cardSprite;
            img.SetNativeSize();

            ClickableCardHandler clickable = cardObj.GetComponent<ClickableCardHandler>();
            if (clickable != null)
            {
                clickable.Initialize(MyDeck[i].cardId, this);
                clickable.SetAbility(MyDeck[i].Ability);
            }

            RectTransform rt = cardObj.GetComponent<RectTransform>();

            // 🔒 Native size
            rt.sizeDelta = new Vector2(NATIVE_WIDTH, NATIVE_HEIGHT);

            // ✅ Uniform scale
            rt.localScale = Vector3.one * uniformScale;

            // ✅ Correct positioning (NO double scale)
            float x = startX + i * effectiveSpacing;
            rt.anchoredPosition = new Vector2(
                Mathf.RoundToInt(x),
                0
            );

            rt.SetSiblingIndex(i);

            spawnedCards.Add(cardObj);
            playableCardsID.Add(MyDeck[i].cardId);
        }

        // 🔹 Ensure local avatar stays on top
        uiManager = FindAnyObjectByType<UIManager>();
        uiManager.avatarSlots[0].transform.SetAsLastSibling();
    }


    public void CardDeactivator()
    {
        foreach (GameObject g in spawnedCards)
        {
            g.GetComponent<ClickableCardHandler>().ChangeState(0);
            //g.GetComponent<ClickableCardHandler>().deactivateCard();
        }
    }

    private void UpdateCardBordersBasedOnTurn()
    {
        if (gameManager == null || tableManager == null)
            return;

        // Case 1: Not my turn → normal display
        /* if (!gameManager.IsMyTurn)
         {
             SetAllCardBorders(Color.clear, false);
             return;
         }*/

        // Case 2: My turn, but no cards on table → normal display
        /* if (tableManager.cardsOnTable.Count == 0)
         {
             SetAllCardBorders(Color.clear, false);
             return;
         }*/

        // Case 3: My turn, and cards are on table
        int currentSuit = tableManager.currentSuit;

        // Find cards matching the current suit
        var sameSuitCards = MyDeck.Where(card => tableManager.GetSuitIndex(card.suit) == currentSuit).ToList();
        bool hasSameSuit = sameSuitCards.Count > 0;

        foreach (GameObject cardObj in spawnedCards)
        {
            ClickableCardHandler clickable = cardObj.GetComponent<ClickableCardHandler>();
            if (clickable == null) continue;

            Card cardData = MyDeck.FirstOrDefault(c => c.cardId == clickable.cardId);
            if (cardData == null) continue;

            Image cardImage = cardObj.GetComponent<Image>();

            // Player has same suit
            if (hasSameSuit)
            {
                bool isSameSuit = tableManager.GetSuitIndex(cardData.suit) == currentSuit;
                SetCardBorder(cardImage, isSameSuit ? Color.green : Color.clear);
            }
            else
            {
                // No same-suit cards → red border
                SetCardBorder(cardImage, Color.red);
            }
        }
    }
    private void SetCardBorder(Image cardImage, Color color)
    {
        // You can apply border using material outline or extra UI element.
        // For simplicity, we’ll use the Image’s outline via color tint (border-like).

        Outline outline = cardImage.GetComponent<Outline>();
        if (outline == null)
            outline = cardImage.gameObject.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = new Vector2(color == Color.clear ? 0 : 5, color == Color.clear ? 0 : 5);
    }

    /*private void SetAllCardBorders(Color color, bool addOutline = true)
    {
        foreach (GameObject cardObj in spawnedCards)
        {
            Image img = cardObj.GetComponent<Image>();
            if (img == null) continue;
            if (addOutline)
                SetCardBorder(img, color);
            else
                SetCardBorder(img, Color.clear);
        }
    }*/




    /*public void RemoveCardFromDeck(Card card)
    {
        if (MyDeck.Contains(card))
        {
            MyDeck.Remove(card);
            DisplayCards();
        }
    }
    */
    public int GetSelectedCardId()
    {
        return selectedCardId.Value;
    }

    public int GetRandomCardId()
    {
        if (MyDeck == null || MyDeck.Count == 0)
            return -1;

        int index = UnityEngine.Random.Range(0, MyDeck.Count);
        Debug.Log(index);
        return MyDeck[index].cardId;
    }

    public void SetSelectedCardId(int cardId)
    {
        if (!IsOwner) return;

        selectedCardId.Value = cardId;

        // Track selected GameObject
        foreach (var cardGO in spawnedCards)
        {
            var handler = cardGO.GetComponent<ClickableCardHandler>();
            if (handler != null && handler.cardId == cardId)
            {
                selectedCardObject = cardGO;
                break;
            }
        }
    }
    public void SyncDeckFromServer(List<int> updatedCardIds)
    {
        MyDeck.Clear();

        foreach (int id in updatedCardIds)
        {
            Card card = MyUtilities.GetCardById(id);
            if (card != null)
            {
                MyDeck.Add(card);
            }
        }

        DisplayCards(); // Automatically destroys and rebuilds UI
    }

    public void DestroySelectedCard()
    {
        if (selectedCardObject != null)
        {
            // Remove from visual list
            spawnedCards.Remove(selectedCardObject);

            // Remove from actual deck so DisplayCards() doesn't recreate it
            var handler = selectedCardObject.GetComponent<ClickableCardHandler>();
            if (handler != null)
            {
                Card cardToRemove = MyDeck.FirstOrDefault(c => c.cardId == handler.cardId);
                if (cardToRemove != null)
                    MyDeck.Remove(cardToRemove);
            }

            // Destroy object
            Destroy(selectedCardObject);
            selectedCardObject = null;
        }
    }
    public void InitializePlayerNames(Dictionary<ulong, string> clientNames)
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        uiManager.uiSlotToClientId.Clear();

        // Other players (1–3)
        for (int i = 1; i < rotated.Length; i++)
        {
            ulong clientId = rotated[i];

            if (clientNames.TryGetValue(clientId, out string playerName))
                uiManager.playerNames[i].text = playerName;
            else
                uiManager.playerNames[i].text = "Player";

            uiManager.avatarSlots[i].GetComponent<UnityEngine.UI.Image>().sprite =
                uiManager.avatars[clientId];

            uiManager.uiSlotToClientId[i] = clientId;
        }

        // 🔒 LOCAL PLAYER LOGIC — UNCHANGED
        ulong localClientId = rotated[0];

        uiManager.playerNames[0].text = "";
        uiManager.avatarSlots[0].GetComponent<UnityEngine.UI.Image>().sprite =
            uiManager.avatars[localClientId];
        uiManager.avatarSlots[0].transform.SetAsLastSibling();

        uiManager.uiSlotToClientId[0] = localClientId;
    }



    public bool TryGetSpawnedCardPosition(
        int myCardId,
        RectTransform targetParent,
        out Vector2 localPosition
    )
    {
        foreach (GameObject cardObj in spawnedCards)
        {
            if (cardObj == null)
                continue;

            ClickableCardHandler handler =
                cardObj.GetComponent<ClickableCardHandler>();

            if (handler == null)
                continue;

            if (handler.cardId != myCardId)
                continue;

            RectTransform cardRT = cardObj.GetComponent<RectTransform>();
            if (cardRT == null)
                continue;

            // Convert card world position → target parent local space
            localPosition = targetParent.InverseTransformPoint(cardRT.position);
            return true;
        }

        localPosition = Vector2.zero;
        return false;
    }

    public void placeCardOnTable(int index, Card card, ulong clientID)
    {
        if (clientID != OwnerClientId)
        {
            playSFX.Deal();
        }
        GameObject slot = uiManager.cardsOnTableShots[index];
        GameObject cardObj = Instantiate(cardPrefab, slot.transform.parent);

        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = cardObj.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform slotRT = slot.GetComponent<RectTransform>();
        RectTransform cardRT = cardObj.GetComponent<RectTransform>();

        // 🔹 Match anchors & pivot
        cardRT.anchorMin = slotRT.anchorMin;
        cardRT.anchorMax = slotRT.anchorMax;
        cardRT.pivot = slotRT.pivot;

        // 🔥 FIND CLIENT'S AVATAR SLOT
        int avatarSlotIndex = -1;
        foreach (var kvp in uiManager.uiSlotToClientId)
        {
            if (kvp.Value == clientID)
            {
                avatarSlotIndex = kvp.Key;
                break;
            }
        }
        Vector2 startPos;

        if (avatarSlotIndex != -1)
        {
            RectTransform avatarRT =
                uiManager.avatarSlots[avatarSlotIndex].GetComponent<RectTransform>();

            // Convert avatar position to table parent space
            startPos = slotRT.parent.InverseTransformPoint(avatarRT.position);
        }
        else
        {
            // Fallback (should not happen)
            startPos = cardRT.anchoredPosition;
        }

        Vector2 startSize = cardRT.sizeDelta;
        Vector2 targetPos = slotRT.anchoredPosition;
        Vector2 targetSize = slotRT.sizeDelta;

        // Set initial position BEFORE animation
        cardRT.anchoredPosition = startPos;

        Image cardImage = cardObj.GetComponent<Image>();
        Debug.Log("Reached sprite assign");

        Debug.Log(card == null ? "Card is NULL" : $"Card ID: {card.cardId}");
        Debug.Log(cardImage == null ? "cardImage is NULL" : "cardImage OK");

        if (MyUtilities.cardsDatabase.Any(c => c.cardId == card.cardId))
        {
            Debug.Log("Has Card");
        }
        else
        {
            Debug.Log("No Cards are in database");
        }

        cardImage.sprite = MyUtilities.GetSprite(card.cardId);

        playedCardObjects.Add(cardObj);

        ClickableCardHandler clickable = cardObj.GetComponent<ClickableCardHandler>();
        if (clickable != null)
            clickable.SetAbility(card.Ability);

        // 🔥 Animate ONLY move + scale
        StartCoroutine(
            AnimateCardToSlot(cardRT, startPos, targetPos, startSize, targetSize)
        );
    }


    private IEnumerator AnimateCardToSlot(
    RectTransform cardRT,
    Vector2 startPos,
    Vector2 targetPos,
    Vector2 startSize,
    Vector2 targetSize)
    {
        runningCardAnimations++;   // 🔥 START

        float duration = 0.35f;
        float elapsed = 0f;

        cardRT.SetAsLastSibling();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cardRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            cardRT.sizeDelta = Vector2.Lerp(startSize, targetSize, smoothT);

            yield return null;
        }

        cardRT.anchoredPosition = targetPos;
        cardRT.sizeDelta = targetSize;

        runningCardAnimations--;   // ✅ END
    }



    public void ClearTableVisual(bool Hit = false, ulong targetClientId = ulong.MaxValue)
    {
        if (Hit)
        {
            playSFX.Hit();
        }
        interactionBlocker = FindAnyObjectByType<UIInteractionBlocker>();
        interactionBlocker.transform.SetAsLastSibling();
        interactionBlocker.Block();
        if (clearRoutine != null)
            StopCoroutine(clearRoutine);

        clearRoutine = StartCoroutine(
            ClearAfterAnimationsFinished(Hit, targetClientId)
        );
    }
    private IEnumerator ClearAfterAnimationsFinished(
        bool Hit,
        ulong targetClientId)
    {
        // 1️⃣ Wait until all place-card animations finish
        yield return new WaitUntil(() => runningCardAnimations == 0);

        // 2️⃣ Custom delay AFTER animations
        yield return new WaitForSeconds(animationDelay);

        // 3️⃣ Clear visuals
        CardStashAnimator stashAnimator = FindAnyObjectByType<CardStashAnimator>();
        stashAnimator.AnimateCardsToStash(playedCardObjects, Hit, targetClientId);

        playedCardObjects.Clear();
        interactionBlocker.Unblock();
    }

}
