using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValleyMP.Interface;
using StardewValleyMP.Platforms;
using StardewValleyMP.Vanilla;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP
{
    public class MultiplayerMod : Mod
    {
        public static bool DEBUG { get { return ModConfig.Debug; } }

        public static MultiplayerMod instance;
        public static MultiplayerConfig ModConfig { get; private set; }
        public static Assembly a;
        public override void Entry(IModHelper helper)
        {
            instance = this;

            Log.info("Loading Config");
            ModConfig = Helper.ReadConfig<MultiplayerConfig>();

            GameEvents.LoadContent += loadContent;
            GameEvents.UpdateTick += onUpdate;
            GraphicsEvents.OnPreRenderHudEvent += onPreDraw;      
            LocationEvents.CurrentLocationChanged += onCurrentLocationChange;
            ControlEvents.KeyboardChanged += onKeyboardChange;

            //GraphicsEvents.DrawDebug += Multiplayer.drawNetworkingDebug;

            if (DEBUG)
            {
                Helper.ConsoleCommands.Add("platform", "", platformtest);
                Helper.ConsoleCommands.Add("steam", "", steamtest);
            }

            if (DEBUG)
            {
                a = Assembly.GetAssembly(typeof(StardewValley.Game1));
                Util.SetStaticField(a.GetType("StardewValley.Program"), "releaseBuild", false);
            }
        }

        public static void loadContent( object sender, EventArgs args )
        {
            Util.WHITE_1X1 = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            Util.WHITE_1X1.SetData(new Color[] { Color.White });
        }

        private static IClickableMenu prevMenu = null;
        public static void onUpdate( object sender, EventArgs args )
        {
            try
            {
                IPlatform.instance.update();
                Multiplayer.update();

                // We need our load menu to be able to do things
                if (Game1.activeClickableMenu is TitleMenu)
                {
                    if (TitleMenu.subMenu != null && (TitleMenu.subMenu.GetType() == typeof(LoadGameMenu)))
                    {
                        LoadGameMenu oldLoadMenu = ( LoadGameMenu ) TitleMenu.subMenu;
                        NewLoadMenu newLoadMenu = new NewLoadMenu();

                        IPrivateField< object > task = instance.Helper.Reflection.GetPrivateField< object >(oldLoadMenu, "_initTask");
                        newLoadMenu._initTask = (Task<List<SFarmer>>)task.GetValue();

                        TitleMenu.subMenu = newLoadMenu;
                    }
                }
                prevMenu = Game1.activeClickableMenu;
            }
            catch ( Exception e )
            {
                Log.error("Exception during update: " + e);
            }
        }

        public static void onPreDraw( object sender, EventArgs args )
        {
            try
            {
                if (Game1.spriteBatch == null) return;

                ChatMenu.drawChat(true);

                if (Multiplayer.mode == Mode.Singleplayer) return;

                if (Multiplayer.mode != Mode.Singleplayer) Multiplayer.draw( Game1.spriteBatch );
            }
            catch ( Exception e )
            {
                Log.error("Exception during predraw: " + e);
            }
        }

        public static void onCurrentLocationChange( object sender, EventArgs args )
        {
            try
            {
                if (Multiplayer.mode == Mode.Singleplayer) return;

                Multiplayer.locationChange((args as EventArgsCurrentLocationChanged).PriorLocation, (args as EventArgsCurrentLocationChanged).NewLocation);
            }
            catch ( Exception e )
            {
                Log.error("Exception during location change: " + e);
            }
        }

        public static void onKeyboardChange(object sender, EventArgs args )
        {
            try
            {
                //if (Multiplayer.mode == Mode.Singleplayer) return;

                EventArgsKeyboardStateChanged args_ = ( EventArgsKeyboardStateChanged ) args;
                KeyboardState old = args_.PriorState;
                KeyboardState curr = args_.NewState;

                if ( Game1.gameMode == 3 && Game1.activeClickableMenu == null && prevMenu == null &&
                     //Game1.keyboardDispatcher == null && Game1.keyboardDispatcher.Subscriber == null &&
                     curr.IsKeyDown(Keys.Enter) && !old.IsKeyDown( Keys.Enter ) )
                {
                    if (Game1.isEating && Game1.player.itemToEat != null && Game1.player.itemToEat.parentSheetIndex == 434 /* stardrop */)
                    {
                        // Interrupting a star drop being eaten causes it to disappear and not give you the stamina buff
                    }
                    else
                    {
                        Game1.activeClickableMenu = new ChatMenu();
                    }
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception during keyboard change: " + e);
            }
        }

        public static void platformtest(string str, string[] args)
        {
            if (args.Length == 0)
            {
                Log.error("No command given.");
                return;
            }

            if ( args[ 0 ] == "friends" )
            {
                var friends = IPlatform.instance.getFriends();
                foreach ( Friend friend in friends )
                {
                    Log.info("Friend: " + friend.id + "=" + friend.displayName + " avatar=" + (friend.avatar == null ? "no" : "yes"));
                }
            }
            else if (args[0] == "online")
            {
                var friends = IPlatform.instance.getOnlineFriends();
                foreach (Friend friend in friends)
                {
                    Log.info("Friend: " + friend.id + "=" + friend.displayName + " avatar=" + (friend.avatar == null ? "no" : "yes"));
                }
            }
            else
            {
                Log.error("Bad command.");
            }
        }

        private static Callback<P2PSessionRequest_t> sessReq;
        public static void steamtest( string str, string[] args )
        {
            if (args.Length == 0)
            {
                Log.error("No command given.");
                return;
            }

            try
            {
                if (args[0] == "init")
                {
                    try
                    {
                        bool b = SteamAPI.InitSafe();
                        if (b) Log.info("Steamworks initialized");
                        else Log.error("Failed to initialize steamworks");
                    }
                    catch (Exception e)
                    {
                        Log.error("Exception during steamworks initialization: " + e.Message);
                    }
                }
                else if (args[0] == "id")
                {
                    Log.info("Steam user ID: " + SteamUser.GetSteamID());
                }
                else if (args[0] == "listen")
                {
                    sessReq = Callback<P2PSessionRequest_t>.Create(sessReqFunc);
                }
                else if (args[0] == "accept" && args.Length == 2)
                {
                    CSteamID req = args[1] == "self" ? SteamUser.GetSteamID() : new CSteamID(ulong.Parse(args[1]));
                    bool b = SteamNetworking.AcceptP2PSessionWithUser(req);
                    if (b) Log.info("Successfully accepted p2p session with " + req);
                    else Log.error("Failed to accept session.");
                }
                else if (args[0] == "send" && args.Length >= 3)
                {
                    CSteamID to = args[1] == "self" ? SteamUser.GetSteamID() : new CSteamID(ulong.Parse(args[1]));

                    String[] args_ = new String[args.Length - 2];
                    Array.Copy(args, 2, args_, 0, args.Length - 2);
                    string msg = String.Join(" ", args_);

                    byte[] bytes = new byte[msg.Length * sizeof(char)];
                    System.Buffer.BlockCopy(msg.ToCharArray(), 0, bytes, 0, bytes.Length);

                    bool b = SteamNetworking.SendP2PPacket(to, bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable);
                    if (b) Log.info("Successfully sent \"" + msg + "\" to " + to);
                    else Log.error("Failed to send packet.");
                }
                else if (args[0] == "receive")
                {
                    uint size;
                    while (SteamNetworking.IsP2PPacketAvailable(out size))
                    {
                        CSteamID from;
                        byte[] buffer = new byte[size];
                        uint read;

                        if (SteamNetworking.ReadP2PPacket(buffer, size, out read, out from))
                        {
                            char[] chars = new char[read / sizeof(char)];
                            Buffer.BlockCopy(buffer, 0, chars, 0, (int)read);

                            Log.info("Received " + read + " bytes from " + from + ": ");
                            Log.info(new string(chars));
                        }
                        else
                        {
                            Log.error("Failed to receive packet.");
                        }
                    }
                }
                else
                {
                    Log.error("Bad command.");
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception in steam command: " + e.Message);
            }
        }

        private static void sessReqFunc( P2PSessionRequest_t req )
        {
            CSteamID other = req.m_steamIDRemote;
            Log.info("Session request from " + other);
        }
    }
}
