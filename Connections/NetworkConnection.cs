using System;
using System.IO;
using System.Net.Sockets;

namespace StardewValleyMP.Connections
{
    public class NetworkConnection : IConnection
    {
        private TcpClient socket;
        private NetworkStream stream;

        public NetworkConnection()
        {
        }

        public NetworkConnection( TcpClient theSocket )
        {
            socket = theSocket;
            stream = socket.GetStream();
        }

        ~NetworkConnection()
        {
            disconnect();
        }

        public void connect(String ip, int port)
        {
            disconnect();

            socket = new TcpClient(AddressFamily.InterNetworkV6);
            socket.Client.DualMode = true;
            socket.Connect(ip, port);
            socket.NoDelay = true;

            stream = socket.GetStream();
        }

        public bool isConnected()
        {
            return (socket != null && socket.Connected);
        }

        public void disconnect()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }

        public Stream getStream()
        {
            return stream;
        }
    }
}