using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public class SteamConnection : IConnection
    {
        public SteamConnection()
        {
        }

        ~SteamConnection()
        {
            disconnect();
        }

        public void connect(String ip, int port)
        {
            disconnect();
        }

        public bool isConnected()
        {
            return false;
        }

        public void disconnect()
        {
        }

        public Stream getStream()
        {
            return null;
        }
    }
}