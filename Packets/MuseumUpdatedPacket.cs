using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValleyMP.Interface;
using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Tell everyone the museum has changed.
    public class MuseumUpdatedPacket : Packet
    {
        public byte clientId;
        public string name;
        public string artifacts;

        public MuseumUpdatedPacket()
            : base(ID.MuseumUpdated)
        {
        }

        public MuseumUpdatedPacket(GameLocation loc)
            : this()
        {
            clientId = Multiplayer.getMyId();
            name = loc.name;
            artifacts = Util.serialize( ( loc as LibraryMuseum ).museumPieces );
            message();
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            name = reader.ReadString();
            artifacts = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
            writer.Write(name);
            writer.Write(artifacts);
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
            LibraryMuseum museum = Game1.getLocationFromName(name) as LibraryMuseum;
            if (museum == null) return;

            museum.museumPieces = Util.deserialize<SerializableDictionary<Vector2, int>>( artifacts );
            Multiplayer.locations[name].updateMuseumCache();
            message();
        }

        private void message()
        {
            SFarmer fixer = Multiplayer.getFarmer(clientId);
            string fName = (fixer != null) ? fixer.name : null;

            ChatMenu.chat.Add(new ChatEntry(null, "The " + name + " has been changed" + (fName != null ? (" by " + fName) : "") + "."));
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " " + name + " size:" + (artifacts == null ? 0 : artifacts.Length);
        }
    }
}
