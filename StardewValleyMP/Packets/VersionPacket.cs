using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace StardewValleyMP.Packets
{
    // Client -> Server
    // Send the protocol version to the server to make sure we match.
    public class VersionPacket : Packet
    {
        public byte version = Multiplayer.PROTOCOL_VERSION;

        public VersionPacket() : base( ID.Version )
        {
        }

        protected override void read(BinaryReader reader)
        {
            version = reader.ReadByte();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(version);
        }

        public override void process( Server server, Server.Client client )
        {
            if (version == Multiplayer.PROTOCOL_VERSION)
            {
                client.stage = Server.Client.NetStage.WaitingForFarmerInfo;
                client.send(new YourIDPacket(client.id));
            }
            else
            {
                client.stageFailed = true;
            }
        }
    }
}
