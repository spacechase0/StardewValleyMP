using StardewValley;
using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Tell the server we are in a new location.
    // A client can only tell the server which way THEY are going
    public class LocationPacket : Packet
    {
        public byte clientId;
        public string name;

        public LocationPacket()
            : base(ID.Location)
        {
        }

        public LocationPacket( byte theId, string theName ) : this()
        {
            clientId = theId;
            name = theName;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            name = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write( clientId );
            writer.Write(name);
        }

        public override void process(Client client)
        {
            name = Multiplayer.processLocationNameForPlayerUnique(null, name);

            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;
            Log.debug(farmer.name + " moved to " + name + " (" + Game1.getLocationFromName(name) + ")");

            process(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            name = Multiplayer.processLocationNameForPlayerUnique(client.farmer, name);

            if (clientId != client.id) return;
            Log.debug(client.farmer.name + " moved to " + name + " (" + Game1.getLocationFromName(name) + ")");

            process(client.farmer);

            server.broadcast(this, clientId);
        }
        
        private void process( SFarmer target )
        {
            if (target.currentLocation != null)
                target.currentLocation.farmers.Remove(target);

            if (name == Game1.whereIsTodaysFest)
            {
                Multiplayer.goingToFestival = true;
                /*try
                {
                    Event @event = new Event();
                    string festival = Game1.currentSeason + Game1.dayOfMonth;
                    if (@event.tryToLoadFestival(festival))
                    {
                        this.currentEvent = @event;
                    }
                }
                catch (Exception)
                {
                    if (loc.name == "Temp")
                    {
                        Dictionary<string, string> data = (Dictionary<string, string>)Util.GetInstanceField(typeof(Event), Game1.CurrentEvent, "festivalData");
                        string[] array = data["set-up"].Split('/')[2].Split(' ');
                        for (int i = 0; i < array.Length; ++i)
                        {
                            if (array[i] != "farmer") continue;
                            Game1.player.position.X = (float)(Convert.ToInt32(array[i + 1]) * Game1.tileSize);
                            Game1.player.position.Y = (float)(Convert.ToInt32(array[i + 2]) * Game1.tileSize + Game1.tileSize / 4);
                            Game1.player.faceDirection(Convert.ToInt32(array[i + 3]));
                            Game1.player.FarmerSprite.StopAnimation();
                            break;
                        }
                    }
                }*/
                Game1.warpFarmer(name, (int)(target.position.X / Game1.tileSize), (int)(target.position.Y / Game1.tileSize), false);
            }
            
            if (name == "Temp")
            {
                // Something weird happens with this, like it doesn't exist. So
                // I'm just waiting until the local player gets there and move
                // everyone then
            }
            else if (!name.Equals("UndergroundMine")) // It glitches out a bunch
            {
                // <building stuff>
                target.currentLocation = Game1.getLocationFromName(name);
            }

            target.currentLocation.farmers.Add(target);
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " " + name;
        }
    }
}
