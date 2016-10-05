using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValleyMP.Packets;

namespace StardewValleyMP
{
    public class ChatEntry
    {
        public string timestamp;
        public string player;
        public string message;

        public int screenTime = 60 * 6;

        public ChatEntry(Farmer farmer, string theMessage)
        {
            timestamp = DateTime.Now.ToShortTimeString();
            player = ( farmer != null ) ? farmer.name : "";
            message = theMessage;

            if ( farmer == null )
                Log.Async("[] " + message);
            else
                Log.Async("<" + farmer.name + "> " + message);
        }
    }

    public class ChatMenu : IClickableMenu
    {
        public static List<ChatEntry> chat = new List<ChatEntry>();
        public static string typing = "";

        public ChatMenu() : base( 50, 50, Game1.viewport.Width - 100, Game1.viewport.Height - 100, true )
        {
            //chat.Add(new ChatEntry(Game1.player, "~!@#$%^&*()_+|}{POIUYTREWQASDFGHJKL:\"?><MNBVCXZ"));
            //chat.Add(new ChatEntry(Game1.player, "`1234567890-=\\][poiuytrewqasdfghjkl;'/.,mnbvcxz"));
            KeyboardInput.CharEntered += gotChar;
            exitFunction = destruct;
        }

        private void destruct()
        {
            KeyboardInput.CharEntered -= gotChar;
        }

        private void gotChar(object sender, CharacterEventArgs e)
        {
            if (Game1.activeClickableMenu != this) return;
            char c = e.Character;

            if (c == '\r' || c == '\n')
            {
                if ( typing == "" )
                {
                    exitThisMenu(true);
                    return;
                }

                chat.Add(new ChatEntry(Game1.player, typing));
                if (Multiplayer.mode != Mode.Singleplayer)
                    Multiplayer.sendFunc(new ChatPacket(Multiplayer.getMyId(), typing));

                if (MultiplayerMod.DEBUG)
                {
                    if (typing.StartsWith("/instance ") && typing.Length > 10 )
                    {
                        string baseLoc = null;
                        if (Game1.player.currentLocation is StardewValley.Locations.FarmHouse) baseLoc = "FarmHouse";
                        if (Game1.player.currentLocation is StardewValley.Locations.Cellar) baseLoc = "Cellar";

                        if (baseLoc != null)
                        {
                            Log.Async("Looking for " + baseLoc + "_" + typing.Substring(10));
                            if (Game1.getLocationFromName(baseLoc + "_" + typing.Substring(10)) != null)
                                Game1.warpFarmer(baseLoc + "_" + typing.Substring(10), (int)Game1.player.position.X / Game1.tileSize, (int)Game1.player.position.Y / Game1.tileSize, false);
                        }
                    }
                }

                typing = "";
            }
            else if (c == '\b' && typing.Length > 0)
            {
                typing = typing.Substring(0, typing.Length - 1);
            }
            else if ( !char.IsControl( c ) )
            {
                typing += c;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if ( key == Keys.Escape )
            {
                exitThisMenu(true);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, xPositionOnScreen + 25, yPositionOnScreen + 25, width - 50, height - 50, Color.White);
            this.drawHorizontalPartition(b, yPositionOnScreen + height - 110, true);

            drawChat();
            Util.drawStr(typing, 75, Game1.viewport.Height - 140, Game1.textColor, 1);

            base.draw(b);
            this.drawMouse(b);
        }

        public static void drawChat( bool fade = false )
        {
            SpriteBatch b = Game1.spriteBatch;

            float x = 75;
            float y = Game1.viewport.Height - 185;
            for (int i = chat.Count - 1; i >= 0; --i )
            {
                ChatEntry entry = chat[i];

                float alpha = 1;
                if ( fade )
                {
                    if (entry.screenTime < 60)
                        alpha = entry.screenTime / 60f;
                }
                entry.screenTime--;
                
                Color col = Color.White;
                if ( fade ) col = Game1.textColor;
                string str = entry.player + ": " + entry.message;
                if ( entry.player == "" )
                {
                    str = entry.message;
                    col = ( fade ? Color.Silver : Color.DimGray );
                }

                Util.drawStr(str, x, y, col, alpha);
                if ( !fade )
                {
                    Util.drawStr(entry.timestamp, Game1.viewport.Width - 75 - 95, y, Game1.textColor, alpha);
                }

                y -= 25;

                if (y < 65) break;
            }
        }
    }
}
