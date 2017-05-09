﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValleyMP.Interface;
using StardewValleyMP.Platforms;
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

            Util.WHITE_1X1 = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            Util.WHITE_1X1.SetData(new Color[] { Color.White });

            makeUsLast();

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
        private static bool firstUpdate = true;
        public static void onUpdate( object sender, EventArgs args )
        {
            if ( firstUpdate )
            {
                GameEvents.UpdateTick += onUpdate;
                GraphicsEvents.OnPreRenderHudEvent += onPreDraw;
                LocationEvents.CurrentLocationChanged += onCurrentLocationChange;
                ControlEvents.KeyboardChanged += onKeyboardChange;
                SaveEvents.AfterLoad += afterLoad;
                SaveEvents.BeforeSave += beforeSave;
                SaveEvents.AfterSave += afterSave;

                firstUpdate = false;
            }

            if (DEBUG)
            {
                Game1.options.pauseWhenOutOfFocus = false;
            }
            try
            {
                IPlatform.instance.update();
                Multiplayer.update();
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

        public static void afterLoad(object sender, EventArgs args)
        {
            Game1.activeClickableMenu = new ModeSelectMenu();
        }

        public static void beforeSave(object sender, EventArgs args)
        {
        }

        public static void afterSave(object sender, EventArgs args)
        {
        }

        private void makeUsLast()
        {
            var mods = Helper.Reflection.GetPrivateValue<List<IMod>>(Helper.ModRegistry, "Mods");
            mods.Remove(this);
            mods.Add(this);
        }
    }
}
