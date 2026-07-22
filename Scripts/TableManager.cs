// using System.Collections.Generic;
// using System.Linq;
// using Unity.Netcode;
// using UnityEngine;

// [System.Serializable]
// public struct PlayedCard
// {
//     public Card card;
//     public ulong clientId;
// }
// public class TableManager : MonoBehaviour
// {
//     //    public Dictionary<Card, ulong> cardsOnTable = new();
//     public List<PlayedCard> cardsOnTable = new();

//     public List<GameObject> playedCardObjects = new();
//     private CardDistributer cardDistributer;
//     private GameManager gameManager;

//     public int currentSuit; // Stores index: 0 = Clubs, 1 = Diamonds, 2 = Hearts, 3 = Spades

//     private void Awake()
//     {
//         cardDistributer = GetComponent<CardDistributer>();
//         gameManager = GetComponent<GameManager>();
//     }

//     public void ProcessCardDeal(int cardID, ulong clientID)
//     {
//         Card card = MyUtilities.GetCardById(cardID);
//         cardDistributer.RemoveCardFromPlayerDeck(card.cardId, clientID);
//         if (card == null) return;

//         if (cardsOnTable.Count == 0)
//         {
//             StaringCard(card, clientID);
//             gameManager.InitializeCardOnCurrentTurnClient(cardsOnTable.Count, currentSuit, gameManager.GetCurrentTurnClientId());
//         }
//         else
//         {
//             AddCardToTable(card, clientID);
//         }
//         //gameManager.InitilaizePlayableCardsClientRpc();
//         //gameManager.InitializeCardOnCurrentTurnClient(cardsOnTable.Count, currentSuit, gameManager.GetCurrentTurnClientId());
//     }
//     private void StaringCard(Card card, ulong clientID)
//     {
//         //cardsOnTable[card] = clientID;
//         cardsOnTable.Add(new PlayedCard
//         {
//             card = card,
//             clientId = clientID
//         });


//         currentSuit = GetSuitIndex(card.suit);
//         if (card.Ability == "+1" && HasAtLeastTwoCardsOfSuit(clientID, card.suit))
//         {
//             return;
//         }
//         gameManager.EndTurn();
//     }
//     public bool HasAtLeastTwoCardsOfSuit(ulong playerId, string suit)
//     {
//         // Ensure player exists
//         if (!cardDistributer.playerCardMap.TryGetValue(playerId, out List<Card> cards))
//             return false;

//         int count = 0;

//         foreach (var card in cards)
//         {
//             if (card.suit == suit)
//             {
//                 count++;
//                 if (count >= 1)
//                     return true;
//             }
//         }

//         return false;
//     }

//     private void AddCardToTable(Card card, ulong clientID)
//     {
//         //cardsOnTable[card] = clientID;
//        cardsOnTable.Add(new PlayedCard
//         {
//             card = card,
//             clientId = clientID
//         });

//         if (GetSuitIndex(card.suit) == currentSuit)
//         {
//             if (card.Ability == "+1" && HasAtLeastTwoCardsOfSuit(clientID, card.suit))
//                 return;
//             else
//                 gameManager.EndTurn();

//             if (HasEveryPlayerPlayedAtLeastOneCard() && card.Ability != "+1")
//             {
//                 DiscardCardsOnTable();
//             }
//         }
//         else
//         {
//             // HitPlayer();
//             if (card.Ability == "+1")
//             {
//                 return;
//             }
//             var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerData>();
//             player.clearTableVisual();
//             HitPlayer(clientID);
//         }
//     }
//     public void DiscardCardsOnTable()
//     {
//         var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerData>();
//         player.clearTableVisual();
//         currentSuit = -1;
//         gameManager.DisplayCardsClientRpc();
//         StartNextRound();
//         Debug.Log("New Round");
//     }

//     public void HitPlayer(ulong hittingClientId)
//     {
//         if (cardsOnTable.Count == 0) return;

//         int highestValue = -1;
//         ulong targetClientId = ulong.MaxValue;
//         bool multipleHighestRanks = false;

