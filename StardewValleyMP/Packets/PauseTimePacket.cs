using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // Freeze the world because someone is in a cutscene or something.
    public class PauseTimePacket : Packet
    {
        public bool pause;

        public PauseTimePacket()
            : base(ID.PauseTime)
        {
            pause = Game1.freezeControls;
        }

        protected override void read(BinaryReader reader)
        {
            pause = reader.ReadBoolean();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(pause);
        }

        public override void process(Client client)
        {
            process();
        }

        public override void process( Server server, Server.Client client )
        {
            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            Game1.freezeControls = Multiplayer.prevFreezeControls = pause;
        }
    }
}
