using Microsoft.Xna.Framework;
using StardewValley;
using System.IO;
using Object = StardewValley.Object;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // A fence changed.
    // Fences get a custom packet because it crashes when drawing a gate that
    // was updated through the normal ObjectUpdatePacket.
    // I hope there aren't any other variables I need to watch.
    public class FenceUpdatePacket : Packet
    {
        public string location;
        public float posX;
        public float posY;
        public int gatePos;

        public FenceUpdatePacket()
            : base( ID.FenceUpdate)
        {
        }

        public FenceUpdatePacket(GameLocation loc, Vector2 pos)
            : this()
        {
            location = Multiplayer.getUniqueLocationName(loc); ;
            posX = pos.X;
            posY = pos.Y;

            Object obj = loc.objects[pos];
            if (obj != null && obj.GetType() == typeof(Fence))
            {
                gatePos = (obj as Fence).gatePosition;
            }
        }

        protected override void read(BinaryReader reader)
        {
            location = reader.ReadString();
            posX = reader.ReadSingle();
            posY = reader.ReadSingle();
            gatePos = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(gatePos);
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
            LocationCache loc = Multiplayer.locations[location];

            Vector2 pos = new Vector2(posX, posY);
            if (!loc.loc.objects.ContainsKey(pos))
            {
                return;
            }
            Object obj = loc.loc.objects[pos];

            if ( obj.GetType() != typeof( Fence ) )
            {
                return;
            }
            Fence fence = (Fence)obj;

            loc.ignoreUpdates = true;
            fence.gatePosition = gatePos;
            loc.getMonitor< Fence >().recheck(pos);
            loc.ignoreUpdates = false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + location + " " + posX + " " + posY + " " + gatePos;
        }
    }
}
