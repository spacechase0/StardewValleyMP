using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to a building
    public class BuildingPacket : Packet
    {
        public bool create;
        public string location;

        // Create only
        public string buildingStr;

        // Destroy only
        public string buildingId;

        public BuildingPacket()
            : base(ID.Building)
        {
        }

        public BuildingPacket(GameLocation loc, Building b)
            : this()
        {
            create = true;
            location = loc.name;
            buildingStr = Util.serialize<Building>(b);
        }

        public BuildingPacket(GameLocation loc, string id)
            : this()
        {
            create = false;
            location = loc.name;
            buildingId = id;
        }

        protected override void read(BinaryReader reader)
        {
            create = reader.ReadBoolean();
            location = reader.ReadString();

            if ( create )
            {
                buildingStr = reader.ReadString();
            }
            else
            {
                buildingId = reader.ReadString();
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(create);
            writer.Write(location);
            
            if ( create )
            {
                writer.Write(buildingStr);
            }
            else
            {
                writer.Write(buildingId);
            }
        }

        public override void process(Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(null, location);

            process();
        }

        public override void process( Server server, Server.Client client )
        {
            location = Multiplayer.processLocationNameForPlayerUnique(client.farmer, location);

            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            if ( !create )
            {
                if (!Multiplayer.locations.ContainsKey(location)) return;
                Multiplayer.locations[location].destroyBuilding(buildingId);
                return;
            }

            // See whining at DebrisPacket.process
            Type[] buildingTypes = new Type[]
            {
                typeof( Building ),
                typeof( Coop ),
                typeof( Stable ),
                typeof( Barn ),
                typeof( JunimoHut ),
                typeof( Mill ),
            };

            Building b = null;
            try
            {
                foreach (Type type in buildingTypes)
                {
                    if (buildingStr.IndexOf("<" + type.Name) != -1)
                    {
                        b = (Building)new XmlSerializer(type).Deserialize(Util.stringStream(buildingStr));
                        break;
                    }
                }
                if (b == null)
                {
                    int begin = buildingStr.IndexOf("<");
                    string type = buildingStr.Substring(begin + 1, buildingStr.IndexOf(" xmlns") - begin - 1);
                    Log.warn("!!! An unknown building type (" + type + ") was created and received by us.");
                    return;
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception deserializing building: " + e);
            }

            Multiplayer.locations[location].addBuilding(b);
            b.load();
        }

        public override string ToString()
        {
            return base.ToString() + " " + create + " " + location + " size:" + (buildingStr == null ? 0 : buildingStr.Length) + " " + buildingId;
        }
    }
}
