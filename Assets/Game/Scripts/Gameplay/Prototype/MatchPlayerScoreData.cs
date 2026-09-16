using System;
using Unity.Netcode;

namespace PiGame.Gameplay
{
    public struct MatchPlayerScoreData :
        INetworkSerializable,
        IEquatable<MatchPlayerScoreData>
    {
        public ulong ClientId;
        public int PlayerSlot;
        public int Kills;

        public MatchPlayerScoreData(ulong clientId, int playerSlot, int kills = 0)
        {
            ClientId = clientId;
            PlayerSlot = playerSlot;
            Kills = kills;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerSlot);
            serializer.SerializeValue(ref Kills);
        }

        public bool Equals(MatchPlayerScoreData other)
        {
            return ClientId == other.ClientId
                && PlayerSlot == other.PlayerSlot
                && Kills == other.Kills;
        }

        public override bool Equals(object obj)
        {
            return obj is MatchPlayerScoreData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ClientId.GetHashCode();
                hashCode = (hashCode * 397) ^ PlayerSlot;
                hashCode = (hashCode * 397) ^ Kills;
                return hashCode;
            }
        }
    }
}
