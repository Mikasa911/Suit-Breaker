using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;

public static class MyUtilities
{
    public static List<Card> cardsDatabase = new();
    private static Dictionary<string, Sprite> abilitySpriteMap;
    public static Sprite[] cardSprites;
    private static bool abilitySpritesLoaded = false;
    public static int ShuffleSeed = 10;
    public static int lastCardId = 0;
    public static int ClientCount = 0;
    public static string LobbyCode;
    public static int rank=0;
    public static bool UseRandomSeed = true;
    public static readonly string[] suits =
    {
        "Club", "Diamond", "Heart", "Spade"
    };

    public static readonly List<string> Abilities = new()
    {
        "+1", "Club", "Shield", "Diamond","Skip", "Heart", "Spade","Reverse",
        "ReverseHit", "WildCard","Erase","Twin","DownShift"
    };
    public static Dictionary<string, Sprite> abilityButtonSpriteMap;
    private static bool abilityButtonSpritesLoaded = false;

    public static void LoadAbilityButtonSprites()
    {
        if (abilityButtonSpritesLoaded) return;

        abilityButtonSpriteMap = new Dictionary<string, Sprite>();

        // Folder: Assets/Resources/AbilityButtonSprites
        Sprite[] sprites = Resources.LoadAll<Sprite>("AbilityButtonSprites");

        foreach (var s in sprites)
            abilityButtonSpriteMap[s.name] = s;

        abilityButtonSpritesLoaded = true;
    }


    public static void LoadAbilitySprites()
    {
        if (abilitySpritesLoaded) return;

        abilitySpriteMap = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("AbilitySprites");

        foreach (var s in sprites)
            abilitySpriteMap[s.name] = s;

        abilitySpritesLoaded = true;
    }

    public static Sprite GetAbilitySprite(string ability)
    {
        LoadAbilitySprites();

        if (string.IsNullOrEmpty(ability))
            return null;

        abilitySpriteMap.TryGetValue(ability, out var sprite);
        return sprite;
    }
    public static Sprite GetAbilityButtonSprite(string ability)
    {
        LoadAbilityButtonSprites();

        if (string.IsNullOrEmpty(ability))
            return null;

        abilityButtonSpriteMap.TryGetValue(ability, out var sprite);
        return sprite;
    }
    public static void LoadSprites()
    {
        cardSprites = Resources.LoadAll<Sprite>("Cards/");

    }
    public static List<Card> InitializeCards()
    {
        cardsDatabase.Clear();
        LoadSprites();
        for (int suitCode = 0; suitCode < suits.Length; suitCode++)
        {
            string suit = suits[suitCode];
            //string suit = suits[2];

            // 2 → King
            for (int value = 2; value <= 13; value++)
            {
                //int cardId = (value * 10) + suitCode;
                cardsDatabase.Add(CreateCard(value, value - 1, suit, -1));
            }

            // Ace
            //int aceId = (1 * 10) + suitCode;
            cardsDatabase.Add(CreateCard(1, 13, suit, -1));
        }

        return cardsDatabase;
    }


    public static int SetCardAbility(int cardId, int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= Abilities.Count)
        {
            Debug.LogWarning("Invalid ability index");
            return cardId;
        }

        Card card = GetCardById(cardId);
        if (card == null)
        {
            Debug.LogError($"Card not found for cardId {cardId}");
            return cardId;
        }

        switch (Abilities[abilityIndex])
        {
            case "DownShift":
                {
                    int newCardId = GetDownShiftedCardId(card);
                    return newCardId;
                    //return ApplyCanonicalCard(card, newCardId);
                }

            case "Spade":
            case "Club":
            case "Heart":
            case "Diamond":
                {
                    //int newCardId = (value * 10) + suitIndex;
                    return ApplyCanonicalCard(cardId, GetSuitIndex(Abilities[abilityIndex]));
                }

            default:
                Card newCard = CreateCard(card.cardValue, card.cardRank, card.suit, abilityIndex);
                cardsDatabase.Add(newCard);
                return newCard.cardId;
        }

    }
    public static Card CreateCard(int value, int rank, string suit, int ability)
    {
        string abilityString;
        if (ability == -1)
        {
            abilityString = "none";
        }
        else
        {
            abilityString = Abilities[ability];
        }
        Card card = new()
        {
            cardValue = value,
            cardRank = rank,
            suit = suit,
            cardId = ++lastCardId,
            Ability = abilityString
        };

        // string spriteName = $"{value}_{suit.ToLower()}";
        // card.cardSprite = Resources.Load<Sprite>($"Cards/{spriteName}");
        string spriteName = $"{value}_{suit.ToLower()}";

        card.cardSprite =  Array.Find(
            cardSprites,
            s => s.name == spriteName
        );


        if (card.cardSprite == null)
            Debug.LogWarning($"❌ Sprite not found: {spriteName}");
        return card;
    }
    private static int GetDownShiftedCardId(Card card)
    {
        int currentValue = card.cardValue; // 1–13
        int currentRank = card.cardRank;
        int newRank = currentRank - 2;
        if (newRank <= 0)
            newRank += 13;
        int newValue = currentValue - 2;
        if (newValue <= 0)
            newValue += 13;

        Card newCard = new()
        {
            cardId = ++lastCardId,
            cardRank = newRank,
            cardValue = newValue,
            cardSprite = GetSprite(newValue, card.suit),
            Ability = card.Ability,
            suit = card.suit
        };
        cardsDatabase.Add(newCard);

        return newCard.cardId;
        //return (newValue * 10) + suitIndex;
    }
    private static int ApplyCanonicalCard(int originalCardId, int suit = -1)
    {
        string suitString = suit == -1 ? GetSuitString(originalCardId) : suits[suit];

        Card originalCard = GetCardById(originalCardId);
        if (originalCard == null)
        {
            Debug.LogError($"Canonical card missing for cardId {originalCardId}");
            return originalCardId;
        }
        Card card = new()
        {
            cardId = ++lastCardId,
            cardValue = originalCard.cardValue,
            cardRank = originalCard.cardRank,
            suit = suitString,
            cardSprite = GetSprite(originalCard.cardValue, suitString),
            Ability = originalCard.Ability,
        };

        cardsDatabase.Add(card);
        return card.cardId;
    }

    public static Card GetCardById(int id)
    {
        return cardsDatabase.Find(c => c.cardId == id);
    }

    public static Sprite GetSprite(int cardId)
    {
        Card card = GetCardById(cardId);
        return card.cardSprite;
    }

    public static Sprite GetSprite(int value, string suit)
    {
        Card card = cardsDatabase.Find(c =>
            c.cardValue == value &&
            string.Equals(c.suit, suit, System.StringComparison.OrdinalIgnoreCase)
        );

        return card?.cardSprite;
    }

    public static int GetSuitIndex(int cardId)
    {
        return GetSuitIndex(GetCardById(cardId).suit);
        //return cardId % 10;
    }

    public static string GetSuitString(int cardId)
    {
        return GetCardById(cardId).suit;
        // return suits[cardId % 10];
    }

    public static int GetRank(int cardId)
    {
        return GetCardById(cardId).cardRank;
        //return cardId / 10;
    }

    public static int GetSuitIndex(string suitName)
    {
        for (int i = 0; i < suits.Length; i++)
        {
            if (string.Equals(suits[i], suitName,
                System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public static void Shuffle<T>(List<T> list)
    {
        System.Random rng = new(ShuffleSeed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    public static int GetAbilityIndex(string ability)
    {
        return Abilities.IndexOf(ability);
    }

}



