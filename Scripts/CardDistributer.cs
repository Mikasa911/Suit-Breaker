using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CardDistributer : NetworkBehaviour
{
    public Dictionary<ulong, List<Card>> playerCardMap = new();
    public Dictionary<ulong, List<string>> playerAbilityMap = new();
    //public List<string> Abilities = new List<string> { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen" };

    [SerializeField] public int cardsPerPlayer = 2;
    /// <summary>
    /// Called to distribute cards to players on the server.
    /// </summary>
    public void DistributeCards(List<Card> deck, List<ulong> clientIds)
    {
        MyUtilities.rank = 0;
        AssignCardsToPlayers(deck, clientIds);
        SendCardIdsToClients();
    }
    public void DistributeAbilities(List<string> Abilities, List<ulong> clientIds)
    {
        AssignAbilitiesToPlayers(Abilities, clientIds);
        SendAbilitiesToClient();
    }
    /// <summary>
    /// Assigns 13 cards from the shuffled deck to each client ID.
    /// </summary>
    void AssignAbilitiesToPlayers(List<string> Abilties, List<ulong> clientIds)
    {
        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];
            playerAbilityMap[clientId] = Abilties.GetRange(i * 3, 3);
        }
    }
    private void AssignCardsToPlayers(List<Card> deck, List<ulong> clientIds)
    {

        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];
            playerCardMap[clientId] = deck.GetRange(i * cardsPerPlayer, cardsPerPlayer);
        }
    }
    /// <summary>
    /// Sends the card IDs to each client using ClientRpc.
    /// </summary>
    private void SendCardIdsToClients()
    {
        foreach (var pair in playerCardMap)
        {
            ulong clientId = pair.Key;
            List<Card> cards = pair.Value;

            int[] cardIds = cards.Select(c => c.cardId).ToArray();

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            ReceiveCardIdsClientRpc(cardIds, rpcParams);
        }
    }
    private void SendAbilitiesToClient()
    {
        foreach (var pair in playerAbilityMap)
        {
            ulong clientId = pair.Key;
            List<string> Abilities = pair.Value;
            int[] AbilitiesArray = Abilities.Select(a => MyUtilities.Abilities.IndexOf(a)).ToArray();

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            ReceiveAbiltiesClientRpc(AbilitiesArray, rpcParams);
        }
    }
    [ClientRpc]
    void ReceiveAbiltiesClientRpc(int[] Abilities, ClientRpcParams rpcParams = default)
    {
        PlayerBehavior player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerBehavior>();
        player.AssignMyAbilities(Abilities);
    }
    /// <summary>
    /// Receives card IDs on each client and assigns the corresponding cards.
    /// </summary>
    [ClientRpc]
    private void ReceiveCardIdsClientRpc(int[] cardIds, ClientRpcParams rpcParams = default)
    {
        PlayerBehavior player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerBehavior>();
        player.AssignMyDeck(cardIds); // This method should assign cards using CardInitializer.CardDatabase

    }

    private void LogDistributedCards()
    {
        foreach (var pair in playerCardMap)
        {
            Debug.Log($"🃏 Client {pair.Key} received {pair.Value.Count} cards.");
            foreach (var card in pair.Value)
            {
                Debug.Log($"- {card.cardValue} of {card.suit}");
            }
        }
    }
    public void RemoveCardFromPlayerDeck(int cardID, ulong ClientID)
    {
        Card card = MyUtilities.GetCardById(cardID);
        if (card != null)
        {
            if (playerCardMap.TryGetValue(ClientID, out var cardList))
            {
                cardList.Remove(card);
            }
        }
    }
    public void EliminatePlayers()
    {
        var keysToRemove = playerCardMap
        .Where(kvp => kvp.Value == null || kvp.Value.Count == 0)
        .Select(kvp => kvp.Key)
        .ToList();

        // Debug.Log("Elimination");

        foreach (var key in keysToRemove)
        {
            playerCardMap.Remove(key);
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager.ShowPlayerWon(++MyUtilities.rank, key);
            if (playerCardMap.Count <= 1)
            {


                // HandleConnectedClients handleConnectedClients=FindAnyObjectByType<HandleConnectedClients>();
                //handleConnectedClients.StartGameByLobby();
                ulong lastPlayerId = playerCardMap.Keys.First();
                gameManager.ShowPlayerWon(++MyUtilities.rank, lastPlayerId);
                gameManager.ShowSummary();
                gameManager.StopGameClientRpc();
            }
        }
    }

    public List<Card> GetCardsForPlayer(ulong clientId)
    {
        if (playerCardMap.TryGetValue(clientId, out var cards))
        {
            return cards;
        }
        return null;
    }

}
