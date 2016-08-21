using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.Locations;
using StardewModdingAPI;
using Microsoft.Xna.Framework;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Tell everyone the beach bridge is now fixed.
    public class BeachBridgeFixedPacket : Packet
    {
        public byte clientId;
        public string name;

        public BeachBridgeFixedPacket()
            : base(ID.BeachBridgeFixed)
        {
        }

        public BeachBridgeFixedPacket(GameLocation loc)
            : this()
        {
            clientId = Multiplayer.getMyId();
            name = loc.name;
            beachMessage();
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            name = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
            writer.Write( name );
        }

        public override void process(Client client)
        {
            process();
        }

        public override void process(Server server, Server.Client client)
        {
            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            Beach beach = Game1.getLocationFromName(name) as Beach;
            if (beach == null) return;

            beach.bridgeFixed = true;
            Multiplayer.locations[name].prevBridgeFixed = true;
            beachMessage();
        }

        private void beachMessage()
        {
            Farmer fixer = Multiplayer.getFarmer(clientId);
            string fName = (fixer != null) ? fixer.name : null;

            ChatMenu.chat.Add(new ChatEntry(null, "The bridge at the " + name + " is was fixed" + (fName != null ? (" by " + fName) : "") + "."));
        }
    }
}
