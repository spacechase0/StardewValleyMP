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
    // Tell the the other who is holding what
    // A client can only tell the server what THEY are holding
    public class HeldItemPacket : Packet
    {
        public byte clientId;
        public int index;
        public bool big;
        public int tool;

        public HeldItemPacket()
            : base(ID.HeldItem)
        {
        }

        public HeldItemPacket(byte theId, Farmer farmer)
            : this()
        {
            clientId = theId;
            if (farmer.ActiveObject == null)
            {
                index = -1;
                big = false;
            }
            else
            {
                index = farmer.ActiveObject.ParentSheetIndex;
                big = farmer.ActiveObject.bigCraftable;
            }
            tool = farmer.CurrentToolIndex;
            //Log.Async(farmer.Name + " holding " + tool);
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            index = reader.ReadInt32();
            big = reader.ReadBoolean();
            tool = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write( clientId );
            writer.Write(index);
            writer.Write(big);
            writer.Write(tool);
        }

        public override void process(Client client)
        {
            Farmer farmer = client.others[clientId];
            if (farmer == null) return;

            doFarmer(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            if (clientId != client.id) return;

            doFarmer(client.farmer);
            server.broadcast(this, clientId);
        }

        private void doFarmer( Farmer farmer )
        {
            if (index == -1)
                farmer.ActiveObject = null;
            else
                farmer.ActiveObject = ( big ? new StardewValley.Object(Vector2.Zero, index, false) : new StardewValley.Object(Vector2.Zero, index, 1));
            //farmer.CurrentToolIndex = tool;
        }
    }
}