//         foreach (var entry in cardsOnTable)
//         {
//             // ❌ Skip hitting player cards
//             if (entry.Value == hittingClientId)
//                 continue;

//             Card card = entry.Key;
//             ulong clientId = entry.Value;

//             if (card.cardRank > highestValue)
//             {
//                 // New highest found → reset
//                 highestValue = card.cardRank;
//                 targetClientId = clientId;
//                 multipleHighestRanks = false;
//             }
//             else if (card.cardRank == highestValue)
//             {
//                 // Tie detected
//                 multipleHighestRanks = true;
//             }
//         }
//         if (multipleHighestRanks)
//         {
//             // Tie → no one gets hit, discard table, start fresh round
//             cardsOnTable.Clear();
//             gameManager.ClearTableClientRpc();

//             currentSuit = -1;

//             EliminateClients();
//             DecidePlayerTurn(true); // normal round continuation

//             gameManager.DisplayCardsClientRpc();
//             return; // 🔴 CRITICAL: stop further execution
//         }

//         else
//         {
//             if (targetClientId == ulong.MaxValue)
//                 return;

//             // Ensure key exists
//             if (!cardDistributer.playerCardMap.ContainsKey(targetClientId))
//                 cardDistributer.playerCardMap[targetClientId] = new List<Card>();

//             foreach (var entry in cardsOnTable)
//             {
//                 cardDistributer.playerCardMap[targetClientId].Add(entry.Key);
//             }

//             cardsOnTable.Clear();

//             gameManager.ClearTableClientRpc();

//             // 🔁 Sync target client's deck
//             List<int> updatedCardIds = new();
//             foreach (var card in cardDistributer.playerCardMap[targetClientId])
//                 updatedCardIds.Add(card.cardId);

//             var clientParams = new ClientRpcParams
//             {
//                 Send = new ClientRpcSendParams
//                 {
//                     TargetClientIds = new[] { targetClientId }
//                 }
//             };
//             gameManager.SyncDeckClientRpc(updatedCardIds.ToArray(), clientParams);
//         }
//         var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerData>();
//         player.clearTableVisual();
//         currentSuit = -1;
//         gameManager.DisplayCardsClientRpc();
//         EliminateClients();
//         DecidePlayerTurn(true);
//     }


//     private void StartNextRound()
//     {
//         EliminateClients();
//         DecidePlayerTurn(false);
//         cardsOnTable.Clear();
//         gameManager.ClearTableClientRpc();
//     }

//     private void EliminateClients()
//     {
//         cardDistributer.EliminatePlayers();
//     }

//     private void DecidePlayerTurn(bool isHit)
//     {
//         if (isHit)
//         {
//             while (!cardDistributer.playerCardMap.ContainsKey(gameManager.GetCurrentTurnClientId()))
//             {
//                 gameManager.EndTurn();
//             }

//             return;
//         }
//         ulong clientIDOfHighestValueCard = GetClientIdOfHighestCard();

//         while (!cardDistributer.playerCardMap.ContainsKey(clientIDOfHighestValueCard))
//         {
//             RemoveCardFromTableByClientId(clientIDOfHighestValueCard);
//             clientIDOfHighestValueCard = GetClientIdOfHighestCard();
//         }

//         gameManager.SetCurrentTurn(clientIDOfHighestValueCard);
//     }



//     public ulong GetClientIdOfHighestCard()
//     {
//         int highestValue = -1;
//         ulong clientIdOfHighestCard = ulong.MaxValue;
//         bool multipleHighestRanks = false;

//         ulong firstCardClientId = ulong.MaxValue;
//         bool firstCaptured = false;

//         foreach (var entry in cardsOnTable)
//         {
//             Card card = entry.Key;
//             ulong clientId = entry.Value;

//             // Capture first card's clientId
//             if (!firstCaptured)
//             {
//                 firstCardClientId = clientId;
//                 firstCaptured = true;
//             }

//             if (card.cardRank > highestValue)
//             {
//                 highestValue = card.cardRank;
//                 clientIdOfHighestCard = clientId;
//                 multipleHighestRanks = false;
//             }
//             else if (card.cardRank == highestValue)
//             {
//                 multipleHighestRanks = true;
//             }
//         }

