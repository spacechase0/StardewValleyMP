using System.Collections.Generic;

namespace StardewValleyMP.Platforms
{
    class DummyPlatform : IPlatform
    {
        public override List< Friend > getFriends()
        {
            return new List<Friend>();
        }

        public override List<Friend> getOnlineFriends()
        {
            return new List<Friend>();
        }
    }
}
