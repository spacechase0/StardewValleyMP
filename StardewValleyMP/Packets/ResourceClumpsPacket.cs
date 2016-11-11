//zzhack
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // Synchronize the state of ResourceClumps (hardwood logs, meteorite, boulders, etc)
    public class ResourceClumpsPacket : Packet
    {
        public byte map;
        public int hash;

        public const byte MAP_WOODS = 0;
        public const byte MAP_FOREST = 1;
        public const byte MAP_FARM = 2;

        public ResourceClumpsPacket(byte map, int hash)
            : base(ID.ResourceClumps)
        {
            this.map = map;
            this.hash = hash;
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
            map = reader.ReadByte();
            hash = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(map);
            writer.Write(hash);
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

            switch (map)
            {
                case MAP_FOREST:
                    //only the log here, sooo.
                    ((Forest)Game1.getLocationFromName("Forest")).log = null;
                    break;
                case MAP_WOODS:
                    //check the stumps in the woods
                    Woods map_woods = (Woods)Game1.getLocationFromName("Woods");
                    for (int i = 0; i <= map_woods.stumps.Count - 1; i++)
                    {
                        int vx = map_woods.stumps[i].getBoundingBox(map_woods.stumps[i].tile).X;
                        int vy = map_woods.stumps[i].getBoundingBox(map_woods.stumps[i].tile).Y;
                        if (hashVec2(vx, vy) == hash)
                        {
                            //we have found a removed stump! let's kill it :D
                            map_woods.stumps.RemoveAt(i);
                        }
                    }
                    break;
                case MAP_FARM:
                    //check the resource clumps on the farm
                    Farm map_farm = (Farm)Game1.getLocationFromName("Farm");
                    for (int i = 0; i <= map_farm.resourceClumps.Count - 1; i++)
                    {
                        int vx = map_farm.resourceClumps[i].getBoundingBox(map_farm.resourceClumps[i].tile).X;
                        int vy = map_farm.resourceClumps[i].getBoundingBox(map_farm.resourceClumps[i].tile).Y;
                        if (hashVec2(vx, vy) == hash)
                        {
                            //we have found a removed resource clump! let's kill it :D
                            map_farm.resourceClumps.RemoveAt(i);
                        }
                    }
                    break;
            }



        }

    }
}
