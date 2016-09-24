using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMP.Packets
{
    // Client -> Server
    // Send the protocol version to the server to make sure we match.
    public class LatestIdPacket : Packet
    {
        public long latest;

        public LatestIdPacket() : base( ID.LatestId )
        {
            latest = MultiplayerUtility.latestID;
        }

        protected override void read(BinaryReader reader)
        {
            latest = reader.ReadInt64();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(latest);
        }

        public override void process(Client client)
        {
            Log.Async("Updated latest ID");
            Multiplayer.prevLatestId = MultiplayerUtility.latestID = latest;
        }

        public override void process(Server server, Server.Client client)
        {
            Log.Async("Updated latest ID");
            Multiplayer.prevLatestId = MultiplayerUtility.latestID = latest;
        }
    }
}
