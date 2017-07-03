using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StardewValleyMP
{
    public class LanDiscovery
    {
        // These are two different ports so that it works for localhost
        public const int DEFAULT_PORT_REQUEST = 24645;
        public const int DEFAULT_PORT_RESPONSE = 24646;
        public static byte[] MAGIC { get { return Encoding.ASCII.GetBytes("SVMP"); } }

        // http://stackoverflow.com/questions/746519/udpclient-receive-on-broadcast-address

        private static bool running = false;
        private static Thread thread;
        private static UdpClient client;

        public static Action<string, IPEndPoint, int> onDiscovery;

        public static bool isRunning() { return running; }

        public static void stop()
        {
            Log.info("LAN Discovery: Stopping");
            if (running)
            {
                running = false;
                if ( client != null )
                    client.Close();
                if ( thread != null )
                    thread.Join();
                thread = null;
                client = null;
            }
        }

        public static void startServer(string name, int port)
        {
            if (running || thread != null) return;

            thread = new Thread(() => runServer(name, port));
            thread.Start();
        }

        private static void runServer( string name, int port )
        {
            try
            {
                IPEndPoint addr = new IPEndPoint(IPAddress.Any, DEFAULT_PORT_REQUEST);
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.ExclusiveAddressUse = false;
                client.Client.Bind(addr);

                Log.debug("LAN Discovery: Starting to listen");

                running = true;
                while (running)
                {
                    IPEndPoint other = null;
                    byte[] bytes = client.Receive(ref other);
                    if (bytes.Length < MAGIC.Length + 1) continue;
                    Log.debug("LAN Discovery: Received a request from " + other);

                    bool okay = true;
                    for (int i = 0; i < MAGIC.Length; ++i)
                    {
                        if (bytes[i] != MAGIC[i])
                        {
                            okay = false;
                            break;
                        }
                    }
                    if (!okay)
                    {
                        Log.debug("LAN Discovery: Bad magic");
                        continue;
                    }
                    if (bytes[MAGIC.Length] != Multiplayer.PROTOCOL_VERSION)
                    {
                        Log.debug("LAN Discovery: Bad protocol version (" + (int)bytes[MAGIC.Length] + " vs " + (int)Multiplayer.PROTOCOL_VERSION + ")");
                        continue;
                    }

                    Log.debug("LAN Discovery: Preparing and sending response");
                    byte[] portBytes = BitConverter.GetBytes(port);
                    byte[] nameBytes = Encoding.ASCII.GetBytes(name);
                    byte[] sendBack = new byte[MAGIC.Length + portBytes.Length + nameBytes.Length];
                    Array.Copy(MAGIC, 0, sendBack, 0, MAGIC.Length);
                    Array.Copy(portBytes, 0, sendBack, MAGIC.Length, portBytes.Length);
                    Array.Copy(nameBytes, 0, sendBack, MAGIC.Length + portBytes.Length, nameBytes.Length);

                    other.Port = DEFAULT_PORT_RESPONSE;
                    UdpClient sendFrom = new UdpClient();
                    sendFrom.Send(sendBack, sendBack.Length, new IPEndPoint(IPAddress.Parse("10.0.2.15"), DEFAULT_PORT_RESPONSE));
                    sendFrom.Close();
                }
            }
            catch ( Exception e )
            {
                if (e is SocketException && (((SocketException)e).Message.IndexOf("A blocking operation was interrupted") != -1 ||
                                              ((SocketException)e).Message.IndexOf("WSACancelBlockingCall") != -1))
                    return;
                Log.error("Exception during LAN discovery: " + e);
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
        }

        public static void startClient()
        {
            if (!running && thread == null)
            {
                thread = new Thread(() => runClient());
                thread.Start();
            }

            // https://stackoverflow.com/a/436898
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!ni.SupportsMulticast)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    UdpClient client = null;
                    try
                    {
                        Log.trace("Doing " + ni.Name + " " + ni.Id + " " + ua.Address);
                        IPEndPoint addr = new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT_REQUEST);
                        client = new UdpClient();
                        client.Client.ExclusiveAddressUse = false;
                        client.Client.Bind(new IPEndPoint(ua.Address, DEFAULT_PORT_REQUEST));
                        //client.JoinMulticastGroup(IPAddress.Parse("255.1.2.3"), ua.Address);

                        byte[] toSend = new byte[MAGIC.Length + 1];
                        Array.Copy(MAGIC, 0, toSend, 0, MAGIC.Length);
                        toSend[MAGIC.Length] = Multiplayer.PROTOCOL_VERSION;

                        Log.debug("LAN Discovery: Broadcasting request");
                        client.Send(toSend, toSend.Length, addr);
                    }
                    catch ( Exception e )
                    {
                        Log.trace("Exception: " + e);
                    }
                    finally
                    {
                        if ( client != null )
                            client.Close();
                    }
                }
            }
        }

        private static void runClient()
        {
            try
            {
                IPEndPoint addr = new IPEndPoint(IPAddress.Any, DEFAULT_PORT_RESPONSE);
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.ExclusiveAddressUse = false;
                client.Client.Bind(addr);

                running = true;
                while (running)
                {
                    IPEndPoint other = null;
                    byte[] bytes = client.Receive(ref other);
                    if (bytes.Length < MAGIC.Length + 5) continue;
                    Log.debug("LAN Discovery: Received a response from " + other);

                    bool okay = true;
                    for (int i = 0; i < MAGIC.Length; ++i)
                    {
                        if (bytes[i] != MAGIC[i])
                        {
                            okay = false;
                            break;
                        }
                    }
                    if (!okay)
                    {
                        Log.debug("LAN Discovery: Bad magic");
                        continue;
                    }

                    Log.debug("LAN Discovery: Valid LAN discovery found.");

                    byte[] nameBytes = new byte[bytes.Length - MAGIC.Length - 4];
                    Array.Copy(bytes, MAGIC.Length + 4, nameBytes, 0, nameBytes.Length);
                    int port = BitConverter.ToInt16(bytes, MAGIC.Length);
                    string name = Encoding.ASCII.GetString(nameBytes);
                    if (onDiscovery != null)
                        onDiscovery.Invoke(name, other, port);
                }
            }
            catch (Exception e)
            {
                if (e is SocketException && (((SocketException)e).Message.IndexOf("A blocking operation was interrupted") != -1 ||
                                              ((SocketException)e).Message.IndexOf("WSACancelBlockingCall") != -1))
                    return;
                Log.error("Exception during LAN discovery: " + e);
            }
            finally
            {
                if ( client != null )
                {
                    client.Close();
                    client = null;
                }
            }
        }
    }
}
