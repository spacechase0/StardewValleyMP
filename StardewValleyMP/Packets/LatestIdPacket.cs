using StardewValley;
using System.IO;

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
            Log.trace("Updated latest ID");
            Multiplayer.prevLatestId = MultiplayerUtility.latestID = latest;
        }

        public override void process(Server server, Server.Client client)
        {
            Log.trace("Updated latest ID");
            Multiplayer.prevLatestId = MultiplayerUtility.latestID = latest;
        }

        public override string ToString()
        {
            return base.ToString() + " " + latest;
        }
    }
}
