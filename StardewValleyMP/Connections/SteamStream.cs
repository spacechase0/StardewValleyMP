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
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            uint size;

            long oldPos = incoming.Position;
            incoming.Position = incoming.Seek(0, SeekOrigin.End);
            while (incoming.Length - oldPos < count)//(SteamNetworking.IsP2PPacketAvailable(out size, conn.channel))
            {
                //Log.trace("" + incoming.Length + " " + incoming.Position + " " + oldPos + " " + count);
                if ( !SteamNetworking.IsP2PPacketAvailable(out size, conn.channel) )
                {
                    Thread.Sleep(1);
                    continue;
                }
                byte[] tmp = new byte[size];
                CSteamID id = new CSteamID();
                SteamNetworking.ReadP2PPacket(tmp, size, out size, out id, conn.channel);
                incoming.Write(tmp, 0, (int)size);
            }
            incoming.Position = oldPos;
                
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
            SteamNetworking.SendP2PPacket(new CSteamID(conn.friend.id), buffer, (uint) count, EP2PSend.k_EP2PSendReliable, conn.channel);
        }
    }
}
