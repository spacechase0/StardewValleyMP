using System;
using System.Threading;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValleyMP.Vanilla;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SFarmer = StardewValley.Farmer;
using StardewValleyMP.Platforms;
using StardewValleyMP.Connections;
using System.Collections.Generic;
using System.IO;
using StardewValleyMP.Packets;
using System.Net.NetworkInformation;
using System.Net;

namespace StardewValleyMP.Interface
{
    public class ModeSelectMenu : IClickableMenu
    {
        private string path;

        public bool readyToLoad = false;
        public bool didModeSelect = false;
        private Thread modeInit;

        private TextBox ipBox;
        private TextBox portBox;
        private FriendSelectorWidget friends;
        private int buttonX;
        private int buttonY1;
        private int buttonY2;
        private int buttonY3;
        private int buttonW;
        private int buttonH;

        private bool showingFriends = false;
        private List< IConnection > pendingConns = new List< IConnection >();
        private Client pendingClient = null;

        private string localIp, externalIp;

        public ModeSelectMenu(string thePath) : base(Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false)
        {
            path = thePath;
            if ( IPlatform.instance.getFriends().Count > 0 )
            {
                friends = new FriendSelectorWidget( true, xPositionOnScreen + width / 5, 75, width / 5 * 3, 475 );
                friends.onSelectFriend = new Action<Friend>(onFriendSelected);
            }

            try
            {
                NetworkInterface[] ints = NetworkInterface.GetAllNetworkInterfaces();
                foreach ( var net in ints )
                {
                    IPInterfaceProperties ipProps = net.GetIPProperties();
                    foreach (IPAddressInformation ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || IPAddress.IsLoopback(ip.Address))
                            continue;

                        localIp = ip.Address.ToString();
                        break;
                    }

                    if (localIp != "")
                        break;
                }
            }
            catch ( Exception e )
            {
                Log.warn("Exception getting internal IP: " + e);
                localIp = "n/a";
            }
            try
            {
                externalIp = new WebClient().DownloadString("http://ipinfo.io/ip").Trim();
            }
            catch (Exception e)
            {
                Log.warn("Exception getting external IP: " + e);
                externalIp = "n/a";
            }
        }

        private bool justClicked = false;
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (pendingClient != null) return;

