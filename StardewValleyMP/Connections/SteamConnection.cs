using StardewValleyMP.Platforms;
using Steamworks;
using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public class SteamConnection : IConnection
    {
        public Friend friend { get; private set; }

        public bool accepted { get; private set; }

        // Ugh, wish I could just do friend SteamPlatform; or something
        internal SteamConnection( Friend theFriend )
        {
            friend = theFriend;
            accepted = false;
        }

        ~SteamConnection()
        {
            disconnect();
        }

        public bool isConnected()
        {
            return friend != null && accepted;
        }

        public void disconnect()
        {
            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(friend.id));
            friend = null;
        }

        public Stream getStream()
        {
            return null;
        }
    }
}