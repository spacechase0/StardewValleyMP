using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace StardewValleyMP.Vanilla
{
    public class NewSaveGameMenu : SaveGameMenu
    {
        private IEnumerator<int> loader;

        private int completePause = -1;

        //public bool quit;

        //public bool hasDrawn;

        ////////////////////////////////////////
        private SparklingText waitingText;
        ////////////////////////////////////////
        private SparklingText saveText;

        private int margin = 500;

        private StringBuilder _stringBuilder = new StringBuilder();

        private float _ellipsisDelay = 0.5f;

        private int _ellipsisCount;

        public NewSaveGameMenu()
        {
            ////////////////////////////////////////
            //Log.Async("New save menu created");
            /*if ( Multiplayer.mode == Mode.Host )
            {
                //Multiplayer.server.playing = false;
            }
            else if ( Multiplayer.mode == Mode.Client )
            {
                Multiplayer.client.stage = Client.NetStage.Waiting;
                //Multiplayer.sendFunc(new ClientFarmerDataPacket(Util.serialize<SFarmer>(Game1.player)));
                Multiplayer.sendFunc(new NextDayPacket());
            }*/
            this.waitingText = new SparklingText(Game1.dialogueFont, "Waiting on host", Color.DodgerBlue, Color.Black * 0.001f, false, 0.1, 1500, Game1.tileSize / 2, 500);
            ////////////////////////////////////////
            this.saveText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11378", new object[0]), Color.LimeGreen, Color.Black * 0.001f, false, 0.1, 1500, Game1.tileSize / 2, 500);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void update(GameTime time)
        {
            if (this.quit)
            {
                if ( Game1.currentLoader.Current < 100 )
                {
                    Game1.currentLoader.MoveNext();
                }
                else Game1.exitActiveMenu();
                return;
            }

            ////////////////////////////////////////
            if (Multiplayer.mode == Mode.Client)
            {
                Log.info("Reloading world for next day");

                // Yes, it is necessary to do this again (previously done when the next day packet was sent before the fade)
                // This time newDayAfterFade has run, and so mail and stuff has changed
                var it = NewSaveGame.Save(true);
                while (it.Current < 100)
                {
                    it.MoveNext();
                    Thread.Sleep(5);
                }

                Multiplayer.client.processDelayedPackets();
                NewSaveGame.Load("MEOW", true);
                quit = true;
                return;
            }
            ////////////////////////////////////////
            
            if (!Game1.saveOnNewDay)
            {
                this.quit = true;
                if (Game1.activeClickableMenu.Equals(this))
                {
                    Game1.player.checkForLevelTenStatus();
                    Game1.exitActiveMenu();
                }
                return;
            }
            if (this.loader != null)
            {
                this.loader.MoveNext();
                if (this.loader.Current >= 100)
                {
                    this.margin -= time.ElapsedGameTime.Milliseconds;
                    if (this.margin <= 0)
                    {
                        Game1.playSound("money");
                        this.completePause = 1500;
                        this.loader = null;
                        Game1.game1.IsSaving = false;
                    }
                }
                this._ellipsisDelay -= (float)time.ElapsedGameTime.TotalSeconds;
                if (this._ellipsisDelay <= 0f)
                {
                    this._ellipsisDelay += 0.75f;
                    this._ellipsisCount++;
                    if (this._ellipsisCount > 3)
                    {
                        this._ellipsisCount = 1;
                    }
                }
            }
            else if (this.hasDrawn && this.completePause == -1)
            {
                ////////////////////////////////////////
                if (Multiplayer.mode == Mode.Host)
                {
                    foreach (Server.Client client in Multiplayer.server.clients)
                    {
                        // They should have sent their farmer data again.
                        // We can update their stuff before the new day.
                        client.processDelayedPackets();
                    }
                }
                ////////////////////////////////////////
                Game1.game1.IsSaving = true;
                this.loader = NewSaveGame.Save(); // SaveGame -> NewSaveGame
            }
            if (this.completePause >= 0)
            {
                this.completePause -= time.ElapsedGameTime.Milliseconds;
                this.saveText.update(time);
                if (this.completePause < 0)
                {
                    this.quit = true;
                    this.completePause = -9999;
                    if (Game1.activeClickableMenu.Equals(this))
                    {
                        Game1.player.checkForLevelTenStatus();
                        Game1.exitActiveMenu();
                    }
                    Game1.currentLocation.resetForPlayerEntry();
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            ////////////////////////////////////////
            /*
            if ( Multiplayer.waitingOnOthers() )
            {
                this.waitingText.draw(b, new Vector2((float)Game1.tileSize, (float)(Game1.viewport.Height - Game1.tileSize)));
                this.hasDrawn = true;
                return;
            }*/
            ////////////////////////////////////////
            Vector2 vector = new Vector2((float)Game1.tileSize, (float)(Game1.viewport.Height - Game1.tileSize));
            Vector2 renderSize = new Vector2((float)Game1.tileSize, (float)Game1.tileSize);
            vector = Utility.makeSafe(vector, renderSize);
            if (this.completePause >= 0)
            {
                this.saveText.draw(b, new Vector2((float)Game1.tileSize, (float)(Game1.viewport.Height - Game1.tileSize)));
            }
            else
            {
                this._stringBuilder.Clear();
                this._stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11381", new object[0]));
                for (int i = 0; i < this._ellipsisCount; i++)
                {
                    this._stringBuilder.Append(".");
                }
                b.DrawString(Game1.dialogueFont, this._stringBuilder, vector, Color.White);
            }
            this.hasDrawn = true;
        }

        public void Dispose()
        {
            Game1.game1.IsSaving = false;
        }
    }
}
