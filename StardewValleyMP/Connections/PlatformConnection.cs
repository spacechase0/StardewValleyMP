using StardewValleyMP.Platforms;
using Steamworks;
using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public abstract class PlatformConnection : IConnection
    {
        public Friend friend { get; protected set; }

        public bool accepted { get; protected set; }

        // Ugh, wish I could just do friend SteamPlatform; or something
        protected PlatformConnection( Friend theFriend, bool alreadyConnected = false )
        {
            friend = theFriend;
            accepted = alreadyConnected;
        }

        ~PlatformConnection()
        {
            disconnect();
        }

        public abstract bool isConnected();
        public abstract void disconnect();
        public abstract void accept();

        public abstract Stream getStream();
    }
}