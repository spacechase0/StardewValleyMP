using StardewValley;
using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Tell everyone our new animation state
    // A client can only tell the server what THEY are doing.
    public class AnimationPacket : Packet
    {
        public byte clientId;
        public int anim;
        public float interval;
        public int frames;
        public bool backwards;
        public int tool;

        public AnimationPacket()
            : base(ID.Animation)
        {
        }

        public AnimationPacket(byte theId, SFarmer farmer)
            : this()
        {
            clientId = theId;
            anim = (int)Util.GetInstanceField(typeof(FarmerSprite), farmer.FarmerSprite, "currentSingleAnimation");
            interval = farmer.FarmerSprite.currentSingleAnimationInterval;
            frames = ( farmer.FarmerSprite.CurrentAnimation != null ) ? farmer.FarmerSprite.CurrentAnimation.Count : 0; // Not sure what the proper solution is
            backwards = farmer.FarmerSprite.animateBackwards;
            tool = (farmer.CurrentTool != null) ? farmer.CurrentTool.currentParentTileIndex : -1;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            anim = reader.ReadInt32();
            interval = reader.ReadSingle();
            frames = reader.ReadInt32();
            backwards = reader.ReadBoolean();
            tool = reader.ReadInt32();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write( clientId );
            writer.Write(anim);
            writer.Write(interval);
            writer.Write(frames);
            writer.Write(backwards);
            writer.Write(tool);
        }

        public override void process(Client client)
        {
            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;

            doFarmer(farmer);
        }

        public override void process(Server server, Server.Client client)
        {
            if (clientId != client.id) return;

            doFarmer(client.farmer);
            server.broadcast(this, clientId);
        }

        private void doFarmer( SFarmer farmer )
        {
            //Log.Async(farmer.Name + " animation " + anim + ", " + interval + ", " + frames + ", " + tool);

            farmer.FarmerSprite.CurrentToolIndex = tool;
                farmer.FarmerSprite.animateOnce(anim, interval, frames);
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " " + anim + " " + interval + " " + frames + " " + backwards + " " + tool;
        }
    }
}
