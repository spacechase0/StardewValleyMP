using System.IO;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    enum MovementFlags
    {
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        Running = 1 << 4,
    }

    // Client <-> Server
    // Sends the movement state of a player.
    // A client can only tell the server which way THEY are going
    public class MovingStatePacket : Packet
    {
        public byte clientId;
        public byte flags;
        public float x, y;

        public MovingStatePacket() : base( ID.MovingState )
        {
        }

        public MovingStatePacket( byte theId, SFarmer player ) : this()
        {
            clientId = theId;
            flags = 0;
            if (player.movementDirections.Contains(3)) flags |= ( byte ) MovementFlags.Left;
            if (player.movementDirections.Contains(1)) flags |= (byte)MovementFlags.Right;
            if (player.movementDirections.Contains(0)) flags |= (byte)MovementFlags.Up;
            if (player.movementDirections.Contains(2)) flags |= (byte)MovementFlags.Down;
            if (player.running) flags |= (byte)MovementFlags.Running;
            x = player.position.X;
            y = player.position.Y;
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            flags = reader.ReadByte();
            x = reader.ReadSingle();
            y = reader.ReadSingle();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
            writer.Write(flags);
            writer.Write(x);
            writer.Write(y);
        }

        public override void process(Client client)
        {
            SFarmer farmer = client.others[clientId];
            if (farmer == null) return;

            //Log.Async("Movement " + flags + " " + x + " " + y);
            doFarmer(farmer);

        }

        public override void process( Server server, Server.Client client )
        {
            if (clientId != client.id) return;

            //Log.Async("Movement " + flags + " " + x + " " + y);
            doFarmer(client.farmer);

            server.broadcast( this, clientId );
        }

        private void doFarmer( SFarmer farmer )
        {
            farmer.SetMovingLeft((flags & (byte)MovementFlags.Left) != 0);
            farmer.SetMovingRight((flags & (byte)MovementFlags.Right) != 0);
            farmer.SetMovingUp((flags & (byte)MovementFlags.Up) != 0);
            farmer.SetMovingDown((flags & (byte)MovementFlags.Down) != 0);
            farmer.setRunning((flags & (byte)MovementFlags.Running) != 0, true);
            farmer.position.X = x;
            farmer.position.Y = y;
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " " + flags + " " + x + " " + y;
        }
    }
}
