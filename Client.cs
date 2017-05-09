using StardewValleyMP.Connections;
using StardewValleyMP.Interface;
using StardewValleyMP.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP
{
    public class Client
    {
        public enum NetStage
        {
            WaitingForID,
            WaitingForWorldData,
            Waiting,
            Playing,
        }

        private IConnection conn;
        private Thread receiver;
        private BlockingCollection<Packet> toReceive = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        public byte id = 255;
        public NetStage stage = NetStage.WaitingForID;

        public Dictionary< byte, SFarmer > others = new Dictionary< byte, SFarmer >();

        public Client( IConnection theConn )
        {
            conn = theConn;
            receiver = new Thread(receiveAndQueue);
            receiver.Start();

            new VersionPacket().writeTo(conn.getStream());

            Multiplayer.sendFunc = send;
        }

        ~Client()
        {
            conn.disconnect();
            receiver.Join();
        }

        private Queue<Packet> packetDelay = new Queue<Packet>();
        public void processDelayedPackets()
        {
            while (packetDelay.Count > 0)
            {
                try
                {
                    packetDelay.Dequeue().process(this);
                }
                catch ( Exception e )
                {
                    Log.error("Exception processing delayed packet: " + e);
                }
            }
        }
        
        public void update()
        {
            if ( !conn.isConnected() )
            {
                ChatMenu.chat.Add(new ChatEntry(null, "You lost connection to the server."));
                Multiplayer.mode = Mode.Singleplayer;
                Multiplayer.client = null;
                return;
            }
            
            if (stage != NetStage.Waiting)
            {
                processDelayedPackets();
            }

            if ( stage == NetStage.Playing )
            {
                Multiplayer.doMyPlayerUpdates(id);
            }

            try
            {
                while (toReceive.Count > 0)
                {
                    Packet packet;
                    bool success = toReceive.TryTake(out packet);
                    if (!success) continue;

                    packet.process(this);
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception receiving: " + e);
            }
        }

        public void forceUpdate()
        {
            try
            {
                Packet packet = Packet.readFrom(conn.getStream());
                if (packet != null) packet.process(this);
            }
            catch (Exception e)
            {
                Log.error("Exception logging packet: " + e);
            }
        }

        public void send(Packet packet)
        {
            packet.writeTo(conn.getStream());
        }

        private void receiveAndQueue()
        {
            try
            {
                while (conn.isConnected())
                {
                    Packet packet = Packet.readFrom(conn.getStream());
                    toReceive.Add(packet);
                }
            }
            catch (Exception e)
            {
                Log.error("Exception while receiving: " + e);
                Multiplayer.mode = Mode.Singleplayer;
                Multiplayer.client = null;
            }
        }
    }
}
