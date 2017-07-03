using StardewValleyMP.Connections;
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

        public override void process( Client client )
        {
            client.send(new VersionPacket());
        }

        public override void process( Server server, Server.Client client )
        {
            Log.trace("Got version packet");
            if (client.stage != Server.Client.NetStage.VerifyingVersion)
            {
                Log.debug("Got version packet at wrong stage");
                //return;
            }

            if (version == Multiplayer.PROTOCOL_VERSION)
            {
                client.stage = Server.Client.NetStage.WaitingForFarmerInfo;
                if (!client.sentId)
                {
                    Log.trace("Sending ID packet " + client.id);
                    client.send(new YourIDPacket(client.id));
                    client.sentId = true;
                }
            }
            else
            {
                Log.trace("Bad version from client " + client.id);
                client.stageFailed = true;
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (int)version;
        }
    }
}
