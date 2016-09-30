using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewModdingAPI;
using StardewValleyMP.Vanilla;

namespace StardewValleyMP.Packets
{
    // Server -> Client
    // Send the client their ID.
    // Also signals them to send their farmer info.
    public class YourIDPacket : Packet
    {
        public byte clientId;

        public YourIDPacket() : base( ID.YourID )
        {
        }

        public YourIDPacket(byte theId)
            : this()
        {
            clientId = theId;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
        }

        public override void process(Client client)
        {
            client.id = clientId;
        }
    }
}
