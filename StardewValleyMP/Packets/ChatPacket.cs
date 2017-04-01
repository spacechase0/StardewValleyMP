using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // A chat message.
    public class ChatPacket : Packet
    {
        public byte clientId;
        public string message;

        public ChatPacket()
            : base(ID.Chat)
        {
        }

        public ChatPacket(byte theId, string theMessage)
            : this()
        {
            clientId = theId;
            message = theMessage;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            message = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write( clientId );
            writer.Write(message);
        }

        public override void process(Client client)
        {
            if (clientId == 255)
            {
                doSFarmer(null);
                return;
            }

            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;

            doSFarmer(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            if (clientId == 255)
            {
                doSFarmer(null);
                return;
            }

            if (clientId != client.id) return;

            doSFarmer(client.farmer);
            server.broadcast(this, client.id);
        }

        private void doSFarmer( SFarmer farmer )
        {
            ChatMenu.chat.Add(new ChatEntry(farmer, message));
        }
    }
}
