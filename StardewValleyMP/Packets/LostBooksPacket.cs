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
    // Update the amount of lost books found.
    public class LostBooksPacket : Packet
    {
        public int bookCount;

        public LostBooksPacket()
            : base(ID.LostBooks)
        {
            bookCount = 0;
            if (Game1.player.archaeologyFound.ContainsKey(102))
                bookCount = Game1.player.archaeologyFound[102][0];
        }

        protected override void read(BinaryReader reader)
        {
            bookCount = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(bookCount);
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
            if ( !Game1.player.archaeologyFound.ContainsKey( 102 ) )
            {
                Game1.player.archaeologyFound.Add(102, new int[] { 0, 0 });
            }
            Game1.player.archaeologyFound[102][0] = bookCount;
            Game1.player.archaeologyFound[102][1] = bookCount; // No idea what the second is for, but LibraryMuseum.foundArtifact increments both.
            Multiplayer.prevBooks = bookCount;
        }
    }
}
