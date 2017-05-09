using StardewModdingAPI;
using StardewValley;
using StardewValleyMP.Connections;
using StardewValleyMP.Interface;
using StardewValleyMP.Platforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StardewValleyMP
{
    // TODO: Better name for this
    public class ServerConnectionInterface
    {
        private readonly Server server;
        private Thread listenThread;
        private Thread platformThread;

		internal ServerConnectionInterface( Server theServer )
        {
            server = theServer;
        }

        public void startListening(int port, bool lan, bool platform)
        {
            listenThread = new Thread(() => listen(port));
            listenThread.Start();

            if (lan)
            {
                string name = Path.GetFileNameWithoutExtension(Constants.CurrentSavePath);
                name = name.Substring(0, name.LastIndexOf('_'));
                LanDiscovery.startServer(name, port);
            }

            if (platform)
            {
                IPlatform.instance.onFriendConnected = new Action<Friend, PlatformConnection>(onFriendConnected);
            }
        }

        private static TcpListener listener = null;
        private void listen(int port)
        {
            try
            {
                // http://stackoverflow.com/questions/1777629/how-to-listen-on-multiple-ip-addresses
                listener = Util.UsingMono ? new TcpListener(IPAddress.Any, port) : TcpListener.Create(port);
                listener.Start();

                Log.info("Waiting for connection...");
                TcpClient socket = listener.AcceptTcpClient();
                socket.NoDelay = true;
                server.addClient(new NetworkConnection(socket));
            }
            catch (Exception e)
            {
                if (e is SocketException && (((SocketException)e).Message.IndexOf("A blocking operation was interrupted") != -1 ||
                                              ((SocketException)e).Message.IndexOf("WSACancelBlockingCall") != -1))
                    return;

                Log.error("Exception while listening: " + e);
                ChatMenu.chat.Add(new ChatEntry(null, "Exception while listening for clients: "));
                ChatMenu.chat.Add(new ChatEntry(null, e.Message));
                ChatMenu.chat.Add(new ChatEntry(null, "Check your log file for more details."));
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
        }

        private void onFriendConnected(Friend friend, PlatformConnection conn)
        {
            Log.trace("onFriendConnected " + friend.displayName + " " + conn);
            Game1.activeClickableMenu = new PendingConnectionMenu(conn);
        }
    }
}