using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class Card : INetworkSerializable
{
    public int runtimeId;
    public int cardRank;
    public int cardValue;
    public string suit;
    public Sprite cardSprite;
    public int cardId;
    public string Ability;
    public Card()
    {
        Ability = "none";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardId);
        serializer.SerializeValue(ref Ability);
    }
}
