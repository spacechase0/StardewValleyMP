using System;
using System.IO;

namespace StardewValleyMP.Connections
{
    public interface IConnection
    {
        bool isConnected();
        void disconnect();

        Stream getStream();
    }
}