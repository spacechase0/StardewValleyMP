using Steamworks;
using System;
using System.Collections.Generic;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using StardewValleyMP.Connections;

namespace StardewValleyMP.Platforms
{
    public class SteamPlatform : IPlatform
    {
        public SteamPlatform()
        {
            Log.info("Initializing Steam integration...");
            SteamAPI.InitSafe();

            sessReqCallback = Callback<P2PSessionRequest_t>.Create(onP2PSessionRequest);
        }

        public override string getName()
        {
            return "Steam";
        }

        private List<Friend> friends;
        public override List<Friend> getFriends()
        {
            if (friends != null) return friends;

            friends = new List<Friend>();

            int friendCount = SteamFriends.GetFriendCount( EFriendFlags.k_EFriendFlagImmediate );
            for ( int i = 0; i < friendCount; ++i )
            {
                CSteamID id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                friends.Add( getFriendFromId( id ) );
            }

            return friends;
        }

        public override List<Friend> getOnlineFriends()
        {
            List<Friend> friends = getFriends();
            List<Friend> online = new List<Friend>();
            foreach ( Friend friend in friends )
            {
                var state = SteamFriends.GetFriendPersonaState( new CSteamID( friend.id ) );
                if ( state != EPersonaState.k_EPersonaStateOffline )
                {
                    online.Add(friend);
                }
            }

            return online;
        }

        private Callback<P2PSessionRequest_t> sessReqCallback;
        private static void onP2PSessionRequest( P2PSessionRequest_t req )
        {
            CSteamID other = req.m_steamIDRemote;
            Log.info("Got a P2P session request: " + req.m_steamIDRemote);

            Friend match = null;
            foreach ( Friend friend in IPlatform.instance.getFriends() )
            {
                if ( friend.id == other.m_SteamID )
                {
                    match = friend;
                    break;
                }
            }

            if ( match == null )
            {
                match = getFriendFromId(other);
            }

            IPlatform.instance.onFriendConnected(match, new SteamConnection(match));
        }

        private static Friend getFriendFromId( CSteamID id )
        {
            Friend friend = new Friend();
            friend.id = id.m_SteamID;

            string name = SteamFriends.GetPlayerNickname(id);
            friend.displayName = (name == null || name == "") ? SteamFriends.GetFriendPersonaName(id) : name;

            int handle = SteamFriends.GetMediumFriendAvatar(id);
            uint width = 0, height = 0;
            if (handle != 0 && SteamUtils.GetImageSize(handle, out width, out height))
            {
                byte[] pixels = new byte[width * height * 4];
                if (SteamUtils.GetImageRGBA(handle, pixels, (int)(width * height * 4)))
                {
                    friend.avatar = new Texture2D(Game1.graphics.GraphicsDevice, (int)width, (int)height);
                    friend.avatar.SetData(pixels);
                }
            }

            return friend;
        }
    }
}
