using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using StardewModdingAPI;

namespace StardewValleyMP.Packets
{
    public enum ID
    {
        Version = 0,
        YourID,
        ClientFarmerData,
        OtherFarmerData,
        WorldData,
        MovingState,
        Location,
        Animation,
        HeldItem,
        Debris,
        TerrainFeature, // 10
        Object,
        HoeDirtUpdate,
        FruitTreeUpdate,
        ObjectUpdate,
        DoorUpdate,
        FenceUpdate,
        CrabPotUpdate,
        ChestUpdate,
        PauseTime,
        TimeSync, //20
        CoopUpdate,
        FestivalDancePartner,
        ContinueEvent,
        NextDay,
        ShippingBin,
        NPCUpdate,
        FarmAnimalUpdate,
        Spouse,
        Chat,
        BeachBridgeFixed, // 30
        MuseumUpdated,
        CommunityCenterUpdated,
        TreeUpdate,
        FarmUpdate,
        Building,
        BuildingUpdate,
        FarmAnimal,
        LatestId,
        // Stuff...
    };

    public abstract class Packet
    {
        protected Packet( ID theId )
        {
            id = theId;
        }

        public readonly ID id;

        protected abstract void read(BinaryReader reader);
        protected abstract void write( BinaryWriter writer );
        public virtual void process(Client client) { }
        public virtual void process(Server server, Server.Client client) { }

        public static Packet readFrom( Stream s )
        {
            BinaryReader reader = new BinaryReader( s );
            byte type = reader.ReadByte();

            Packet packet = null;
            switch ( type )
            {
                case (byte)ID.Version: packet = new VersionPacket(); break;
                case (byte)ID.YourID: packet = new YourIDPacket(); break;
                case (byte)ID.ClientFarmerData: packet = new ClientFarmerDataPacket(); break;
                case (byte)ID.OtherFarmerData: packet = new OtherFarmerDataPacket(); break;
                case (byte)ID.WorldData: packet = new WorldDataPacket(); break;
                case (byte)ID.MovingState: packet = new MovingStatePacket(); break;
                case (byte)ID.Location: packet = new LocationPacket(); break;
                case (byte)ID.Animation: packet = new AnimationPacket(); break;
                case (byte)ID.HeldItem: packet = new HeldItemPacket(); break;
                case (byte)ID.Debris: packet = new DebrisPacket(); break;
                case (byte)ID.TerrainFeature: packet = new TerrainFeaturePacket(); break;
                case (byte)ID.Object: packet = new ObjectPacket(); break;
                case (byte)ID.HoeDirtUpdate: packet = new TerrainFeatureUpdatePacket<HoeDirt>(); break;
                case (byte)ID.FruitTreeUpdate: packet = new TerrainFeatureUpdatePacket<FruitTree>(); break;
                case (byte)ID.ObjectUpdate: packet = new ObjectUpdatePacket<StardewValley.Object>(); break;
                case (byte)ID.DoorUpdate: packet = new ObjectUpdatePacket<Door>(); break;
                case (byte)ID.FenceUpdate: packet = new FenceUpdatePacket(); break;
                case (byte)ID.CrabPotUpdate: packet = new ObjectUpdatePacket<CrabPot>(); break;
                case (byte)ID.ChestUpdate: packet = new ChestUpdatePacket(); break;
                case (byte)ID.PauseTime: packet = new PauseTimePacket(); break;
                case (byte)ID.TimeSync: packet = new TimeSyncPacket(); break;
                case (byte)ID.CoopUpdate: packet = new CoopUpdatePacket(); break;
                case (byte)ID.FestivalDancePartner: packet = new FestivalDancePartnerPacket(); break;
                case (byte)ID.ContinueEvent: packet = new ContinueEventPacket(); break;
                case (byte)ID.NextDay: packet = new NextDayPacket(); break;
                case (byte)ID.ShippingBin: packet = new ShippingBinPacket(); break;
                case (byte)ID.NPCUpdate: packet = new NPCUpdatePacket(); break;
                case (byte)ID.FarmAnimalUpdate: packet = new FarmAnimalUpdatePacket(); break;
                case (byte)ID.Spouse: packet = new SpousePacket(); break;
                case (byte)ID.Chat: packet = new ChatPacket(); break;
                case (byte)ID.BeachBridgeFixed: packet = new BeachBridgeFixedPacket(); break;
                case (byte)ID.MuseumUpdated: packet = new MuseumUpdatedPacket(); break;
                case (byte)ID.CommunityCenterUpdated: packet = new CommunityCenterUpdatedPacket(); break;
                case (byte)ID.TreeUpdate: packet = new TerrainFeatureUpdatePacket<Tree>(); break;
                case (byte)ID.FarmUpdate: packet = new FarmUpdatePacket(); break;
                case (byte)ID.Building: packet = new BuildingPacket(); break;
                case (byte)ID.BuildingUpdate: packet = new BuildingUpdatePacket(); break;
                case (byte)ID.FarmAnimal: packet = new FarmAnimalPacket(); break;
                case (byte)ID.LatestId: packet = new LatestIdPacket(); break;
            }

            if (packet == null)
            {
                Log.Async("Bad packet?!?!?!?! ID = " + type);
                return null;
            }
            //Log.Async("Got packet " + type);
            packet.read(reader);

            return packet;
        }

        public void writeTo( Stream s )
        {
            BinaryWriter writer = new BinaryWriter(s);
            writer.Write((byte)id);
            write(writer);
        }
    }
}
