using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public byte id;
        public NetStage stage = NetStage.WaitingForID;

        public Dictionary< byte, Farmer > others = new Dictionary< byte, Farmer >();

        public bool tempStopUpdating = false;

        public Client( TcpClient theSocket )
        {
            socket = theSocket;
            stream = socket.GetStream();

            new VersionPacket().writeTo(stream);

            Multiplayer.sendFunc = send;
        }

        ~Client()
        {
            if ( stream != null ) stream.Close();
            if ( socket != null ) socket.Close();
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
            
            while ( true )
            {
                try
                {
                    Packet packet = checkPacket();
                    if (packet == null)
                    {
                        break;
                    }

                    if (stage == NetStage.Waiting && packet.id != ID.NextDay && packet.id != ID.Chat)
                    {
                        if ( packet.id == ID.OtherFarmerData || packet.id == ID.WorldData )
                            packetDelay.Enqueue(packet);
                    }
                    else
                        packet.process(this);
                }
                catch ( Exception e )
                {
                    Log.Async("Exception processing packet: " + e);
                }
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
            packet.writeTo(stream);
        }

        private Packet checkPacket()
        {
            if (!stream.DataAvailable)
            {
                return null;
            }

            return Packet.readFrom(stream);
        }
    }
}
