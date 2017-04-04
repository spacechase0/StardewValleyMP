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
        private bool online;
        private List<Friend> friends;

        private int x, y;
        private int w, h;

        private int scroll = 0;

        public FriendSelectorWidget( bool onlineOnly, int x, int y, int w, int h )
        {
            online = onlineOnly;

            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;

            friends = false&&online ? IPlatform.instance.getOnlineFriends() : IPlatform.instance.getFriends();
        }

        public void mouseScroll( int dir )
        {
            scroll += dir * 1;
            if (scroll > 0)
                scroll = 0;
            else if (scroll < friends.Count * -80 + h - 48)
                scroll = friends.Count * -80 + h - 48;
        }

        public void update( GameTime time )
        {
        }

        public void draw( SpriteBatch b )
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, Color.White, (float)Game1.pixelZoom, true);

            for ( int i = 0; i < friends.Count; ++i )
            {
                Friend friend = friends[i];
                int ix = x + 32;
                int iy = y + 32 + i * 80 + scroll;

                b.Draw(friend.avatar, new Rectangle(ix, iy, 64, 64), Color.White);
                SpriteText.drawString(b, friend.displayName, ix + 88, iy + 8);
            }
        }
    }
}