            if ( showingFriends && pendingConns.Count == 0 )
            {
                friends.leftClick(x, y);
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
            else if (modeInit == null)
            {
                if (Multiplayer.mode == Mode.Client)
                {
                    Rectangle r1 = new Rectangle(Game1.viewport.Width / 2 - SpriteText.getWidthOfString("Network") - 50, 20, SpriteText.getWidthOfString("Network"), SpriteText.getHeightOfString("Network"));
                    Rectangle r2 = new Rectangle(Game1.viewport.Width / 2 + 50, 20, SpriteText.getWidthOfString("Friends"), SpriteText.getHeightOfString("Friends"));

                    if (r1.Contains(x, y))
                    {
                        showingFriends = false;
                        Log.trace("Changing to network tab");
                    }
                    else if (r2.Contains(x, y) && friends != null)
                    {
                        showingFriends = true;
                        Log.trace("Changing to friends tab");
                    }
                }

                Multiplayer.problemStarting = false;
                if (!showingFriends)
                {
                    if (ipBox != null) ipBox.Update();
                    if (portBox != null) portBox.Update();
                }

                Rectangle r = new Rectangle(buttonX, buttonY3 + buttonH, buttonW, buttonH);
                if (r.Contains(x, y) && !showingFriends)
                {
                    MultiplayerMod.ModConfig.DefaultPort = portBox.Text;
                    Multiplayer.portStr = portBox.Text;
                    if (Multiplayer.mode == Mode.Host)
                    {
                        modeInit = new Thread(Multiplayer.startHost);
                        IPlatform.instance.onFriendConnected = new Action<Friend, PlatformConnection>(onFriendConnected);
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
                if (pendingConns.Count != 0)
                {
                    justClicked = true;
                }
                else
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
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (pendingClient != null) return;
            if ( showingFriends && pendingConns.Count == 0 )
            {
                friends.leftRelease(x, y);
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
                NewSaveGame.Load(path);
                Game1.exitActiveMenu();
            }
            else if ( Multiplayer.mode == Mode.Client && modeInit != null && modeInit.ThreadState != ThreadState.Running )
            {
                readyToLoad = true;
            }
            else if ( modeInit == null && Multiplayer.mode == Mode.Client )
            {
                if (friends != null)
                    friends.update(time);
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
                int x = buttonX, y = buttonY3 + buttonH, w = buttonW, h = buttonH;
                String str = (Multiplayer.mode == Mode.Host ? "Listen" : "Connect");

                if (Multiplayer.mode == Mode.Client)
                {
                    /*
                    SpriteText.drawString(b, "Network", Game1.viewport.Width / 2 - SpriteText.getWidthOfString("Network") - 50, 20,
                        999999, -1, 999999, 1, 0.88f, false, -1, "", showingFriends ? -1 : 5);
                    SpriteText.drawString(b, "Friends", Game1.viewport.Width / 2 + 50, 20,
                        999999, -1, 999999, 1, 0.88f, false, -1, "", friends == null ? 0 : (showingFriends ? 5 : -1));*/

                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 2 - Game1.dialogueFont.MeasureString("Network").X - 50+0, 20+2), (Color.Black)*0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 2 - Game1.dialogueFont.MeasureString("Network").X - 50+2, 20+0), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 2 - Game1.dialogueFont.MeasureString("Network").X - 50+0, 20-2), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 2 - Game1.dialogueFont.MeasureString("Network").X - 50-2, 20-0), (Color.Black) *0.25f);
                    b.DrawString(Game1.dialogueFont, "Network", new Vector2(Game1.viewport.Width / 2 - Game1.dialogueFont.MeasureString("Network").X - 50, 20), (showingFriends ? Color.SaddleBrown : Color.OrangeRed));
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 + 50 + 0, 20 + 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 + 50 + 2, 20 + 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 + 50 + 0, 20 - 2), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 + 50 - 2, 20 - 0), (Color.Black) * 0.25f);
                    b.DrawString(Game1.dialogueFont, "Friends", new Vector2(Game1.viewport.Width / 2 + 50, 20), friends == null ? Color.Black : (showingFriends ? Color.OrangeRed : Color.SaddleBrown));
                }

                if (!showingFriends)
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
                else if ( pendingClient == null )
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

                    ix += 40;
                    iy += ih - 32 * 2 - 80;
                    iw = iw / 2 - 96;
                    ih = 80;

                    IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), ix, iy, iw, ih, new Rectangle(ix, iy, iw, ih).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                    SpriteText.drawString(b, "Accept", ix + iw / 2 - SpriteText.getWidthOfString("Accept") / 2, iy + ih / 2 - SpriteText.getHeightOfString("Accept") / 2);
                    if ( justClicked && new Rectangle(ix, iy, iw, ih).Contains( Game1.getMouseX(), Game1.getMouseY() ) )
                    {
                        Log.trace("Accepted " + ((PlatformConnection)pendingConns[0]).friend.displayName);
                        ((PlatformConnection)pendingConns[0]).accept();
                        Multiplayer.server.addClient(pendingConns[0], true);
                        pendingConns.Remove(pendingConns[0]);
                    }

                    ix += iw + 40;

                    IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), ix, iy, iw, ih, new Rectangle(ix, iy, iw, ih).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
                    SpriteText.drawString(b, "Decline", ix + iw / 2 - SpriteText.getWidthOfString("Decline") / 2, iy + ih / 2 - SpriteText.getHeightOfString("Decline") / 2);
                    if (justClicked && new Rectangle(ix, iy, iw, ih).Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        Log.trace("Declined " + ((PlatformConnection)pendingConns[0]).friend.displayName);
                        pendingConns.Remove(pendingConns[0]);
                    }

                    justClicked = false;
                }
                else
                {
                    int x = buttonX, y = buttonY1, w = buttonW, h = buttonH;
                    String str = "Start";

                    Util.drawStr("Local IP: ", x, buttonY1, Color.White);
                    Util.drawStr("External IP: ", x, buttonY1 + 35, Color.White);
                    Util.drawStr("Port: ", x, buttonY1 + 70, Color.White);
                    Util.drawStr(localIp, x + 200, buttonY1, Color.White);
                    Util.drawStr(externalIp, x + 200, buttonY1 + 35, Color.White);
                    Util.drawStr(Multiplayer.portStr, x + 200, buttonY1 + 70, Color.White);

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

        private void onFriendSelected(Friend friend)
        {
            Log.trace("onFriendSelected " + friend.displayName);
            if (modeInit == null && Multiplayer.mode == Mode.Client)
            {
                IConnection conn = IPlatform.instance.connectToFriend(friend);
                pendingClient = new Client(conn);
            }
        }

        private void onFriendConnected(Friend friend, PlatformConnection conn)
        {
            Log.trace("onFriendConnected " + friend.displayName + " " + conn);
            if (modeInit != null && Multiplayer.mode == Mode.Host)
            {
                pendingConns.Add(conn);
            }
        }
    }
}