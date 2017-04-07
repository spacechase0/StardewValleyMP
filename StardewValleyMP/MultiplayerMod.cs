using Microsoft.Xna.Framework.Input;
using Open.Nat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValleyMP.Vanilla;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
            
            GameEvents.UpdateTick += onUpdate;
            GraphicsEvents.OnPreRenderHudEvent += onPreDraw;
            LocationEvents.CurrentLocationChanged += onCurrentLocationChange;
            ControlEvents.KeyboardChanged += onKeyboardChange;

            //GraphicsEvents.DrawDebug += Multiplayer.drawNetworkingDebug;

            if (DEBUG)
            {
                Helper.ConsoleCommands.Add("nat", "", nattest);
            }

            if (DEBUG)
            {
                a = Assembly.GetAssembly(typeof(StardewValley.Game1));
                Util.SetStaticField(a.GetType("StardewValley.Program"), "releaseBuild", false);
            }
        }

        ~MultiplayerMod()
        {
            NatDiscoverer.ReleaseAll();
        }

        private static IClickableMenu prevMenu = null;
        public static void onUpdate( object sender, EventArgs args )
        {
            try
            {
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

        private static NatDiscoverer disco;
        private static Mapping portMap = new Mapping(Protocol.Tcp, 24644, 24644, 5 * 60, "StardewValleyMP");
        private static CancellationTokenSource canc;
        private static List<NatDevice> devs = new List< NatDevice >();
        public async static void nattest(string str, string[] args)
        {
            if ( args.Length < 1 )
            {
                Log.error("No command given");
                return;
            }

            if ( canc == null )
            {
                canc = new CancellationTokenSource();
                canc.CancelAfter(3000);
            }

            if ( args[ 0 ] == "discover" )
            {
                if ( disco == null )
                    disco = new NatDiscoverer();

                Log.info("Starting discovery...");
                var devEnum = await disco.DiscoverDevicesAsync(PortMapper.Pmp | PortMapper.Upnp, canc);
                foreach ( var dev in devEnum )
                {
                    Log.info("\t" + dev);
                    devs.Add(dev);
                }
                Log.info("Done");
            }
            else if (args[0] == "devices" && args.Length == 2)
            {
                Log.info("Devices: ");
                foreach (NatDevice dev in devs )
                {
                    Log.info("\t" + dev);
                }
            }
            else if ( args[ 0 ] == "mappings" && args.Length == 2 )
            {
                try
                {
                    int index = int.Parse(args[1]);
                    if ( index >= devs.Count )
                    {
                        Log.info("Bad device");
                        return;
                    }
                    NatDevice dev = devs[index];

                    Log.info("Mappings: ");
                    var mappings = await dev.GetAllMappingsAsync();
                    foreach (var mapping in mappings)
                    {
                        Log.info("\t" + mapping);
                    }
                }
                catch (Exception e)
                {
                    Log.error("Exception: " + e);
                }
            }
            else if ( args[ 0 ] == "map" && args.Length == 2 )
            {
                int index = int.Parse(args[1]);
                if (index >= devs.Count)
                {
                    Log.info("Bad device");
                    return;
                }
                NatDevice dev = devs[index];
                
                await dev.CreatePortMapAsync(portMap);

                Log.info("Done.");
            }
            else if (args[0] == "unmap" && args.Length == 2)
            {
                int index = int.Parse(args[1]);
                if (index >= devs.Count)
                {
                    Log.info("Bad device");
                    return;
                }
                NatDevice dev = devs[index];

                await dev.DeletePortMapAsync(portMap);

                Log.info("Done.");
            }
            else
            {
                Log.error("Bad command.");
            }
        }
    }
}
