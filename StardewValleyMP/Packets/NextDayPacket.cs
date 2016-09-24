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

            // This is also set in ClientFarmerDataPacket.
            // I think what's happening is this packet is received, and it goes to the next day.
            // When the next day starts it sees the ClientFarmerDataPacket, and resets this same thing.
            // So the host is ignoring everything the clients do, and queueing their messages up until
            // whenever the stage is set back to Playing. Then all of the clients changes come out at once.
            // The clients are oblivious and still receiving the host updates though.
            //
            // (Of course, I can't reproduce this since I assume it is latency related again, made even
            // weirder with the receiving being on another thread.)
            //
            // Hopefully only setting this in the latter packet will fix this.
            //
            // Actually, that^ might be completely wrong.

            //client.stage = Server.Client.NetStage.WaitingForStart;
            if (client.farmer.currentLocation != null)
                ;// client.farmer.currentLocation.farmers.Remove(client.farmer);
        }
    }
}
