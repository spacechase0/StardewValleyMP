using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using System.Xml;
using System.Xml.Serialization;
using StardewValley.Buildings;
using StardewValleyMP.States;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to a building
    public class BuildingUpdatePacket : Packet
    {
        public string location;
        public string buildingId;
        public BuildingState state = new BuildingState();

        public BuildingUpdatePacket()
            : base(ID.BuildingUpdate)
        {
        }

        public BuildingUpdatePacket(GameLocation loc, Building b)
            : this()
        {
            location = loc.name;
            buildingId = b.nameOfIndoors;
            state = new BuildingState( b );
        }

        protected override void read(BinaryReader reader)
        {
            location = reader.ReadString();
            buildingId = reader.ReadString();
            state.door = reader.ReadBoolean();
            state.upgrade = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(buildingId);
            writer.Write(state.door);
            writer.Write(state.upgrade);
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
            Multiplayer.locations[location].updateBuilding(buildingId, state);
        }
    }
}