//         // If tie → return first card's clientId
//         if (multipleHighestRanks)
//             return firstCardClientId;

//         return clientIdOfHighestCard;
//     }




//     public void RemoveCardFromTableByClientId(ulong clientId)
//     {
//         foreach (var pair in cardsOnTable)
//         {
//             if (pair.Value == clientId)
//             {
//                 cardsOnTable.Remove(pair.Key);
//                 break;
//             }
//         }
//     }

//     public int GetSuitIndex(string suit)
//     {
//         return suit switch
//         {
//             "Club" => 0,
//             "Diamond" => 1,
//             "Heart" => 2,
//             "Spade" => 3,
//             _ => -1
//         };
//     }
//     public bool HasEveryPlayerPlayedAtLeastOneCard()
//     {
//         foreach (ulong playerId in cardDistributer.playerCardMap.Keys)
//         {
//             bool found = false;

//             foreach (ulong playedBy in cardsOnTable.Values)
//             {
//                 if (playedBy == playerId)
//                 {
//                     found = true;
//                     break;
//                 }
//             }

//             if (!found)
//                 return false;
//         }

//         return true;
//     }

// }
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public struct PlayedCard
{
    public Card card;
    public ulong clientId;
}

public class TableManager : MonoBehaviour
{
    public List<PlayedCard> cardsOnTable = new();
    public List<GameObject> playedCardObjects = new();

    private CardDistributer cardDistributer;
    private GameManager gameManager;
    bool isReverse = false;
    public int currentSuit; // 0 = Club, 1 = Diamond, 2 = Heart, 3 = Spade

    public bool getReverse()
    {
        return isReverse;
    }
    private void Awake()
    {
        cardDistributer = GetComponent<CardDistributer>();
        gameManager = GetComponent<GameManager>();
    }

    public void ProcessCardDeal(int cardID, ulong clientID)
    {
        Card card = MyUtilities.GetCardById(cardID);
        if (card == null) return;

        cardDistributer.RemoveCardFromPlayerDeck(card.cardId, clientID);
        if (card.Ability == "Skip")
            gameManager.EndTurn(isReverse);
        if (card.Ability == "Reverse")
            isReverse = !isReverse;

        cardsOnTable.Add(new PlayedCard { card = card, clientId = clientID });
        if (cardsOnTable.Count == 1)
        {
            if (HasEveryPlayerPlayedAtLeastOneCard())
            {
                DiscardCardsOnTable();
                return;
            }
            StartingCard(card, clientID);
            gameManager.InitializeCardOnCurrentTurnClient(cardsOnTable.Count, currentSuit,
            gameManager.GetCurrentTurnClientId()
            );
        }
        else
        {
            AddCardToTable(card, clientID);
        }
    }

    private void StartingCard(Card card, ulong clientID)
    {
        //cardsOnTable.Add(new PlayedCard { card = card, clientId = clientID });

        currentSuit = GetSuitIndex(card.suit);
        gameManager.ChangeBackGroundClientRpc(currentSuit);

        if (card.Ability == "+1" && HasAtLeastTwoCardsOfSuit(clientID, card.suit))
        {
            Debug.Log("has card");
            return;
        }


        gameManager.EndTurn(isReverse);
    }



    private void AddCardToTable(Card card, ulong clientID)
    {
        //cardsOnTable.Add(new PlayedCard { card = card, clientId = clientID });
        if (card.Ability == "WildCard")
        {
            currentSuit = GetSuitIndex(card.suit);
            gameManager.ChangeBackGroundClientRpc(currentSuit);
        }
        if (GetSuitIndex(card.suit) == currentSuit || card.Ability == "WildCard")
        {
            // currentSuit = GetSuitIndex(card.suit);
            if (card.Ability == "+1" && HasAtLeastTwoCardsOfSuit(clientID, card.suit))
                return;

            gameManager.EndTurn(isReverse);

            //if (HasEveryPlayerPlayedAtLeastOneCard() && card.Ability != "+1")
            if (HasEveryPlayerPlayedAtLeastOneCard())
            {
                DiscardCardsOnTable();
            }
        }
        else
        {
            if (card.Ability == "+1")
                return;
            HitPlayer(clientID);
        }
    }

