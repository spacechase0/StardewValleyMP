using StardewValley;
using StardewValley.Buildings;
using StardewValleyMP.States;
using System.IO;

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
            state.x = reader.ReadInt32();
            state.y = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(buildingId);
            writer.Write(state.door);
            writer.Write(state.upgrade);
            writer.Write(state.x);
            writer.Write(state.y);
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

        public override string ToString()
        {
            return base.ToString() + " " + location + " " + buildingId + " " + state;
        }
    }
}
