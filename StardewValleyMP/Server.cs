using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using StardewValley;
using StardewValleyMP.Packets;
using StardewModdingAPI;

namespace StardewValleyMP
{
    // Anywhere in here 'Client' refers to the subclass, NOT the client-mode Client class
    public class Server
    {
        public bool playing = false;
        public bool delayUpdates = false;

        public Server()
        {
            Multiplayer.sendFunc = broadcast;
        }

        private DateTime lastTimeSync;
        public void update()
        {
            for (int i = 0; !delayUpdates && i < clients.Count; ++i )
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

            if ( clients.Count == 0 )
            {
                ChatMenu.chat.Add(new ChatEntry(null, "No more clients."));
                Multiplayer.mode = Mode.Singleplayer;
                Multiplayer.server = null;
                return;
            }

            if (playing && Game1.player != null)
            {
                Multiplayer.doMyPlayerUpdates(0);

                if ((DateTime.Now - lastTimeSync).TotalMilliseconds >= 10000 /*&& Game1.timeOfDay % 100 == 0*/) // 10 seconds? Sure, why not
                {
                    broadcast(new TimeSyncPacket());
                    lastTimeSync = DateTime.Now;
                }
            }/*
            else if ( !playing && Game1.player != null )
            {
                bool othersReady = true;
                foreach ( Server.Client client in clients )
                {
                    othersReady = othersReady && ( client.stage == Client.NetStage.WaitingForStart );
                }

                if ( othersReady )
                {
                    playing = true;
                }
            }*/
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

        public void getPlayerInfo()
        {
            Log.Async("Getting information on the clients.");
            /*foreach ( Client client in clients )
            {
                client.send(new YourIDPacket(client.id));
            }*/

            // Wait for responses
            foreach ( Client client in clients )
            {
                while ( client.stage != Client.NetStage.WaitingForStart )
                {
                    client.update();
                    Thread.Sleep(10);
                }
            }
        }

        public void broadcastInfo()
        {
            Log.Async("Broadcasting world info.");

            // World data packet is the same for everyone, so go ahead and prepare it

            /*String saveFile = SaveGame.loaded.player.Name + "_" + SaveGame.loaded.uniqueIDForThisGame;
            string worldPath = Path.Combine(new string[]
			        {
				        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				        "StardewValley",
				        "Saves",
				        saveFile,
				        saveFile
			        });
            String xml = File.ReadAllText(worldPath);*/
            MemoryStream tmp = new MemoryStream();
            SaveGame.serializer.Serialize(tmp, SaveGame.loaded);
            WorldDataPacket world = new WorldDataPacket(Encoding.UTF8.GetString(tmp.ToArray()));

            foreach ( Client client in clients )
            {
                // Send other farmers first
                OtherFarmerDataPacket others = new OtherFarmerDataPacket();

                /*string savePath = Path.Combine(new string[]
			    {
				    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				    "StardewValley",
				    "Saves",
				    saveFile,
				    "SaveGameInfo"
			    });
                String myXml = File.ReadAllText(savePath);*/
                others.others.Add(0, Util.serialize<Farmer>(SaveGame.loaded.player));

                foreach ( Client other in clients )
                {
                    if (client == other) continue;
                    others.others.Add(other.id, other.farmerXml );
                }
                client.send(others);

                // Send world info
                client.send(world);

                client.stage = Client.NetStage.Playing;
            }

            lastTimeSync = DateTime.Now;
        }

        // Non-game stuff
        // Client management
        public List<Client> clients = new List<Client>();

        public void addClient(Socket socket, NetworkStream stream)
        {
            Log.Async("Got new client.");

            Client client = new Client(this, (byte)getPlayerCount(), socket, stream);

            client.update();
            while (client.stage == Client.NetStage.VerifyingVersion)
            {
                if (client.stageFailed)
                {
                    Log.Async("\tBad protocol version.");
                    return;
                }

                Thread.Sleep(10);
                client.update();

            }

            clients.Add(client);
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
            private Socket socket;
            private NetworkStream stream;
            private Thread receiver;
            private BlockingCollection<Packet> toReceive = new BlockingCollection<Packet>( new ConcurrentQueue< Packet >());
            public NetStage stage = NetStage.VerifyingVersion;
            public bool stageFailed = false;

            public string farmerXml = null;
            public Farmer farmer = null;

            public Client(Server theServer, byte theId, Socket theSocket, NetworkStream theStream)
            {
                server = theServer;
                id = theId;
                socket = theSocket;
                stream = theStream;
                receiver = new Thread( receiveAndQueue );
                receiver.Start();
            }

            ~Client()
            {
                if ( stream != null ) stream.Close();
                if ( socket != null ) socket.Close();
                receiver.Join();
            }

            //public bool tempStopUpdating = false;
            private Queue<Packet> packetDelay = new Queue<Packet>();
            public void processDelayedPackets()
            {
                while (packetDelay.Count > 0)
                {
                    packetDelay.Dequeue().process(server, this);
                }
            }

            public bool connected()
            {
                return (socket != null);
            }

            public void update()
            {
                if (socket == null) return;
                if (!socket.Connected)
                {
                    ChatMenu.chat.Add(new ChatEntry(null, ( farmer != null ? farmer.name : ( "Client " + id ) ) + " lost connection to the server."));
                    if ( farmer != null && farmer.currentLocation != null )
                    {
                        farmer.currentLocation.farmers.Remove(farmer);
                    }
                    socket = null;
                    return;
                }

                //if (tempStopUpdating) return;
                if (stage != NetStage.WaitingForStart) processDelayedPackets();

                try
                {
                    while (toReceive.Count > 0)
                    {
                        Packet packet;
                        bool success = toReceive.TryTake(out packet);
                        if (!success) continue;

                        if (server.playing && stage == NetStage.WaitingForStart)
                        {
                            packetDelay.Enqueue(packet);
                        }
                        else packet.process(server, this);
                    }
                }
                catch ( Exception e )
                {
                    Log.Async("Exception receiving: " + e);
                }
            }

            public void send( Packet packet )
            {
#if false
                try
                {
                    using (MemoryStream s = new MemoryStream())
                    {
                        packet.writeTo(s);
                        byte[] bytes = s.GetBuffer();
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception e)
                {
                    Log.Async("Exception sending " + packet + " to client " + (farmer != null ? farmer.name : ("Client " + id)) + ": " + e);
                }
#endif
                packet.writeTo(stream);
            }

            private void receiveAndQueue()
            {
                try
                {
                    while (connected())
                    {
                        Packet packet = Packet.readFrom(stream);
                        toReceive.Add(packet);
                    }
                }
                catch ( Exception e )
                {
                    Log.Async("Exception while receiving: " + e);
                }
            }
        }
    }
}
