using System.Collections.Generic;
using StardewValley;
using System;
using StardewValleyMP.Connections;

namespace StardewValleyMP.Platforms
{
    public abstract class IPlatform
    {
        public static IPlatform instance = makeCurrentPlatform();

        public abstract string getName();

        public abstract void update();

        public abstract List<Friend> getFriends();
        public abstract List<Friend> getOnlineFriends();

        public Action<Friend, PlatformConnection> onFriendConnected;
        public abstract IConnection connectToFriend(Friend other);

        private static IPlatform makeCurrentPlatform()
        {
            if ( Program.buildType == Program.build_steam )
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (!path.EndsWith("/"))
                    path += "/";
                path += "Steamworks.NET.dll";

                if ( System.IO.File.Exists( path ) )
                    return new SteamPlatform();
            }

            return new DummyPlatform();
        }
    }
}
