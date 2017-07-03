using StardewValley;
using StardewValley.Quests;
using StardewValleyMP.Connections;
using StardewValleyMP.Interface;
using StardewValleyMP.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SFarmer = StardewValley.Farmer;

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
            Log.debug("Getting information on the clients.");
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
            Log.debug("Broadcasting world info.");

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
            foreach ( var quest in SaveGame.loaded.player.questLog)
            {
                if (quest is SlayMonsterQuest)
                    (quest as SlayMonsterQuest).loadQuestInfo();
            }
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
                others.others.Add(0, Util.serialize<SFarmer>(SaveGame.loaded.player));

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

        public int currentlyAccepting { get; set; }
        public void addClient(IConnection socket, bool askResend = false)
        {
            ++currentlyAccepting;
            Log.info("Got new client.");

            Client client = new Client(this, (byte)getPlayerCount(), socket);
            if (askResend)
            {
                client.send(new VersionPacket());
            }

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

            public bool sentId = false;

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
                    Log.error("Exception receiving: " + e);
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
#if NETWORKING_BENCHMARK
                int bytes = packet.writeTo(stream);
                Interlocked.Add(ref Multiplayer.serverToClientBytesTransferred, bytes);
                Log.Async("Sent packet " + packet + " ( " + bytes + " bytes)");
#else
                packet.writeTo(socket.getStream());
#endif
            }

            private void receiveAndQueue()
            {
                try
                {
                    while (connected())
                    {
                        try
                        {
                            Packet packet = Packet.readFrom(socket.getStream());
                            toReceive.Add(packet);
                        }
                        catch ( Exception e )
                        {
                            Log.error("Exception while receiving: " + e);
                            socket.disconnect();
                        }

#if NETWORKING_BENCHMARK
                        using (MemoryStream tmpMs = new MemoryStream())
                        {
                            int bytes = packet.writeTo(tmpMs);
                            Interlocked.Add(ref Multiplayer.clientToServerBytesTransferred, bytes);
                            Log.Async("Received packet " + packet + " ( " + bytes + " bytes)");
                        }
#endif
                    }
                }
                catch ( Exception e )
                {
                }
            }
        }
    }
}