    public void DiscardCardsOnTable()
    {
        var player = NetworkManager.Singleton.SpawnManager
            .GetLocalPlayerObject()?.GetComponent<PlayerBehavior>();

        player?.ClearTableVisual();

        currentSuit = -1;
        gameManager.ChangeBackGroundClientRpc(currentSuit);
        gameManager.DisplayCardsClientRpc();
        StartNextRound();
    }
    public void HitPlayer(ulong hittingClientId)
    {
        if (cardsOnTable.Count == 0)
            return;

        int highestValue = -1;
        Card highestCard = null;
        ulong targetClientId = ulong.MaxValue;

        // Track all highest-rank cards (excluding hitter)
        List<(Card card, ulong clientId)> highestEntries = new();

        // 1️⃣ Find highest rank
        foreach (var entry in cardsOnTable)
        {
            if (entry.clientId == hittingClientId)
                continue;

            if (entry.card.cardRank > highestValue)
            {
                highestValue = entry.card.cardRank;
                highestEntries.Clear();
                highestEntries.Add((entry.card, entry.clientId));
            }
            else if (entry.card.cardRank == highestValue)
            {
                highestEntries.Add((entry.card, entry.clientId));
            }
        }

        // 2️⃣ Handle multiple highest with Shield exclusion
        if (highestEntries.Count > 1)
        {
            // Remove Shield cards from highest-rank group
            var nonShieldHighest = highestEntries.FindAll(e => e.card.Ability != "Shield");

            if (nonShieldHighest.Count == 1)
            {
                highestCard = nonShieldHighest[0].card;
                targetClientId = nonShieldHighest[0].clientId;
            }
            else
            {
                // Still tie OR only shields → discard & restart
                cardsOnTable.Clear();
                gameManager.ClearTableClientRpc();

                currentSuit = -1;
                gameManager.ChangeBackGroundClientRpc(currentSuit);
                EliminateClients();
                DecidePlayerTurn(true);
                gameManager.DisplayCardsClientRpc();
                return;
            }
        }
        else if (highestEntries.Count == 1)
        {
            highestCard = highestEntries[0].card;
            targetClientId = highestEntries[0].clientId;
        }

        if (targetClientId == ulong.MaxValue)
            return;

        // 3️⃣ ReverseHit override
        if (highestCard.Ability == "ReverseHit")
        {
            targetClientId = cardsOnTable[cardsOnTable.Count - 1].clientId;
        }

        // 4️⃣ Shield handling (secondary recalculation)
        if (highestCard.Ability == "Shield")
        {
            // Shield + 2 cards → discard
            if (cardsOnTable.Count == 2)
            {
                cardsOnTable.Clear();
                gameManager.ClearTableClientRpc();

                currentSuit = -1;
                gameManager.ChangeBackGroundClientRpc(currentSuit);
                EliminateClients();
                DecidePlayerTurn(true);
                gameManager.DisplayCardsClientRpc();
                return;
            }

            // Recalculate highest excluding hitter + shield holder
            highestValue = -1;
            highestCard = null;
            ulong shieldHolderId = targetClientId;
            targetClientId = ulong.MaxValue;

            foreach (var entry in cardsOnTable)
            {
                if (entry.clientId == hittingClientId ||
                    entry.clientId == shieldHolderId)
                    continue;

                if (entry.card.cardRank > highestValue)
                {
                    highestValue = entry.card.cardRank;
                    highestCard = entry.card;
                    targetClientId = entry.clientId;
                }
            }

            if (targetClientId == ulong.MaxValue)
                return;
        }


        if (!cardDistributer.playerCardMap.ContainsKey(targetClientId))
            cardDistributer.playerCardMap[targetClientId] = new List<Card>();

        foreach (var entry in cardsOnTable)
        {
            cardDistributer.playerCardMap[targetClientId].Add(entry.card);
        }

        cardsOnTable.Clear();
        gameManager.ClearTableClientRpc(true, targetClientId);

        // Sync deck
        List<int> updatedCardIds = new();
        foreach (var c in cardDistributer.playerCardMap[targetClientId])
            updatedCardIds.Add(c.cardId);

        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        };

