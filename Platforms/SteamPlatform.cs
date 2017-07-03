using Steamworks;
using System;
using System.Collections.Generic;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using StardewValleyMP.Connections;
using System.Text;
using System.Threading;

namespace StardewValleyMP.Platforms
{
    public class SteamPlatform : IPlatform
    {
        private static bool initSuccess = false;
        private static Dictionary<int, Texture2D> avatars = new Dictionary< int, Texture2D >();
        private static Dictionary<ulong, IConnection> conns = new Dictionary<ulong, IConnection>();
        private Thread connUpdate;

        public SteamPlatform()
        {
            Log.info("Initializing Steam integration...");
            if ( !SteamAPI.Init() )
            {
                Log.warn("Failed to initialize Steam!");
                IPlatform.instance = new DummyPlatform(); // I don't even know if this will work how I want it
                return;
            }
            initSuccess = true;

            warningHook = new SteamAPIWarningMessageHook_t(onSteamWarning);
            SteamClient.SetWarningMessageHook(warningHook);

            overlayCallback = Callback<GameOverlayActivated_t>.Create(onOverlay);
            sessReqCallback = Callback<P2PSessionRequest_t>.Create(onP2PSessionRequest);
            sessConnFailCallback = Callback<P2PSessionConnectFail_t>.Create(onP2PConnectionFail);

            connUpdate = new Thread(() => { Log.debug("Starting steam listen thread.");  while (true) { SteamStream.update(); } } );
            connUpdate.Start();
        }

        ~SteamPlatform()
        {
            foreach ( KeyValuePair< int, Texture2D > entry in avatars )
            {
                entry.Value.Dispose();
            }
            avatars.Clear();

            if ( connUpdate != null )
                connUpdate.Abort();
        }

        public override string getName()
        {
            return "Steam";
        }

        public override void update()
        {
            if ( !initSuccess )
            {
                IPlatform.instance = new DummyPlatform();
                return;
            }

            SteamAPI.RunCallbacks();
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

        public override IConnection connectToFriend(Friend other)
        {
            var conn = new SteamConnection(other);
            conns.Add(other.id, conn);
            return conn;
        }

        private static SteamAPIWarningMessageHook_t warningHook;
        private static void onSteamWarning( int sev, StringBuilder str )
        {
            Log.warn("[STEAM] " + str);
        }

        private static Callback<GameOverlayActivated_t> overlayCallback;
        static void onOverlay(GameOverlayActivated_t t)
        {
            Log.trace("Steam overlay toggled.");
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

            if (IPlatform.instance.onFriendConnected != null)
            {
                PlatformConnection conn = new SteamConnection(match, true);
                conns.Add(other.m_SteamID, conn);
                IPlatform.instance.onFriendConnected(match, conn);
            }
        }

        private Callback<P2PSessionConnectFail_t> sessConnFailCallback;
        private static void onP2PConnectionFail(P2PSessionConnectFail_t req)
        {
            Log.info("P2P connection status for " + req.m_steamIDRemote.m_SteamID + ": " + req.m_eP2PSessionError);
            if (req.m_eP2PSessionError != 0 /*EP2PSessionError.k_EP2PSessionErrorNone*/ )
            {
                Log.warn("Connection failed for some reason or another.");

                IConnection conn;
                if (conns.TryGetValue(req.m_steamIDRemote.m_SteamID, out conn) )
                {
                    conn.disconnect();
                    conns.Remove(req.m_steamIDRemote.m_SteamID);
                }
            }
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
