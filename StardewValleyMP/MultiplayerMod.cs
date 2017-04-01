using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValleyMP.Vanilla;
using System;
using System.Reflection;

namespace StardewValleyMP
{
    public class MultiplayerMod : Mod
    {
        public static bool DEBUG { get { return ModConfig.Debug; } }

        public static MultiplayerMod instance;
        public static MultiplayerConfig ModConfig { get; private set; }
        public static Assembly a;
        public override void Entry(params object[] objects)
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
                a = Assembly.GetAssembly(typeof(StardewValley.Game1));
                Util.SetStaticField(a.GetType("StardewValley.Program"), "releaseBuild", false);
            }
        }

        private static IClickableMenu prevMenu = null;
        public static void onUpdate( object sender, EventArgs args )
        {
            try
            {
                if (Multiplayer.mode != Mode.Singleplayer) Multiplayer.update();

                // We need our load menu to be able to do things
                if (Game1.activeClickableMenu is TitleMenu)
                {
                    if (TitleMenu.subMenu != null && (TitleMenu.subMenu.GetType() == typeof(LoadGameMenu)))
                    {
                        TitleMenu.subMenu = new NewLoadMenu();
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
    }
}