        gameManager.SyncDeckClientRpc(updatedCardIds.ToArray(), clientParams);

        //var player = NetworkManager.Singleton.SpawnManager
        // .GetLocalPlayerObject()?.GetComponent<PlayerData>();

        //player?.ClearTableVisual(true);

        currentSuit = -1;
        gameManager.ChangeBackGroundClientRpc(currentSuit);
        //gameManager.DisplayCardsClientRpc();

        EliminateClients();
        DecidePlayerTurn(true);
    }

    private void StartNextRound()
    {
      
        EliminateClients();
        DecidePlayerTurn(false);
        cardsOnTable.Clear();
        gameManager.ClearTableClientRpc();
    }

    private void EliminateClients()
    {
        cardDistributer.EliminatePlayers();
    }

    private void DecidePlayerTurn(bool isHit)
    {
        if (isHit)
        {
            while (!cardDistributer.playerCardMap.ContainsKey(
                gameManager.GetCurrentTurnClientId()))
            {
                gameManager.EndTurn(isReverse);
            }
            return;
        }

        ulong clientId = GetClientIdOfHighestCard();
        gameManager.SetCurrentTurn(clientId);
    }

    public ulong GetClientIdOfHighestCard()
    {
        int highestValue = -1;
        ulong firstClientId = ulong.MaxValue;
        ulong bestClientId = ulong.MaxValue;
        bool multipleHighest = false;

        for (int i = 0; i < cardsOnTable.Count; i++)
        {
            var entry = cardsOnTable[i];

            if (i == 0)
                firstClientId = entry.clientId;

            if (entry.card.cardRank > highestValue)
            {
                highestValue = entry.card.cardRank;
                bestClientId = entry.clientId;
                multipleHighest = false;
            }
            else if (entry.card.cardRank == highestValue)
            {
                multipleHighest = true;
            }
        }

        return multipleHighest ? firstClientId : bestClientId;
    }

    public void RemoveCardFromTableByClientId(ulong clientId)
    {
        cardsOnTable.RemoveAll(pc => pc.clientId == clientId);
    }

    public int GetSuitIndex(string suit)
    {
        return suit switch
        {
            "Club" => 0,
            "Diamond" => 1,
            "Heart" => 2,
            "Spade" => 3,
            _ => -1
        };
    }

    public bool HasEveryPlayerPlayedAtLeastOneCard()
    {
        int totalPlayers = cardDistributer.playerCardMap.Count;

        HashSet<ulong> playersWhoPlayed = new();
        HashSet<ulong> skippedPlayers = new();

        // Track who played
        foreach (var played in cardsOnTable)
        {
            playersWhoPlayed.Add(played.clientId);
        }

        // Identify skipped players (direction-aware)
        foreach (var played in cardsOnTable)
        {
            if (played.card.Ability != "Skip")
                continue;

            int currentIndex = gameManager.getSyncedClients().IndexOf(played.clientId);
            if (currentIndex == -1)
                continue;

            int direction = isReverse ? -1 : 1;
            int skippedIndex =
                (currentIndex + direction + gameManager.getSyncedClients().Count) % gameManager.getSyncedClients().Count;

            ulong skippedClientId = gameManager.getSyncedClients()[skippedIndex];
            skippedPlayers.Add(skippedClientId);
        }

        int requiredPlays = totalPlayers - skippedPlayers.Count;

        return playersWhoPlayed.Count >= requiredPlays;
    }



    public bool HasAtLeastTwoCardsOfSuit(ulong playerId, string suit)
    {
        if (!cardDistributer.playerCardMap.TryGetValue(playerId, out var cards))
            return false;

        int count = 0;
        foreach (var c in cards)
        {
            if ((c.suit == suit || c.Ability == "WildCard") && ++count >= 1)
                return true;
        }
        return false;
    }
}
