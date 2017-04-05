using StardewValleyMP.Platforms;
using Steamworks;
using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public class SteamConnection : PlatformConnection
    {
        // Ugh, wish I could just do friend SteamPlatform; or something
        internal SteamConnection( Friend theFriend, bool alreadyConnected = false )
        :   base( theFriend, alreadyConnected )
        {
        }

        public override bool isConnected()
        {
            return friend != null && accepted;
        }

        public override void disconnect()
        {
            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(friend.id));
            friend = null;
        }

        public override Stream getStream()
        {
            return null;
        }
    }
}