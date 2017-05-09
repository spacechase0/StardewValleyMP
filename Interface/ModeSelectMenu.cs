using System;
using System.Threading;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SFarmer = StardewValley.Farmer;
using StardewValleyMP.Platforms;
using StardewValleyMP.Connections;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace StardewValleyMP.Interface
{
    public class ModeSelectMenu : IClickableMenu
    {
        public bool readyToLoad = false;
        public bool didModeSelect = false;

        private TextBox ipBox;
        private TextBox portBox;
        private FriendSelectorWidget friends;
        private LanSelectorWidget lan;
        private int buttonX;
        private int buttonY1;
        private int buttonY2;
        private int buttonY3;
        private int buttonW;
        private int buttonH;

        private bool showingFriends = false;
        private List< IConnection > pendingConns = new List< IConnection >();
        private Client pendingClient = null;
        
        public bool allowFriends = MultiplayerMod.ModConfig.AllowFriends;
        public bool allowLan = MultiplayerMod.ModConfig.AllowLanDiscovery;

        private bool showingLan = false;

        public ModeSelectMenu() : base(Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false)
        {
            if ( IPlatform.instance.getFriends().Count > 0 )
            {
                friends = new FriendSelectorWidget( true, xPositionOnScreen + width / 5, 75, width / 5 * 3, 475 );
                friends.onSelectFriend = new Action<Friend>(onFriendSelected);
            }

            lan = new LanSelectorWidget(xPositionOnScreen + width / 5, 75, width / 5 * 3, 475);
            lan.onEntrySelected = new Action<LanSelectorWidget.LanEntry>(onLanEntrySelected);
        }
        
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (pendingClient != null) return;

            if ( showingFriends && pendingConns.Count == 0 )
            {
                friends.leftClick(x, y);
            }
            if ( showingLan )
            {
                lan.leftClick(x, y);
            }

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
            else
            {
                if (Multiplayer.mode == Mode.Client)
                {
                    Rectangle r1 = new Rectangle(Game1.viewport.Width / 4, 20, SpriteText.getWidthOfString("Network"), SpriteText.getHeightOfString("Network"));
                    Rectangle r2 = new Rectangle(Game1.viewport.Width / 2 - 50, 20, SpriteText.getWidthOfString("Friends"), SpriteText.getHeightOfString("Friends"));
                    Rectangle r3 = new Rectangle(Game1.viewport.Width / 4 * 3 - 100, 20, SpriteText.getWidthOfString("LAN"), SpriteText.getHeightOfString("LAN"));

                    if (r1.Contains(x, y))
                    {
                        showingFriends = false;
                        showingLan = false;
                        Log.trace("Changing to network tab");
                    }
                    else if (r2.Contains(x, y) && friends != null)
                    {
                        showingFriends = true;
                        showingLan = false;
                        Log.trace("Changing to friends tab");
                    }
                    else if (r3.Contains(x, y))
                    {
                        showingFriends = false;
                        showingLan = true;
                        Log.trace("Changing to LAN tab");

                        lan.start();
                    }
                }
                else if (Multiplayer.mode == Mode.Host)
                {
                    Rectangle r1 = new Rectangle(x, buttonY1 + 48, 64, 64);
                    if ( r1.Contains( x, y ) )
                    {
                        allowFriends = !allowFriends;
                    }
                    r1.Y += 80;
                    if (r1.Contains(x, y))
                    {
                        allowLan = !allowLan;
                    }
                }

                Multiplayer.problemStarting = false;
                if (!showingFriends && !showingLan)
                {
                    if (ipBox != null) ipBox.Update();
                    if (portBox != null) portBox.Update();
                }

                Rectangle r = new Rectangle(buttonX, buttonY3 + buttonH, buttonW, buttonH);
                if (r.Contains(x, y) && !showingFriends && !showingLan)
                {
                    MultiplayerMod.ModConfig.DefaultPort = portBox.Text;
                    Multiplayer.portStr = portBox.Text;
                    if (Multiplayer.mode == Mode.Host)
                    {
                        MultiplayerMod.ModConfig.AllowFriends = allowFriends;
                        MultiplayerMod.ModConfig.AllowLanDiscovery = allowLan;
                        Multiplayer.startHost();
                        Multiplayer.server.connections.startListening(int.Parse(portBox.Text), allowLan, allowFriends);
                    }
                    else if (Multiplayer.mode == Mode.Client)
                    {
                        MultiplayerMod.ModConfig.DefaultIP = ipBox.Text;
                        Multiplayer.ipStr = ipBox.Text;
                        Multiplayer.startClient();
                        ChatMenu.chat.Add(new ChatEntry(null, "NOTE: Chat doesn't work on the connection menu."));
                    }
                    MultiplayerMod.instance.Helper.WriteConfig(MultiplayerMod.ModConfig);
                    Game1.exitActiveMenu();
                }
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (pendingClient != null) return;
            if ( showingFriends && pendingConns.Count == 0 )
            {
                friends.leftRelease(x, y);
            }
            if ( showingLan )
            {
                lan.leftRelease(x, y);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (pendingClient != null) return;
            /*
            Friend f = new Friend();
            f.avatar = Util.WHITE_1X1;
            f.displayName = "TEST DUMMY";
            f.id = 0;
            pendingConns.Add(new SteamConnection(f));
            //*/
        }

        public override void receiveScrollWheelAction( int dir )
        {
            if (pendingClient != null) return;
            if (showingFriends && pendingConns.Count == 0)
            {
                friends.mouseScroll(dir);
            }
            if ( showingLan )
            {
                lan.mouseScroll(dir);
            }
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
            if (pendingClient != null)
            {
                pendingClient.update();
                if (pendingClient.id != 255)
                    readyToLoad = true;
                else return;
            }

            if (!didModeSelect || Multiplayer.problemStarting) return;
            if ( readyToLoad )
            {
                if (pendingClient != null)
                    Multiplayer.client = pendingClient;
                Multiplayer.lobby = false;
                Game1.exitActiveMenu();
            }
            else if ( Multiplayer.mode == Mode.Client )
            {
                if (showingFriends && friends != null)
                    friends.update(time);
                else if (showingLan)
                    lan.update(time);
            }
        }

        public override void draw(SpriteBatch b)
        {
            drawBackground(b);

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
            else
            {
                int x = buttonX, y = buttonY3 + buttonH, w = buttonW, h = buttonH;
                String str = (Multiplayer.mode == Mode.Host ? "Listen" : "Connect");

                if (Multiplayer.mode == Mode.Client)
                {
                    /*
                    SpriteText.drawString(b, "Network", Game1.viewport.Width / 2 - SpriteText.getWidthOfString("Network") - 50, 20,
                        999999, -1, 999999, 1, 0.88f, false, -1, "", showingFriends ? -1 : 5);
                    SpriteText.drawString(b, "Friends", Game1.viewport.Width / 2 + 50, 20,
                        999999, -1, 999999, 1, 0.88f, false, -1, "", friends == null ? 0 : (showingFriends ? 5 : -1));*/

                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 4 +0, 20+2), (Color.Black)*0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 4 +2, 20+0), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 4 +0, 20-2), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 4 -2, 20-0), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 4, 20), (!showingFriends && !showingLan ? Color.OrangeRed : Color.SaddleBrown));
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 - 50 + 0, 20 + 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 - 50 + 2, 20 + 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 - 50 + 0, 20 - 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 - 50 - 2, 20 - 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 - 50, 20), friends == null ? Color.Black : (showingFriends ? Color.OrangeRed : Color.SaddleBrown));
                    b.DrawString(Game1.dialogueFont, "LAN", new Vector2(Game1.viewport.Width / 4 * 3 - 100 + 0, 20 + 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "LAN", new Vector2(Game1.viewport.Width / 4 * 3 - 100 + 2, 20 + 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "LAN", new Vector2(Game1.viewport.Width / 4 * 3 - 100 + 0, 20 - 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "LAN", new Vector2(Game1.viewport.Width / 4 * 3 - 100 - 2, 20 - 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "LAN", new Vector2(Game1.viewport.Width / 4 * 3 - 100, 20), showingLan ? Color.OrangeRed : Color.SaddleBrown);
                }
                else if ( Multiplayer.mode == Mode.Host )
                {
                    Color gray = new Color(127, 127, 127);
                    Color text = new Color(86, 22, 12);
                    Color hover1 = Color.Wheat * 0.8f;
                    Color hover2 = Color.DarkGoldenrod * 0.8f;
                    hover1.A = hover2.A = 0xFF;

                    String checkStr = "Allow direct friend connections";
                    Rectangle r = new Rectangle(x, buttonY1 + 48, 64, 64);
                    Vector2 pos = new Vector2(r.X + r.Width + 30, r.Y + (r.Height - Game1.dialogueFont.MeasureString(checkStr).Y) / 2);
                    drawTextureBox(b, r.X, r.Y, r.Width, r.Height, r.Contains(Game1.getMousePosition()) ? hover1 : Color.White);
                    if ( allowFriends )
                        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), r.X + r.Width / 5, r.Y + r.Height / 5, r.Width / 8 * 5, r.Height / 8 * 5, r.Contains(Game1.getMousePosition()) ? hover2 : Color.DarkGoldenrod, 1, false);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2( 0,  2), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2( 2,  0), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2( 0, -2), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2(-2,  0), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos, text);

                    checkStr = "Allow LAN discovery";
                    r.Y += 80;
                    pos.Y += 80;
                    drawTextureBox(b, r.X, r.Y, r.Width, r.Height, r.Contains(Game1.getMousePosition()) ? hover1 : Color.White);
                    if (allowLan)
                        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), r.X + r.Width / 5, r.Y + r.Height / 5, r.Width / 8 * 5, r.Height / 8 * 5, r.Contains(Game1.getMousePosition()) ? hover2 : Color.DarkGoldenrod, 1, false);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2(0, 2), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2(2, 0), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2(0, -2), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos + new Vector2(-2, 0), gray * 0.25f);
                    b.DrawString(Game1.dialogueFont, checkStr, pos, text);
                }

                if (showingFriends)
                {
                    if (pendingClient == null)
                    {
                        if (pendingConns.Count > 0)
                        {
                            PlatformConnection conn = (PlatformConnection)pendingConns[0];
                            Friend friend = conn.friend;

                            int ix = xPositionOnScreen + width / 5;
                            int iw = width / 5 * 3;
                            int ih = 80 * 2 + 64;
                            int iy = (Game1.viewport.Height - ih) / 2;

                            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), ix, iy, iw, ih, Color.White, (float)Game1.pixelZoom, true);
                            ix += 32;
                            iy += 32;
                            b.Draw(friend.avatar, new Rectangle(ix, iy, 64, 64), Color.White);
                            SpriteText.drawString(b, friend.displayName, ix + 88, iy + 8);
                            SpriteText.drawString(b, "Connecting...", ix + 32, iy + 96);
                        }
                        else friends.draw(b);
                    }
                }
                else if (showingLan)
                {
                    lan.draw(b);
                }
                else
                {
                    Color gray = new Color(127, 127, 127);
                    Color text = new Color(86, 22, 12);
                    if (ipBox != null)
                    {
                        b.DrawString(Game1.dialogueFont, "IP Address:", new Vector2(ipBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 0, ipBox.Y + 2), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "IP Address:", new Vector2(ipBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 2, ipBox.Y + 0), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "IP Address:", new Vector2(ipBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 0, ipBox.Y - 2), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "IP Address:", new Vector2(ipBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 - 2, ipBox.Y - 0), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "IP Address:", new Vector2(ipBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20, ipBox.Y), text);
                        
                        ipBox.Draw(b);
                    }
                    if (portBox != null)
                    {
                        b.DrawString(Game1.dialogueFont, "Port:", new Vector2(portBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 0, portBox.Y + 2), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "Port:", new Vector2(portBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 2, portBox.Y + 0), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "Port:", new Vector2(portBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 + 0, portBox.Y - 2), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "Port:", new Vector2(portBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20 - 2, portBox.Y - 0), gray * 0.25f);
                        b.DrawString(Game1.dialogueFont, "Port:", new Vector2(portBox.X - Game1.dialogueFont.MeasureString("IP Address:").X - 20, portBox.Y), text);

                        portBox.Draw(b);
                    }

                    IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, new Rectangle(x, y, w, h).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                    SpriteText.drawString(b, str, x + w / 2 - SpriteText.getWidthOfString(str) / 2, y + h / 2 - SpriteText.getHeightOfString(str) / 2);
                }
            }

            base.draw(b);

            ChatMenu.drawChat(false);
            drawMouse(b);
        }

        private void onFriendSelected(Friend friend)
        {
            Log.trace("onFriendSelected " + friend.displayName);
            if (Multiplayer.client == null && Multiplayer.mode == Mode.Client)
            {
                IConnection conn = IPlatform.instance.connectToFriend(friend);
                pendingClient = new Client(conn);
            }
        }

        private void onLanEntrySelected(LanSelectorWidget.LanEntry entry)
        {
            Log.info("Selected LAN entry: " + entry.name + " @ " + entry.server.Address + ":" + entry.port);

            Multiplayer.portStr = portBox.Text;
            Multiplayer.ipStr = ipBox.Text;
            Multiplayer.startClient();
        }
    }
}