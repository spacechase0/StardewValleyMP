//zzhack
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System.IO;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // Synchronize the state of ResourceClumps (hardwood logs, meteorite, boulders, etc)
    public class ResourceClumpsPacket : Packet
    {
        public string location;
        public int hash;

        public ResourceClumpsPacket()
            : base(ID.ResourceClumps)
        {
        }

        public ResourceClumpsPacket(GameLocation loc, ResourceClump clump)
            : base(ID.ResourceClumps)
        {
            this.location = loc.name;
            this.hash = hashVec2(clump);
        }

        public ResourceClumpsPacket(GameLocation loc, int hash)
            : base(ID.ResourceClumps)
        {
            this.location = loc.name;
            this.hash = hash;
        }

        public static int hashVec2(ResourceClump clump)
        {
            return hashVec2((int)clump.tile.X, (int)clump.tile.Y);
        }

        public static int hashVec2(int x, int y)
        {
            int res = 17;
            res = ((res + x) << 5) - (res + x);
            res = ((res + y) << 5) - (res + y);
            return res;
        }

        protected override void read(BinaryReader reader)
        {
            location = reader.ReadString();
            hash = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(hash);
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
            GameLocation loc = Game1.getLocationFromName(location);
            LocationCache cache = Multiplayer.locations[location];
            if ( loc is Forest )
            {
                //only the log here, sooo.
                ((Forest) loc).log = null;
                cache.prevForestLog = null;
            }
            else if ( loc is Woods )
            {
                //check the stumps in the woods
                Woods map_woods = (Woods)loc;
                foreach (var clump in map_woods.stumps)
                {
                    if (hashVec2(clump) == hash)
                    {
                        //we have found a removed stump! let's kill it :D
                        map_woods.stumps.Remove(clump);
                        cache.updateClumpsCache( map_woods.stumps );
                        break;
                    }
                }
            }
            else if ( loc is Farm )
            {
                //check the resource clumps on the farm
                Farm map_farm = (Farm)loc;
                foreach (var clump in map_farm.resourceClumps)
                {
                    if (hashVec2(clump) == hash)
                    {
                        //we have found a removed resource clump! let's kill it :D
                        map_farm.resourceClumps.Remove(clump);
                        cache.updateClumpsCache(map_farm.resourceClumps);
                        break;
                    }
                }
            }
        }
        
        public override string ToString()
        {
            return base.ToString() + " " + location + " " + hash;
        }
    }
}
