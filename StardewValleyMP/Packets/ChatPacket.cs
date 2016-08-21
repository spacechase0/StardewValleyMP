using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;

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

            Farmer farmer = client.others[clientId];
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

        private void doFarmer( Farmer farmer )
        {
            ChatMenu.chat.Add(new ChatEntry(farmer, message));
        }
    }
}
