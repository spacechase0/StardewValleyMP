using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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

        int scroll = 0;

        public FriendSelectorWidget( bool onlineOnly, int x, int y, int w, int h )
        {
            online = onlineOnly;

            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;

            friends = online ? IPlatform.instance.getOnlineFriends() : IPlatform.instance.getFriends();
        }

        public void update( GameTime time )
        {
        }

        public void draw( SpriteBatch b )
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), x, y, w, h, Color.White, (float)Game1.pixelZoom, true);
        }
    }
}
