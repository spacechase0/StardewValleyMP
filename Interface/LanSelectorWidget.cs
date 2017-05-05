using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyMP.Interface
{
    public class LanSelectorWidget
    {
        public Action<LanEntry> onEntrySelected;

        public class LanEntry
        {
            public string name;
            public IPEndPoint server;
            public int port;
            public LanEntry(string theName, IPEndPoint theServer, int thePort)
            {
                name = theName;
                server = theServer;
                port = thePort;
            }
        }

        private List<LanEntry> entries = new List< LanEntry >();

        private int x, y;
        private int w, h;

        private int scroll = 0;
        private Rectangle scrollbarBack;
        private Rectangle scrollbar;

        private bool dragScroll = false;

        public LanSelectorWidget( int x, int y, int w, int h )
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;

            scrollbarBack = new Rectangle(x + w - Game1.pixelZoom * 6 - 16, y + 16, Game1.pixelZoom * 6, h - 28);
            scrollbar = new Rectangle(scrollbarBack.X + 2, scrollbarBack.Y + 2, 6 * Game1.pixelZoom - 4, (int)((5.0 / entries.Count) * scrollbarBack.Height) - 4);
        }

        ~LanSelectorWidget()
        {
            LanDiscovery.stop();
        }

        public void start()
        {
            LanDiscovery.onDiscovery = new Action<string, IPEndPoint, int>(onDiscovery);
            LanDiscovery.startClient();
        }

        private bool justClicked = false;
        public void leftClick( int x, int y )
        {
            if ( scrollbarBack.Contains( x, y ) )
            {
                dragScroll = true;
            }
            else
            {
                justClicked = true;
            }
        }

        public void leftRelease(int x, int y)
        {
            dragScroll = false;
        }

        public void mouseScroll( int dir )
        {
            scroll += dir * 1;
            if (scroll > 0)
                scroll = 0;
            else if (scroll < entries.Count * -80 + h - 48 - 4)
                scroll = entries.Count * -80 + h - 48 - 4;
        }

        public void update( GameTime time )
        {
            if ( dragScroll )
            {
                int my = Game1.getMouseY();
                int relY = my - (scrollbarBack.Y + 2 + scrollbar.Height / 2);
                relY = Math.Max(0, relY);
                relY = Math.Min(relY, scrollbarBack.Height - 4 - scrollbar.Height);
                float percY = relY / (scrollbarBack.Height - 4f - scrollbar.Height);
                int totalY = entries.Count * 80 - h + 48 + 4;
                scroll = -(int)( totalY * percY );
            }
        }

        public void draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, Color.White, (float)Game1.pixelZoom, true);

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null,
                    new RasterizerState() { ScissorTestEnable = true } );
            b.GraphicsDevice.ScissorRectangle = new Rectangle(x + 24, y + 20, scrollbarBack.Left - (x + 24), h - 36);
            {
                int si = scroll / -80;
                for (int i = Math.Max(0, si - 1); i < Math.Min(entries.Count, si + h / 80 + 1); ++i)
                {
                    LanEntry entry = entries[i];
                    int ix = x + 32;
                    int iy = y + 32 + 4 + i * 80 + scroll;

                    Rectangle area = new Rectangle(ix - 8, iy - 8, w - 40 - Game1.pixelZoom * 6, 80);
                    if (area.Contains(Game1.getMouseX(), Game1.getMouseY()) )
                    {
                        b.Draw(Util.WHITE_1X1, area, new Color(200, 32, 32, 64));
                        if ( justClicked )
                        {
                            Log.trace("Clicked on " + entry.name);
                            if (onEntrySelected != null)
                                onEntrySelected.Invoke(entry);
                        }
                    }
                    
                    SpriteText.drawString(b, entry.name, ix + 8, iy + 8);
                    SpriteText.drawString(b, entry.server.Address + ":" + entry.port, ix + area.Width - SpriteText.getWidthOfString(entry.server.Address + ":" + entry.port), iy + 8);
                }
            }
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (entries.Count > 5)
            {
                scrollbar.Y = scrollbarBack.Y + 2 + ( int )( ((scroll / -80f) / (entries.Count - (h - 64 + 8) / 80f)) * ( scrollbarBack.Height - scrollbar.Height ) );

                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollbarBack.X, scrollbarBack.Y, scrollbarBack.Width, scrollbarBack.Height, Color.DarkGoldenrod, (float)Game1.pixelZoom, false);
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollbar.X, scrollbar.Y, scrollbar.Width, scrollbar.Height, Color.Gold, (float)Game1.pixelZoom, false);
            }

            justClicked = false;
        }

        private void onDiscovery(string name, IPEndPoint server, int port)
        {
            foreach (var entry in entries)
            {
                //Log.debug("" + entry.server.Address + " " + server.Address + " " + (entry.server.Address == server.Address) + " " + entry.server.Equals(server.Address));
                if (entry.server.Address.ToString() == server.Address.ToString() && entry.port == port)
                {
                    Log.trace("Duplicate LAN discovery");
                    entry.name = name;
                    return;
                }
            }

            Log.info("Found server on LAN: " + name + " @ " + server + ":" + port);
            entries.Add(new LanEntry(name, server, port));

            scrollbar = new Rectangle(scrollbarBack.X + 2, scrollbarBack.Y + 2, 6 * Game1.pixelZoom - 4, (int)((5.0 / entries.Count) * scrollbarBack.Height) - 4);
        }
    }
}
