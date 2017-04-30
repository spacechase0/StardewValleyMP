using StardewValleyMP.Connections;
using System.Collections.Generic;

namespace StardewValleyMP.Platforms
{
    class DummyPlatform : IPlatform
    {
        public DummyPlatform()
        {
            Log.info("Using dummy platform.");
        }

        public override string getName()
        {
            return "";
        }

        public override void update()
        {
        }

        public override List< Friend > getFriends()
        {
            return new List<Friend>();
        }

        public override List<Friend> getOnlineFriends()
        {
            return new List<Friend>();
        }

        public override IConnection connectToFriend(Friend other)
        {
            return null;
        }
    }
}
