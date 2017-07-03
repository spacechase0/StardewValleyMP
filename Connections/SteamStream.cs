using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StardewValleyMP.Connections
{
    public class SteamStream : Stream
    {
        private SteamConnection conn;
        private MemoryStream incoming = new MemoryStream();

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public SteamStream(SteamConnection theConn )
        {
            conn = theConn;
            streams[conn.friend.id] = this;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long oldPos = incoming.Position;
            while (incoming.Length - oldPos < count)
            {
                Thread.Sleep(1);
                continue;
            }
                
            return incoming.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset != 0) throw new NotSupportedException();
            SteamNetworking.SendP2PPacket(new CSteamID(conn.friend.id), buffer, (uint) count, EP2PSend.k_EP2PSendReliable, 0);
        }

        public static void update()
        {
            uint size;
            if (!SteamNetworking.IsP2PPacketAvailable(out size, 0))
            {
                return;
            }
            
            byte[] tmp = new byte[size];
            CSteamID id = new CSteamID();
            SteamNetworking.ReadP2PPacket(tmp, size, out size, out id, 0);
            Log.trace("Got a steam packet: " + size + " " + id);
            if (streams.ContainsKey(id.m_SteamID))
            {
                Log.trace("Have a stream matching that ID!");
                streams[id.m_SteamID].incoming.Write(tmp, 0, (int)size);
            }
        }

        private static Dictionary<ulong, SteamStream> streams = new Dictionary<ulong, SteamStream>();
    }
}
