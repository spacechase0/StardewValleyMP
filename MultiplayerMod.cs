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

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            instance = this;

            Util.WHITE_1X1 = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            Util.WHITE_1X1.SetData(new Color[] { Color.White });

            Log.info("Loading Config");
            ModConfig = Helper.ReadConfig<MultiplayerConfig>();

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderingHud += OnRenderingHud;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.Saving += OnSaving;

            Helper.ConsoleCommands.Add("player_unstuck", "...", unstuckCommand);
            Helper.ConsoleCommands.Add("player_sleep", "...", sleepCommand);
            Helper.ConsoleCommands.Add("player_unsleep", "...", unsleepCommand);

            if (DEBUG)
            {
                a = Assembly.GetAssembly(typeof(StardewValley.Game1));
                Util.SetStaticField(a.GetType("StardewValley.Program"), "releaseBuild", false);
            }
        }

        private static void unstuckCommand( string cmd, string[] args )
        {
            Game1.player.canMove = true;
            Game1.freezeControls = false;
            Game1.pauseTime = 0;
            Game1.fadeToBlack = false;
            Game1.player.freezePause = 0;
            Log.info("Done unstucking.");
        }

        private static void sleepCommand(string cmd, string[] args)
        {
            Game1.NewDay(0.0f);
            Log.info("Done sleeping.");
        }

        private static void unsleepCommand(string cmd, string[] args)
        {
            Log.warn("THE SERVER WILL STILL THINK YOU ARE ASLEEP");
            Game1.player.canMove = true;
            Game1.freezeControls = false;
            Game1.pauseTime = 0;
            Game1.fadeToBlack = false;
            Game1.player.freezePause = 0;
            Game1.newDay = false;
            Log.info("Done unsleeping.");
        }

        private static IClickableMenu prevMenu = null;

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnUpdateTicked( object sender, UpdateTickedEventArgs e )
        {
            if (DEBUG)
            {
                Game1.options.pauseWhenOutOfFocus = false;
            }
            try
            {
                IPlatform.instance.update();
                Multiplayer.update();

                // We need our load menu to be able to do things
                if (Game1.activeClickableMenu is TitleMenu)
                {
                    if (TitleMenu.subMenu is LoadGameMenu)
                    {
                        Log.debug("Found vanilla load game menu, replacing with ours.");

                        Multiplayer.lobby = true;

                        LoadGameMenu oldLoadMenu = ( LoadGameMenu ) TitleMenu.subMenu;
                        NewLoadMenu newLoadMenu = new NewLoadMenu();

                        IReflectedField< object > task = instance.Helper.Reflection.GetField< object >(oldLoadMenu, "_initTask");
                        newLoadMenu._initTask = (Task<List<SFarmer>>)task.GetValue();
                        Log.debug("Stole the save listing task, set it to: " + task);

                        TitleMenu.subMenu = newLoadMenu;
                    }
                }
                prevMenu = Game1.activeClickableMenu;
            }
            catch ( Exception ex )
            {
                Log.error("Exception during update: " + ex);
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnRenderingHud( object sender, RenderingHudEventArgs e )
        {
            try
            {
                if (Game1.spriteBatch == null) return;

                ChatMenu.drawChat(true);

                if (Multiplayer.mode == Mode.Singleplayer) return;

                if (Multiplayer.mode != Mode.Singleplayer) Multiplayer.draw( Game1.spriteBatch );
            }
            catch ( Exception ex )
            {
                Log.error("Exception during predraw: " + ex);
            }
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnWarped( object sender, WarpedEventArgs e )
        {
            try
            {
                if (Multiplayer.mode == Mode.Singleplayer) return;

                Multiplayer.locationChange(e.OldLocation, e.NewLocation);
            }
            catch ( Exception ex )
            {
                Log.error("Exception during location change: " + ex);
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnButtonPressed(object sender, ButtonPressedEventArgs e )
        {
            try
            {
                //if (Multiplayer.mode == Mode.Singleplayer) return;

                if ( Game1.gameMode == 3 && Game1.activeClickableMenu == null && prevMenu == null &&
                     //Game1.keyboardDispatcher == null && Game1.keyboardDispatcher.Subscriber == null &&
                     e.Button == SButton.Enter)
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
            catch ( Exception ex )
            {
                Log.error("Exception during keyboard change: " + ex);
            }
        }

        /// <summary>Raised before the game begins writes data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void OnSaving( object sender, EventArgs e )
        {
            Multiplayer.onBeforeSave();
        }
    }
}
