using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Inform everyone of someone being engaged/married. (Engaged is the name + "engaged", I think)
    public class SpousePacket : Packet
    {
        public byte clientId;
        public string spouse;

        public SpousePacket()
            : base(ID.Spouse)
        {
        }

        public SpousePacket(byte theId, string theSpouse)
            : this()
        {
            clientId = theId;
            spouse = theSpouse;
            spouseMessage();
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            spouse = reader.ReadString();
            if (spouse == "") spouse = null;
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write( clientId );
            writer.Write(spouse != null ? spouse : "");
        }

        public override void process(Client client)
        {
            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;

            doSFarmer(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            if (clientId != client.id) return;

            doSFarmer(client.farmer);
            server.broadcast(this, client.id);
        }

        private void doSFarmer( SFarmer farmer )
        {
            farmer.spouse = spouse;
            spouseMessage();
        }

        private void spouseMessage()
        {
            return;

            SFarmer farmer = Multiplayer.getFarmer(clientId);
            if (farmer == null) return;
            if (spouse == null) return;

            if (spouse.EndsWith("engaged"))
            {
                ChatMenu.chat.Add(new ChatEntry(null, farmer.name + " is now engaged to " + spouse.Substring(0, spouse.Length - 7) + "."));
            }
            else
            {
                ChatMenu.chat.Add(new ChatEntry(null, farmer.name + " is now married to " + spouse + "."));
            }

        }
    }
}
