using StardewValleyMP.Platforms;
using Steamworks;
using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public class SteamConnection : PlatformConnection
    {
        private Stream stream;

        // Ugh, wish I could just do friend SteamPlatform; or something
        internal SteamConnection( Friend theFriend, bool alreadyConnected = false )
        :   base( theFriend, alreadyConnected )
        {
            stream = new SteamStream(this);
        }

        public override bool isConnected()
        {
            return friend != null;
        }

        public override void disconnect()
        {
            if (friend == null) return;

            Log.trace("Closed P2P session with " + friend.displayName);
            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(friend.id));
            friend = null;
        }

        public override void accept()
        {
            Log.trace("Accepted P2P session with " + friend.displayName);
            SteamNetworking.AcceptP2PSessionWithUser( new CSteamID( friend.id ) );
            accepted = true;
        }

        public override Stream getStream()
        {
            return stream;
        }
    }
}