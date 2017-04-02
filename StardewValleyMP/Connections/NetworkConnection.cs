using System;
using System.IO;
using System.Net.Sockets;

namespace StardewValleyMP.Connections
{
    public class NetworkConnection : IConnection
    {
        private TcpClient conn;
        private NetworkStream stream;

        public NetworkConnection()
        {
        }

        ~NetworkConnection()
        {
            disconnect();
        }

        public void connect(String ip, int port)
        {
            disconnect();

            TcpClient socket = new TcpClient(AddressFamily.InterNetworkV6);
            socket.Client.DualMode = true;
            socket.Connect(ip, port);
            socket.NoDelay = true;
        }

        override public bool isConnected()
        {
            return (socket != null && socket.Connected);
        }

        override public void disconnect()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (conn != null)
            {
                conn.Close();
                conn = null;
            }
        }

        override public Stream getStream()
        {
            return stream;
        }
    }
}