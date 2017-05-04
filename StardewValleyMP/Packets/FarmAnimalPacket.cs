using StardewValley;
using StardewValley.Buildings;
using System;
using System.IO;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to a farm animal
    public class FarmAnimalPacket : Packet
    {
        public bool create;
        public string location;

        // Create only
        public string animalStr;

        // Destroy only
        public long animalId;

        public FarmAnimalPacket()
            : base(ID.FarmAnimal)
        {
        }

        public FarmAnimalPacket(FarmAnimal a)
            : this()
        {
            create = true;
            location = a.home.nameOfIndoors;
            animalStr = Util.serialize<FarmAnimal>(a);
        }

        public FarmAnimalPacket(Building prevHome, long id)
            : this()
        {
            create = false;
            location = prevHome.nameOfIndoors;
            animalId = id;
        }

        protected override void read(BinaryReader reader)
        {
            create = reader.ReadBoolean();
            location = reader.ReadString();

            if ( create )
            {
                animalStr = reader.ReadString();
            }
            else
            {
                animalId = reader.ReadInt64();
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(create);
            writer.Write(location);
            
            if ( create )
            {
                writer.Write(animalStr);
            }
            else
            {
                writer.Write(animalId);
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
                NPCMonitor.destroyAnimal(animalId);
                return;
            }

            FarmAnimal a = null;
            try
            {
                a = Util.deserialize< FarmAnimal >( animalStr );
            }
            catch ( Exception e)
            {
                Log.error("Exception deserializing farm animal: " + e);
            }

            NPCMonitor.addAnimal( location, a );
            a.reload();
        }

        public override string ToString()
        {
            return base.ToString() + " " + create + " " +location + " size:" + (animalStr == null ? 0 : animalStr.Length) + " " + animalId;
        }
    }
}
