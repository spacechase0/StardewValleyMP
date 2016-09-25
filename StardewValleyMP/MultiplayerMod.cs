using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Inheritance;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using Object = StardewValley.Object;
using StardewValleyMP.Vanilla;
using StardewValleyMP.Packets;
using Microsoft.Xna.Framework.Input;

namespace StardewValleyMP
{
    public class MultiplayerMod : Mod
    {
        public const bool DEBUG = true;

        public static Assembly a;
        public override void Entry(params object[] objects)
        {
            GameEvents.UpdateTick += onUpdate;
            GraphicsEvents.OnPreRenderHudEventNoCheck += onPreDraw;
            LocationEvents.CurrentLocationChanged += onCurrentLocationChange;
            ControlEvents.KeyboardChanged += onKeyboardChange;
            GraphicsEvents.DrawDebug += Multiplayer.drawNetworkingDebug;

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
                    TitleMenu title = (TitleMenu)Game1.activeClickableMenu;
                    if (DEBUG)
                    {
                        Util.SetInstanceField(typeof(TitleMenu), title, "chuckleFishTimer", 0);
                        Util.SetInstanceField(typeof(TitleMenu), title, "logoFadeTimer", 0);
                        Util.SetInstanceField(typeof(TitleMenu), title, "fadeFromWhiteTimer", 0);
                    }

                    IClickableMenu submenu = (IClickableMenu)Util.GetInstanceField(typeof(TitleMenu), title, "subMenu");
                    if (submenu != null && submenu.GetType() == typeof(LoadGameMenu))
                    {
                        Util.SetInstanceField(typeof(TitleMenu), title, "subMenu", new NewLoadMenu());
                    }
                }
                prevMenu = Game1.activeClickableMenu;
            }
            catch ( Exception e )
            {
                Log.Async("Exception during update: " + e);
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
                Log.Async("Exception during predraw: " + e);
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
                Log.Async("Exception during location change: " + e);
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
                Log.Async("Exception during keyboard change: " + e);
            }
        }
    }
}
