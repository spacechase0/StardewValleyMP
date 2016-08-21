using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValleyMP.Vanilla;
using StardewModdingAPI;

namespace StardewValleyMP.Packets
{
    // Client -> Server
    // Send signal that we are waiting for the next day.
    // Server -> Client
    // Tell them to proceed to the next day.
    public class NextDayPacket : Packet
    {
        //string clientXml;

        public NextDayPacket() : base( ID.NextDay )
        {
        }

        /*public NextDayPacket(string theXml)
            : base(ID.NextDay)
        {
            clientXml = theXml;
        }*/

        protected override void read(BinaryReader reader)
        {
            //clientXml = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            //writer.Write(clientXml);
        }

        public override void process(Client client)
        {
            Log.Async("Ready to start next day");
            client.stage = Client.NetStage.Playing;
            client.tempStopUpdating = true;
        }

        public override void process(Server server, Server.Client client)
        {
            ChatMenu.chat.Add(new ChatEntry(null, client.farmer.name + " is in bed."));
            server.broadcast(new ChatPacket(255, client.farmer.name + " is in bed."), client.id);

            client.stage = Server.Client.NetStage.WaitingForStart;
            if (client.farmer.currentLocation != null)
                ;// client.farmer.currentLocation.farmers.Remove(client.farmer);
        }
    }
}
