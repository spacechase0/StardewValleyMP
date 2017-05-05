using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValleyMP.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyMP.Interface
{
    public class FriendSelectorWidget
    {
        public Action<Friend> onSelectFriend;

        private bool online;
        private List<Friend> friends;

        private int x, y;
        private int w, h;

        private int scroll = 0;
        private Rectangle scrollbarBack;
        private Rectangle scrollbar;

        private bool dragScroll = false;

        public FriendSelectorWidget( bool onlineOnly, int x, int y, int w, int h )
        {
            online = onlineOnly;

            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;

            friends = online ? IPlatform.instance.getOnlineFriends() : IPlatform.instance.getFriends();
            if (friends.Count > 0)
            {
                scrollbarBack = new Rectangle(x + w - Game1.pixelZoom * 6 - 16, y + 16, Game1.pixelZoom * 6, h - 28);
                scrollbar = new Rectangle(scrollbarBack.X + 2, scrollbarBack.Y + 2, 6 * Game1.pixelZoom - 4, (int)((5.0 / friends.Count) * scrollbarBack.Height) - 4);
            }
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
            else if (scroll < friends.Count * -80 + h - 48 - 4)
                scroll = friends.Count * -80 + h - 48 - 4;
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
                int totalY = friends.Count * 80 - h + 48 + 4;
                scroll = -(int)( totalY * percY );
            }
        }

        public void draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, Color.White, (float)Game1.pixelZoom, true);

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null,
                    new RasterizerState() { ScissorTestEnable = true } );
            if ( friends.Count > 0 )
                b.GraphicsDevice.ScissorRectangle = new Rectangle(x + 24, y + 20, scrollbarBack.Left - (x + 24), h - 36);
            {
                int si = scroll / -80;
                for (int i = Math.Max(0, si - 1); i < Math.Min(friends.Count, si + h / 80 + 1); ++i)
                {
                    Friend friend = friends[i];
                    int ix = x + 32;
                    int iy = y + 32 + 4 + i * 80 + scroll;

                    Rectangle area = new Rectangle(ix - 8, iy - 8, w - 40 - Game1.pixelZoom * 6, 80);
                    if (area.Contains(Game1.getMouseX(), Game1.getMouseY()) )
                    {
                        b.Draw(Util.WHITE_1X1, area, new Color(200, 32, 32, 64));
                        if ( justClicked )
                        {
                            Log.trace("Clicked on " + friend.displayName);
                            if (onSelectFriend != null)
                                onSelectFriend.Invoke(friend);
                        }
                    }

                    b.Draw(friend.avatar, new Rectangle(ix, iy, 64, 64), Color.White);
                    SpriteText.drawString(b, friend.displayName, ix + 88, iy + 8);
                }
            }
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (friends.Count > 5)
            {
                scrollbar.Y = scrollbarBack.Y + 2 + ( int )( ((scroll / -80f) / (friends.Count - (h - 64 + 8) / 80f)) * ( scrollbarBack.Height - scrollbar.Height ) );

                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollbarBack.X, scrollbarBack.Y, scrollbarBack.Width, scrollbarBack.Height, Color.DarkGoldenrod, (float)Game1.pixelZoom, false);
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollbar.X, scrollbar.Y, scrollbar.Width, scrollbar.Height, Color.Gold, (float)Game1.pixelZoom, false);
            }

            justClicked = false;
        }
    }
}
