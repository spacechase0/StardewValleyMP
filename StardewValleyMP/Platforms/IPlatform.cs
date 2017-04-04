using System.Collections.Generic;
using StardewValley;

namespace StardewValleyMP.Platforms
{
    public abstract class IPlatform
    {
        public static IPlatform instance = makeCurrentPlatform();

        public abstract string getName();

        public abstract List<Friend> getFriends();
        public abstract List<Friend> getOnlineFriends();

        private static IPlatform makeCurrentPlatform()
        {
            if ( Program.buildType == Program.build_steam )
            {
                return new SteamPlatform();
            }

            return new DummyPlatform();
        }
    }
}
