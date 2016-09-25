using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewModdingAPI;
using StardewValleyMP.Vanilla;

namespace StardewValleyMP.Packets
{
    // Server -> Client
    // Send the client their ID.
    // Also signals them to send their farmer info.
    public class YourIDPacket : Packet
    {
        public byte clientId;

        public YourIDPacket() : base( ID.YourID )
        {
        }

        public YourIDPacket(byte theId)
            : this()
        {
            clientId = theId;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
        }

        public override void process(Client client)
        {
            client.id = clientId;
            
            string text2 = NewLoadMenu.pendingSelected.name;//SaveGame.loaded.player.Name;
            string text3 = text2;
            for (int i = 0; i < text3.Length; i++)
            {
                char c = text3[i];
                if (!char.IsLetterOrDigit(c))
                {
                    text2 = text2.Replace(string.Concat(c), "");
                }
            }
            string text4 = text2 + "_" + Game1.uniqueIDForThisGame;
            String saveFile = NewLoadMenu.pendingSelected.favoriteThing;//SaveGame.loaded.uniqueIDForThisGame;
            string savePath = Path.Combine(new string[]
			    {
				    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				    "StardewValley",
				    "Saves",
				    saveFile,
				    saveFile,//"SaveGameInfo"
			    });
            String xml = File.ReadAllText(savePath);
            ClientFarmerDataPacket farmerData = new ClientFarmerDataPacket(xml);
            client.send(farmerData);

            client.stage = Client.NetStage.WaitingForWorldData;
        }
    }
}
