using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using StardewValley;
using StardewValleyMP.Packets;
using StardewModdingAPI;

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

        private TcpClient socket;
        private NetworkStream stream;
        private Thread receiver;
        private BlockingCollection<Packet> toReceive = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
        public byte id = 255;
        public NetStage stage = NetStage.WaitingForID;

        public Dictionary< byte, Farmer > others = new Dictionary< byte, Farmer >();

        public bool tempStopUpdating = false;

        public Client( TcpClient theSocket )
        {
            socket = theSocket;
            stream = socket.GetStream();
            receiver = new Thread(receiveAndQueue);
            receiver.Start();

            new VersionPacket().writeTo(stream);

            Multiplayer.sendFunc = send;
        }

        ~Client()
        {
            if ( stream != null ) stream.Close();
            if ( socket != null ) socket.Close();
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
                    Log.Async("Exception processing delayed packet: " + e);
                }
            }
        }
        
        public void update()
        {
            if ( !socket.Connected )
            {
                ChatMenu.chat.Add(new ChatEntry(null, "You lost connection to the server."));
                Multiplayer.mode = Mode.Singleplayer;
                Multiplayer.client = null;
                return;
            }

            if (tempStopUpdating) return;
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

                    if (stage == NetStage.Waiting && packet.id != ID.NextDay && packet.id != ID.Chat /*&& packet.id != ID.WorldData*/)
                        packetDelay.Enqueue(packet);
                    else packet.process(this);
                }
            }
            catch ( Exception e )
            {
                Log.Async("Exception receiving: " + e);
            }
        }

        public void forceUpdate()
        {
            try
            {
                Packet packet = Packet.readFrom(stream);
                if (packet != null) packet.process(this);
            }
            catch (Exception e)
            {
                Log.Async("Exception logging packet: " + e);
            }
        }

        public void send(Packet packet)
        {
#if false
            try
            {
                using (MemoryStream s = new MemoryStream())
                {
                    packet.writeTo(s);
                    byte[] bytes = s.GetBuffer();
                    stream.Write( bytes, 0, bytes.Length );
                }
            }
            catch ( Exception e )
            {
                Log.Async("Exception sending " + packet + " to server: " + e);
            }
#endif
#if NETWORKING_BENCHMARK
            int bytes = packet.writeTo(stream);
            Interlocked.Add(ref Multiplayer.clientToServerBytesTransferred, bytes);
            Log.Async("Sent packet " + packet + " ( " + bytes + " bytes)");
#else
            packet.writeTo(stream);
#endif
        }

        private void receiveAndQueue()
        {
            try
            {
                while (socket.Connected)
                {
                    Packet packet = Packet.readFrom(stream);
                    if ( MultiplayerMod.FAKE_LATENCY )
                    {
                        Thread.Sleep(100);
                    }
                    toReceive.Add(packet);

#if NETWORKING_BENCHMARK
                    using (MemoryStream tmpMs = new MemoryStream())
                    {
                        int bytes = packet.writeTo(tmpMs);
                        Interlocked.Add(ref Multiplayer.serverToClientBytesTransferred, bytes);
                        Log.Async("Received packet " + packet + " ( " + bytes + " bytes)");
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Log.Async("Exception while receiving: " + e);
                Multiplayer.mode = Mode.Singleplayer;
                Multiplayer.client = null;
            }
        }
    }
}
