using StardewValley;
using StardewValleyMP.States;
using System.IO;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // An animal changed in a way other players need to know now
    public class FarmAnimalUpdatePacket : Packet
    {
        public long id;
        public FarmAnimalState state = new FarmAnimalState();

        public FarmAnimalUpdatePacket()
            : base(ID.FarmAnimalUpdate)
        {
        }

        public FarmAnimalUpdatePacket(FarmAnimal animal)
            : this()
        {
            id = animal.myID;
            state = new FarmAnimalState(animal);
        }

        protected override void read(BinaryReader reader)
        {
            id = reader.ReadInt64();
            state.name = reader.ReadString();
            state.reproduce = reader.ReadBoolean();
            state.fullness = reader.ReadByte();
            state.product = reader.ReadInt32();
            state.pet = reader.ReadBoolean();
            state.friendship = reader.ReadInt32();
            state.homeLoc.X = reader.ReadSingle();
            state.homeLoc.Y = reader.ReadSingle();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(id);
            writer.Write(state.name);
            writer.Write(state.reproduce);
            writer.Write(state.fullness);
            writer.Write(state.product);
            writer.Write(state.pet);
            writer.Write(state.friendship);
            writer.Write(state.homeLoc.X);
            writer.Write(state.homeLoc.Y);
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
            NPCMonitor.updateAnimal(id, state);
        }

        public override string ToString()
        {
            return base.ToString() + " " + id + " " + state;
        }
    }
}
