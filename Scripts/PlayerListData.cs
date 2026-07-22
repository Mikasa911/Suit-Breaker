using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
public struct PlayerListData : INetworkSerializable
{
    public FixedString64Bytes name;
    public int rank;
    public int coins;

    public int diamonds;

    public ulong playerID;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref coins);
        serializer.SerializeValue(ref diamonds);
        serializer.SerializeValue(ref rank);
    }
}
