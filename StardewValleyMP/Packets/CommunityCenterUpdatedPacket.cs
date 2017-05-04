using StardewValley;
using StardewValley.Locations;
using StardewValleyMP.Interface;
using System.IO;
using System.Linq;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Tell everyone the community center has changed.
    public class CommunityCenterUpdatedPacket : Packet
    {
        public enum Reason
        {
            BundlesProgress = 0,
            BundlesCompleted = 1,
            BundlesRewards = 2,
        }

        public byte clientId;
        public string name;
        public byte reason;

        public string xml;
        public bool[] completed;

        public CommunityCenterUpdatedPacket()
            : base(ID.CommunityCenterUpdated)
        {
        }

        public CommunityCenterUpdatedPacket(GameLocation loc, SerializableDictionary<int, bool[]> progress)
            : this()
        {
            clientId = Multiplayer.getMyId();
            name = loc.name;
            reason = ( byte ) Reason.BundlesProgress;
            xml = Util.serialize(progress);
            message();
        }

        public CommunityCenterUpdatedPacket(GameLocation loc, bool[] theCompleted)
            : this()
        {
            clientId = Multiplayer.getMyId();
            name = loc.name;
            reason = (byte)Reason.BundlesCompleted;
            completed = theCompleted;
            message();
        }

        public CommunityCenterUpdatedPacket(GameLocation loc, SerializableDictionary<int, bool> rewards)
            : this()
        {
            clientId = Multiplayer.getMyId();
            name = loc.name;
            reason = (byte)Reason.BundlesRewards;
            xml = Util.serialize(rewards);
            message();
        }

        protected override void read(BinaryReader reader)
        {
            clientId = reader.ReadByte();
            name = reader.ReadString();
            reason = reader.ReadByte();
            switch ( reason )
            {
                case (byte)Reason.BundlesProgress:
                case (byte)Reason.BundlesRewards:
                    xml = reader.ReadString();
                    break;
                case (byte)Reason.BundlesCompleted:
                    byte count = reader.ReadByte();
                    completed = new bool[count];
                    for ( byte i = 0; i < count; ++i )
                    {
                        completed[i] = reader.ReadBoolean();
                    }
                    break;
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(clientId);
            writer.Write(name);
            writer.Write(reason);
            switch (reason)
            {
                case (byte)Reason.BundlesProgress:
                case (byte)Reason.BundlesRewards:
                    writer.Write(xml);
                    break;
                case (byte)Reason.BundlesCompleted:
                    writer.Write((byte)completed.Count());
                    foreach ( bool c in completed )
                    {
                        writer.Write(c);
                    }
                    break;
            }
        }

        public override void process(Client client)
        {
            process();
        }

        public override void process(Server server, Server.Client client)
        {
            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            CommunityCenter center = Game1.getLocationFromName(name) as CommunityCenter;
            if (center == null) return;

            switch ( reason )
            {
                case (byte)Reason.BundlesProgress:
                    center.bundles = Util.deserialize<SerializableDictionary<int, bool[]>>(xml);
                    break;
                case (byte)Reason.BundlesCompleted:
                    for (int i = 0; i < 6; ++i )
                    {
                        if ( center.areasComplete[ i ] && completed[ i ] )
                        {
                            center.areaCompleteReward(i);
                        }
                    }
                    center.areasComplete = completed;
                    break;
                case (byte)Reason.BundlesRewards:
                    center.bundleRewards = Util.deserialize<SerializableDictionary<int, bool>>(xml);
                    break;
            }
            Multiplayer.locations[center.name].updateCommunityCenterCache();
            message();
        }

        private void message()
        {
            SFarmer fixer = Multiplayer.getFarmer(clientId);
            string fName = (fixer != null) ? fixer.name : null;

            switch (reason)
            {
                case (byte)Reason.BundlesProgress:
                    ChatMenu.chat.Add(new ChatEntry(null, fName + " has made progress on a bundle."));
                    break;
                case (byte)Reason.BundlesCompleted:
                    ChatMenu.chat.Add(new ChatEntry(null, fName + " has completed a bundle."));
                    break;
                case (byte)Reason.BundlesRewards:
                    ChatMenu.chat.Add(new ChatEntry(null, fName + " has claimed a bundle reward."));
                    break;
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + clientId + " " + name + " " + reason + " size:" + (xml == null ? 0 : xml.Length) + " " + completed;
        }
    }
}
