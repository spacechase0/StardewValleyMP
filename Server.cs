using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;
using StardewValleyMP.Connections;
using StardewValleyMP.Interface;
using StardewValleyMP.Packets;
using StardewValleyMP.Platforms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP
{
    // Anywhere in here 'Client' refers to the subclass, NOT the client-mode Client class
    public class Server
    {
        public bool playing = false;
        public ServerConnectionInterface connections;

        public Server()
        {
            Multiplayer.sendFunc = broadcast;
            connections = new ServerConnectionInterface(this);
        }

        private DateTime lastTimeSync;
        public void update()
        {
            for (int i = 0; i < clients.Count; ++i )
            {
                clients[i].update();
                if ( !clients[ i ].connected() )
                {
                    clients.Remove( clients[ i ] );
                    --i;
                    continue;
                }
            }

            if (Multiplayer.lobby) return;

            if (playing && Game1.player != null)
            {
                Multiplayer.doMyPlayerUpdates(0);

                if ((DateTime.Now - lastTimeSync).TotalMilliseconds >= 10000) // 10 seconds? Sure, why not
                {
                    broadcast(new TimeSyncPacket());
                    lastTimeSync = DateTime.Now;
                }
            }
        }

        public void broadcast(Packet packet) { broadcast( packet, -1 ); } // Can't use default parameter because of passing as Action
        public void broadcast(Packet packet, int except )
        {
            foreach ( Client client in clients )
            {
                if (client.id == except) continue;
                client.send(packet);
            }
        }

        // Non-game stuff
        // Client management
        public List<Client> clients = new List<Client>();

        public int currentlyAccepting { get; set; }
        public void addClient(IConnection socket, bool askResend = false)
        {
            ++currentlyAccepting;
            Log.info("Got new client.");

            Task.Run(
            () =>
            {
                Client client = new Client(this, (byte)getPlayerCount(), socket);
                if (askResend)
                    client.send(new VersionPacket());

                client.update();
                while (client.stage == Client.NetStage.VerifyingVersion)
                {
                    if (client.stageFailed)
                    {
                        Log.info("\tBad protocol version.");
                        return;
                    }

                    Thread.Sleep(10);
                    client.update();

                }

                clients.Add(client);
                Log.trace("Finished accepting client");
            } );
        }

        public int getClientCount() { return clients.Count; }
        public int getPlayerCount() { return clients.Count + 1; }

        public class Client
        {
            public enum NetStage
            {
                VerifyingVersion,
                WaitingForFarmerInfo,
                WaitingForStart,
                Playing,
            }

            private Server server;
            public readonly byte id;
            private IConnection socket;
            private Thread receiver;
            private BlockingCollection<Packet> toReceive = new BlockingCollection<Packet>( new ConcurrentQueue< Packet >());
            public NetStage stage = NetStage.VerifyingVersion;
            public bool stageFailed = false;

            public string farmerXml = null;
            public SFarmer farmer = null;
            public int farmType = -1;

            public IDictionary<string, GameLocation> addDuringLoading = new Dictionary<string, GameLocation>();

            public Client(Server theServer, byte theId, IConnection theSocket)
            {
                server = theServer;
                id = theId;
                socket = theSocket;
                receiver = new Thread( receiveAndQueue );
                receiver.Start();
            }

            ~Client()
            {
                if (socket != null)
                {
                    socket.disconnect();
                    socket = null;
                }
                if (receiver != null)
                {
                    receiver.Join();
                    receiver = null;
                }
            }

            public bool connected()
            {
                return (socket != null);
            }

            public void update()
            {
                if (socket == null) return;
                if (!socket.isConnected())
                {
                    ChatMenu.chat.Add(new ChatEntry(null, ( farmer != null ? farmer.name : ( "Client " + id ) ) + " lost connection to the server."));
                    if ( farmer != null && farmer.currentLocation != null )
                    {
                        farmer.currentLocation.farmers.Remove(farmer);
                    }
                    socket = null;
                    return;
                }

                try
                {
                    while (toReceive.Count > 0)
                    {
                        Packet packet;
                        bool success = toReceive.TryTake(out packet);
                        if (!success) continue;

                        packet.process(server, this);
                    }
                }
                catch ( Exception e )
                {
                    Log.error("Exception receiving: " + e);
                }
            }

            public void send( Packet packet )
            {
                packet.writeTo(socket.getStream());
            }

            private void receiveAndQueue()
            {
                try
                {
                    while (connected())
                    {
                        Packet packet = Packet.readFrom(socket.getStream());
                        toReceive.Add(packet);
                    }
                }
                catch ( Exception e )
                {
                    Log.error("Exception while receiving: " + e);
                }
            }
        }
    }
}
