using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.IO;
using Object = StardewValley.Object;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // An object is different
    public class ObjectUpdatePacket< TYPE > : Packet
        where TYPE : Object
    {
        public static readonly ID typePacketId = thisPacketId();
        private static ID thisPacketId()
        {
            Type myType = typeof(TYPE);
            if (myType == typeof(Door)) return ID.DoorUpdate;
            else if (myType == typeof(Object)) return ID.ObjectUpdate;
            else if (myType == typeof(CrabPot)) return ID.CrabPotUpdate;
            else if (myType == typeof(Chest)) return ID.ChestUpdate;

            throw new InvalidOperationException("ObjectUpdatePacket used for unsupported type (" + typeof( TYPE ) + ")!");
        }

        public string location;
        public float posX;
        public float posY;
        public string str;

        public ObjectUpdatePacket()
            : base(typePacketId)
        {
        }

        public ObjectUpdatePacket(GameLocation loc, Vector2 pos)
            : this()
        {
            location = Multiplayer.getUniqueLocationName(loc); ;
            posX = pos.X;
            posY = pos.Y;

            Object obj = loc.objects[pos];
            if (obj != null && obj.GetType() == typeof(TYPE))
            {
                str = Util.serialize<TYPE>(obj as TYPE);
            }
        }

        protected override void read(BinaryReader reader)
        {
            location = reader.ReadString();
            posX = reader.ReadSingle();
            posY = reader.ReadSingle();
            str = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(str);
        }

        public override void process(Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(null, location);

            process();
        }

        public override void process(Server server, Server.Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(client.farmer, location);

            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            if ( !Multiplayer.locations.ContainsKey( location ) )
            {
                return;
            }

            Vector2 pos = new Vector2(posX, posY);
            TYPE obj = Util.deserialize< TYPE >( str );
            obj.reloadSprite();
            LocationCache loc = Multiplayer.locations[location];

            loc.ignoreUpdates = true;
            loc.loc.objects[pos] = obj;
            loc.getMonitor<TYPE>().recheck(pos);
            loc.ignoreUpdates = false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + location + " " + posX + " " + posY + " size:" + (str == null ? 0 : str.Length);
        }
    }
}
