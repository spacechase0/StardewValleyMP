using StardewValleyMP.Interface;
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
                doFarmer(null);
                return;
            }

            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;

            doFarmer(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            if (clientId == 255)
            {
                doFarmer(null);
                return;
            }

            if (clientId != client.id) return;

            doFarmer(client.farmer);
            server.broadcast(this, client.id);
        }

        private void doFarmer( SFarmer farmer )
        {
            ChatMenu.chat.Add(new ChatEntry(farmer, message));
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " <msg not included>";
        }
    }
}
