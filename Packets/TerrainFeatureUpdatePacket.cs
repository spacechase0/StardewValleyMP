using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.IO;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // A terrain feature is different
    public class TerrainFeatureUpdatePacket< TYPE > : Packet
        where TYPE : TerrainFeature
    {
        public static readonly ID typePacketId = thisPacketId();
        private static ID thisPacketId()
        {
            Type myType = typeof( TYPE );
            if (myType == typeof(HoeDirt)) return ID.HoeDirtUpdate;
            else if (myType == typeof(Tree)) return ID.TreeUpdate;
            else if (myType == typeof(FruitTree)) return ID.FruitTreeUpdate;

            throw new InvalidOperationException("TerrainFeatureUpdatePacket used for unsupported type (" + typeof( TYPE ) + ")!");
        }

        public string location;
        public float posX;
        public float posY;
        public string str;

        public TerrainFeatureUpdatePacket()
            : base(typePacketId)
        {
        }

        public TerrainFeatureUpdatePacket(GameLocation loc, Vector2 pos)
            : this()
        {
            location = Multiplayer.getUniqueLocationName(loc);
            posX = pos.X;
            posY = pos.Y;

            TerrainFeature tf = loc.terrainFeatures[pos];
            if (tf != null && tf.GetType() == typeof(TYPE))
            {
                str = Util.serialize<TYPE>(tf as TYPE);
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
            obj.loadSprite();

            LocationCache loc = Multiplayer.locations[location];
            loc.ignoreUpdates = true;
            loc.loc.terrainFeatures[pos] = obj;
            loc.getMonitor<TYPE>().recheck(pos);
            loc.ignoreUpdates = false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + location + " " + posX + " " + posY + " size:" + (str == null ? 0 : str.Length);
        }
    }
}
