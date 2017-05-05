using StardewValley;
using StardewValleyMP.Vanilla;
using System.Collections.Generic;
using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Server -> Client
    // Tell the client about everyone else.
    public class OtherFarmerDataPacket : Packet
    {
        public Dictionary< byte, string > others = new Dictionary< byte, string >();

        public OtherFarmerDataPacket()
            : base(ID.OtherFarmerData)
        {
        }

        protected override void read(BinaryReader reader)
        {
            others.Clear();

            byte amount = reader.ReadByte();
            for ( byte i = 0; i < amount; ++i )
            {
                byte id = reader.ReadByte();
                string data = reader.ReadString();
                others.Add(id, data);
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write((byte)others.Count);
            foreach ( KeyValuePair< byte, string > entry in others )
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
        }

        public override void process( Client client )
        {
            Log.debug("Got farmer data for other players.");

            foreach (SFarmer farmer in client.others.Values)
            {
                if (farmer.currentLocation != null)
                    farmer.currentLocation.farmers.Remove(farmer);
            }
            client.others.Clear();

            foreach (KeyValuePair<byte, string> other in others)
            {
                SFarmer farmer = (SFarmer)SaveGame.farmerSerializer.Deserialize(Util.stringStream(other.Value));
                farmer.uniqueMultiplayerID += 1 + client.id; // For IsMainPlayer

                //SFarmer oldPlayer = Game1.player;
                NewSaveGame.loadDataToFarmer(farmer);
                //Game1.player = oldPlayer; // Seriously, why does this get reassigned in there?

                client.others.Add(other.Key, farmer);

                if ( other.Key == 0 )
                {
                    foreach ( string mail in Multiplayer.checkMail )
                    {
                        if (farmer.mailReceived.Contains(mail) && !SaveGame.loaded.player.mailReceived.Contains(mail))
                            SaveGame.loaded.player.mailReceived.Add(mail);
                        if (farmer.mailForTomorrow.Contains(mail) && !SaveGame.loaded.player.mailForTomorrow.Contains(mail))
                            SaveGame.loaded.player.mailForTomorrow.Add(mail);
                        if (farmer.mailReceived.Contains(mail + "%&NL&%") && !SaveGame.loaded.player.mailReceived.Contains(mail + "%&NL&%"))
                            SaveGame.loaded.player.mailReceived.Add(mail + "%&NL&%");
                        if (farmer.mailForTomorrow.Contains(mail + "%&NL&%") && !SaveGame.loaded.player.mailForTomorrow.Contains(mail + "%&NL&%"))
                            SaveGame.loaded.player.mailForTomorrow.Add(mail + "%&NL&%");
                    }
                    // Maybe more
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + others.Count;
        }
    }
}
