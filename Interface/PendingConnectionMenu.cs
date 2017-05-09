using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewValleyMP.Connections;
using StardewValleyMP.Platforms;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.BellsAndWhistles;

namespace StardewValleyMP.Interface
{
    public class PendingConnectionMenu : IClickableMenu
    {
        private PlatformConnection conn;

        public PendingConnectionMenu( PlatformConnection theConn)
        {
            conn = theConn;
        }

        private bool justClicked = false;
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            justClicked = true;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void draw(SpriteBatch b)
        {
            Friend friend = conn.friend;

            int ix = xPositionOnScreen + width / 5;
            int iw = width / 5 * 3;
            int ih = 80 * 2 + 64;
            int iy = (Game1.viewport.Height - ih) / 2;

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), ix, iy, iw, ih, Color.White, Game1.pixelZoom, true);
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
            if (justClicked && new Rectangle(ix, iy, iw, ih).Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                Log.trace("Accepted " + conn.friend.displayName);
                Task.Run(() =>
                {
                    var platConn = conn as PlatformConnection;
                    platConn.accept();
                    Multiplayer.server.addClient(platConn, true);
                });
                Game1.exitActiveMenu();
            }

            ix += iw + 40;

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), ix, iy, iw, ih, new Rectangle(ix, iy, iw, ih).Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, true);
            SpriteText.drawString(b, "Decline", ix + iw / 2 - SpriteText.getWidthOfString("Decline") / 2, iy + ih / 2 - SpriteText.getHeightOfString("Decline") / 2);
            if (justClicked && new Rectangle(ix, iy, iw, ih).Contains(Game1.getMouseX(), Game1.getMouseY()))
            {
                Log.trace("Declined " + conn.friend.displayName);
                Game1.exitActiveMenu();
            }

            justClicked = false;

            drawMouse(b);
        }
    }
}
