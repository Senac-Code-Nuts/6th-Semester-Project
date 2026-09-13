using System;
using Unity.Netcode;

namespace PiGame.Lobby
{
    public struct LobbyMapVoteData : INetworkSerializable, IEquatable<LobbyMapVoteData>
    {
        public ulong ClientId;
        public LobbyMapId MapId;
        public bool IsConfirmed;

        public LobbyMapVoteData(
            ulong clientId,
            LobbyMapId mapId = LobbyMapId.Random,
            bool isConfirmed = false)
        {
            ClientId = clientId;
            MapId = mapId;
            IsConfirmed = isConfirmed;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref MapId);
            serializer.SerializeValue(ref IsConfirmed);
        }

        public bool Equals(LobbyMapVoteData other)
        {
            return ClientId == other.ClientId
                && MapId == other.MapId
                && IsConfirmed == other.IsConfirmed;
        }

        public override bool Equals(object obj)
        {
            return obj is LobbyMapVoteData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ClientId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MapId;
                hashCode = (hashCode * 397) ^ IsConfirmed.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(LobbyMapVoteData left, LobbyMapVoteData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LobbyMapVoteData left, LobbyMapVoteData right)
        {
            return !left.Equals(right);
        }
    }
}
