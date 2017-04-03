using System;
using System.Threading;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Vanilla
{
    public class ModeSelectMenu : IClickableMenu
    {
        private string path;

        public bool readyToLoad = false;
        public bool didModeSelect = false;
        private Thread modeInit;

        private TextBox ipBox;
        private TextBox portBox;
        private int buttonX;
        private int buttonY1;
        private int buttonY2;
        private int buttonY3;
        private int buttonW;
        private int buttonH;

        public ModeSelectMenu(string thePath) : base(Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false)
        {
            path = thePath;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!didModeSelect)
            {
                Rectangle r = new Rectangle(buttonX, buttonY1, buttonW, buttonH);
                if (r.Contains(x, y))
                {
                    Multiplayer.mode = Mode.Singleplayer;
                    Multiplayer.client = null;
                    Multiplayer.server = null;
                    didModeSelect = true;
                    readyToLoad = true;
                }
                r.Y = buttonY2;
                if (r.Contains(x, y))
                {
                    Multiplayer.mode = Mode.Host;
                    didModeSelect = true;

                    portBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
                    portBox.Width *= 3;
                    portBox.X = buttonX + buttonW / 2 - (SpriteText.getWidthOfString("IP Address:") + portBox.Width + 20) / 2 + SpriteText.getWidthOfString("IP Address:") + 20;
                    portBox.Y = buttonY2 + buttonH / 2 - portBox.Height / 2;
                    portBox.Text = MultiplayerMod.ModConfig.DefaultPort;
                }
                r.Y = buttonY3;
                if (r.Contains(x, y))
                {
                    Multiplayer.mode = Mode.Client;
                    didModeSelect = true;

                    ipBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
                    ipBox.Width *= 3;
                    ipBox.X = buttonX + buttonW / 2 - (SpriteText.getWidthOfString("IP Address:") + ipBox.Width + 20) / 2 + SpriteText.getWidthOfString("IP Address:") + 20;
                    ipBox.Y = buttonY1 + buttonH / 2 - ipBox.Height / 2;
                    ipBox.Text = MultiplayerMod.ModConfig.DefaultIP;

                    portBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
                    portBox.Width *= 3;
                    portBox.X = buttonX + buttonW / 2 - (SpriteText.getWidthOfString("IP Address:") + portBox.Width + 20) / 2 + SpriteText.getWidthOfString("IP Address:") + 20;
                    portBox.Y = buttonY2 + buttonH / 2 - portBox.Height / 2;
                    portBox.Text = MultiplayerMod.ModConfig.DefaultPort;
                }
            }
            else if (modeInit == null)
            {
                Multiplayer.problemStarting = false;
                if (ipBox != null) ipBox.Update();
                if (portBox != null) portBox.Update();

                Rectangle r = new Rectangle(buttonX, buttonY3, buttonW, buttonH);
                if (r.Contains(x, y))
                {
                    MultiplayerMod.ModConfig.DefaultPort = portBox.Text;
                    Multiplayer.portStr = portBox.Text;
                    if (Multiplayer.mode == Mode.Host)
                    {
                        modeInit = new Thread(Multiplayer.startHost);
                    }
                    else if (Multiplayer.mode == Mode.Client)
                    {
                        MultiplayerMod.ModConfig.DefaultIP = ipBox.Text;
                        Multiplayer.ipStr = ipBox.Text;
                        modeInit = new Thread(Multiplayer.startClient);
                    }
                    modeInit.Start();
                    ChatMenu.chat.Clear();
                    ChatMenu.chat.Add(new ChatEntry(null, "NOTE: Chat doesn't work on the connection menu."));
                }
            }
            else if (Multiplayer.problemStarting)
            {
                Rectangle r = new Rectangle(buttonX, buttonY3, buttonW, buttonH);
                if (r.Contains(x, y))
                {
                    Multiplayer.client = null;
                    Multiplayer.server = null;
                    readyToLoad = false;

                    didModeSelect = false;
                    modeInit = null;
                    Multiplayer.problemStarting = false;
                }
            }
            else if (Multiplayer.mode == Mode.Host)
            {
                Rectangle r = new Rectangle(buttonX, buttonY3, buttonW, buttonH);
                if (r.Contains(x, y))
                {
                    Log.debug("Stopping listener, beginning loading");
                    Multiplayer.listener.Server.Close();
                    readyToLoad = true;
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            /*
            Rectangle r = new Rectangle(buttonX, buttonY1, buttonW, buttonH);
            if ( r.Contains( x, y ) && !r.Contains( Game1.getOldMouseY(), Game1.getOldMouseY() ) )
            {
                Game1.playSound("Cowboy_gunshot");
            }

            r.Y = buttonY2;
            if (r.Contains(x, y) && !r.Contains(Game1.getOldMouseY(), Game1.getOldMouseY()))
            {
                Game1.playSound("Cowboy_gunshot");
            }

            r.Y = buttonY3;
            if (r.Contains(x, y) && !r.Contains(Game1.getOldMouseY(), Game1.getOldMouseY()))
            {
                Game1.playSound("Cowboy_gunshot");
            }
            //*/
        }

        public override void update(GameTime time)
        {
            if (!didModeSelect) return;
            if ( readyToLoad )
            {
                Multiplayer.lobby = false;
                NewSaveGame.Load(path);
                Game1.exitActiveMenu();
            }
            else if ( Multiplayer.mode == Mode.Client && modeInit != null && modeInit.ThreadState != ThreadState.Running )
            {
                readyToLoad = true;
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (!didModeSelect)
            {
                int x = xPositionOnScreen + width / 4;
                int y = yPositionOnScreen + (int)(height / 5 * 0.5);
                int w = width / 2;
                int h = height / 5;

                buttonX = x;
                buttonH = h;
                buttonW = w;

                buttonY1 = y;
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, "Singleplayer", x + w / 2 - SpriteText.getWidthOfString("Singleplayer") / 2, y + h / 2 - SpriteText.getHeightOfString("Singleplayer") / 2);
                y += (int)(h * 1.25);

                buttonY2 = y;
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, "Host", x + w / 2 - SpriteText.getWidthOfString("Host") / 2, y + h / 2 - SpriteText.getHeightOfString("Host") / 2);
                y += (int)(h * 1.25);

                buttonY3 = y;
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, "Client", x + w / 2 - SpriteText.getWidthOfString("Client") / 2, y + h / 2 - SpriteText.getHeightOfString("Client") / 2);
            }
            else if (modeInit == null)
            {
                int x = buttonX, y = buttonY3, w = buttonW, h = buttonH;
                String str = (Multiplayer.mode == Mode.Host ? "Listen" : "Connect");

                if (ipBox != null)
                {
                    SpriteText.drawString(b, "IP Address:", ipBox.X - SpriteText.getWidthOfString("IP Address:") - 20, ipBox.Y);
                    ipBox.Draw(b);
                }
                if (portBox != null)
                {
                    SpriteText.drawString(b, "Port:", portBox.X - SpriteText.getWidthOfString("IP Address:") - 20, portBox.Y);
                    portBox.Draw(b);
                }

                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, str, x + w / 2 - SpriteText.getWidthOfString(str) / 2, y + h / 2 - SpriteText.getHeightOfString(str) / 2);
            }
            else if (Multiplayer.problemStarting)
            {
                int x = buttonX, y = buttonY1, w = buttonW, h = buttonH;
                String str = "Error";
                SpriteText.drawString(b, str, x + w / 2 - SpriteText.getWidthOfString(str) / 2, y + h / 2 - SpriteText.getHeightOfString(str) / 2);

                y = buttonY2;
                //

                y = buttonY3;
                str = "Back";
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, str, x + w / 2 - SpriteText.getWidthOfString(str) / 2, y + h / 2 - SpriteText.getHeightOfString(str) / 2);
            }
            else if (Multiplayer.server != null)
            {
                int x = buttonX, y = buttonY1, w = buttonW, h = buttonH;
                String str = "Start";

                /*Util.drawStr("Other players: ", x, buttonY1, Color.White);
                foreach ( Server.Client client in Multiplayer.server.clients )
                {
                    String str_ = "<Client " + ( int )( client.id ) + ">";
                    if ( client.farmer != null )
                        str_ = client.farmer.name;

                    y += 30;
                    Util.drawStr(str_, x + 25, y, Color.White);
                }*/

                y = buttonY3;
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                SpriteText.drawString(b, str, x + w / 2 - SpriteText.getWidthOfString(str) / 2, y + h / 2 - SpriteText.getHeightOfString(str) / 2);
            }
            else if (Multiplayer.client != null)
            {
                int x = buttonX, y = buttonY1, w = buttonW, h = buttonH;

                /*Util.drawStr("Other players: ", x, buttonY1, Color.White);
                foreach (KeyValuePair< byte, SFarmer > other in Multiplayer.client.others)
                {
                    String str_ = "<Client " + (int)(other.Key) + ">";
                    if (other.Value != null)
                        str_ = other.Value.name;

                    y += 30;
                    Util.drawStr(str_, x + 25, y, Color.White);
                }*/
            }

            base.draw(b);

            ChatMenu.drawChat(false);
        }
    }
}