using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValleyMP.States;
using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // A chest was opened/closed
    public class ChestUpdatePacket : ObjectUpdatePacket< Chest >
    {
        public int opener;

        public ChestUpdatePacket()
            : base()
        {
        }

        public ChestUpdatePacket(GameLocation loc, Vector2 pos)
            : base( loc, pos )
        {
            Chest chest = (Chest)loc.objects[pos];
            ChestState state = new ChestState( chest ); // Slightly cheating, but whatever
            opener = state.opener;

            // The chest is open, nobody else can open it. So why should we send
            // the reserialized chest? It'd be a huge waste of bandwidth (although
            // I do plenty of that already).
            // However, when it closes (opener == -1), we need to know what changed
            // for when someone else opens it.
            // I guess I could just overwrite read/write, but I like this better.
            // I don't have copy the contents of read/write, just add to it
            if ( opener != -1 )
            {
                str = "";
            }
        }

        protected override void read(BinaryReader reader)
        {
            base.read(reader);
            opener = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            base.write(writer);
            writer.Write(opener);
        }

        public override void process(Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(null, location);

            process();
        }

        public override void process(Server server, Server.Client client)
        {
			//redirect the packet to all connected clients to avoid item/data loss
			server.broadcast(this, client.id);
            location = Multiplayer.processLocationNameForPlayerUnique(client.farmer, location);

            process();
        }

        private void process()
        {
            if ( !Multiplayer.locations.ContainsKey( location ) )
            {
                return;
            }
            
            LocationCache loc = Multiplayer.locations[location];
            Vector2 pos = new Vector2(posX, posY);

            Chest obj = null;
            if ( opener != -1 ) // Opened
            {
                obj = (Chest)loc.loc.objects[pos];

                // If two people open a chest at the same it glitches out. It closes for both people,
                // but shows open even though nobody has it open, and nobody can open it. Clients 
                // MIGHT be able  to open it the next day (which would fix the issue), but I haven't
                // tested that.
                SFarmer farmer = (SFarmer ) Util.GetInstanceField(typeof(Chest), obj, "opener" );
                if (farmer != null)
                {
                    // Somebody already has it open. Normally you can't open a chest if someone
                    // else has it open. But this is possible (like I mentioned above).
                    // Not really sure what to do here.
                    // For now, just ignore people who open it and have a higher ID.
                    // This way it always results in one person having the chest open.
                    // I can't just close it for us. Somehow that happens anyways when each person
                    // gets this packet. Doing it this way means whoever has the lower ID keeps
                    // it open, but the higher ID gets it closed. It should then behave normally,
                    // as if the lower ID was the only one to open it. Hopefully?
                    if ( Multiplayer.getFarmerId( farmer ) > Multiplayer.getMyId() )
                    {
                        return;
                    }
                }

                // When opener is set, nobody can open the chest (vanilla).
                Util.SetInstanceField(typeof(Chest), obj, "opener", Multiplayer.getFarmer((byte)opener));

                // Do the animation manually. It pops open instead of being smooth, but fixing
                // that would probably be a lot more work.
                // Setting frameCounter to -2 keeps the opening animation (which includes
                // opening the menu) from triggering (checks > -1), but prevents the closing
                // animation (which also clears opener) from triggering (checks == -1).
                obj.frameCounter = -2;
                obj.currentLidFrame = 135;
                if (Game1.currentLocation != null && loc.loc.name == Game1.currentLocation.name ) 
                    Game1.playSound("openChest");
            }
            else // Closed
            {
                obj = Util.deserialize<Chest>(str);
                obj.currentLidFrame = 135;
                obj.frameCounter = 2;
                if (Game1.currentLocation != null && loc.loc.name == Game1.currentLocation.name) 
				    Game1.playSound("doorCreakReverse");
            }

            loc.ignoreUpdates = true;
            loc.loc.objects[pos] = obj;
            loc.getMonitor<Chest>().recheck(pos);
            loc.ignoreUpdates = false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + opener;
        }
    }
}
