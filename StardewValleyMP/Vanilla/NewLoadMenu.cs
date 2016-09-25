using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StardewValleyMP.Vanilla
{
    // Whole class copied from LoadGameMenu because annoying private variables
    // Changes are SaveGame.Load -> NewSaveGame.Load
    public class NewLoadMenu : IClickableMenu
    {
		public const int itemsPerPage = 4;

		private List<ClickableComponent> gamesToLoadButton = new List<ClickableComponent>();

		private List<ClickableTextureComponent> deleteButtons = new List<ClickableTextureComponent>();

		private int currentItemIndex;

		private int timerToLoad;

		private int selected = -1;

		private int selectedForDelete = -1;

		private ClickableTextureComponent upArrow;

		private ClickableTextureComponent downArrow;

		private ClickableTextureComponent scrollBar;

		private ClickableTextureComponent okDeleteButton;

		private ClickableTextureComponent cancelDeleteButton;

		private bool scrolling;

		private bool deleteConfirmationScreen;

		private List<Farmer> saveGames = new List<Farmer>();

		private Rectangle scrollBarRunner;

		private string hoverText = "";

		private bool loading;

		public NewLoadMenu() : base(Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false)
		{
			this.upArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(421, 459, 11, 12), (float)Game1.pixelZoom);
			this.downArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(421, 472, 11, 12), (float)Game1.pixelZoom);
			this.scrollBar = new ClickableTextureComponent(new Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(435, 463, 6, 10), (float)Game1.pixelZoom);
			this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, this.height - Game1.tileSize - this.upArrow.bounds.Height - Game1.pixelZoom * 7);
			this.okDeleteButton = new ClickableTextureComponent(new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X - Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), "OK", null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false, false);
			this.cancelDeleteButton = new ClickableTextureComponent(new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X + Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), "Cancel", null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1), 1f, false, false);
			for (int i = 0; i < 4; i++)
			{
				this.gamesToLoadButton.Add(new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4 + i * (this.height / 4), this.width - Game1.tileSize / 2, this.height / 4 + Game1.pixelZoom), string.Concat(i)));
				this.deleteButtons.Add(new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize - Game1.pixelZoom, this.yPositionOnScreen + Game1.tileSize / 2 + Game1.pixelZoom + i * (this.height / 4), 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "Delete File", Game1.mouseCursors, new Rectangle(322, 498, 12, 12), (float)Game1.pixelZoom * 3f / 4f));
			}
			string text = Path.Combine(new string[]
			{
				Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves")
			});
			if (Directory.Exists(text))
			{
				string[] directories = Directory.GetDirectories(text);
				if (directories.Count<string>() > 0)
				{
					string[] array = directories;
					for (int j = 0; j < array.Length; j++)
					{
						string text2 = array[j];
						try
						{
							Stream stream = null;
							try
							{
								stream = File.Open(Path.Combine(text, text2, "SaveGameInfo"), FileMode.Open);
							}
							catch (IOException)
							{
								if (stream != null)
								{
									stream.Close();
								}
							}
							if (stream != null)
							{
								Farmer farmer = (Farmer)SaveGame.farmerSerializer.Deserialize(stream);
								SaveGame.loadDataToFarmer(farmer, farmer);
								farmer.favoriteThing = text2.Split(new char[]
								{
									Path.DirectorySeparatorChar
								}).Last<string>();
								this.saveGames.Add(farmer);
								stream.Close();
							}
						}
						catch (Exception)
						{
						}
					}
				}
			}
			this.saveGames.Sort();
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			this.upArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(421, 459, 11, 12), (float)Game1.pixelZoom);
			this.downArrow = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(421, 472, 11, 12), (float)Game1.pixelZoom);
			this.scrollBar = new ClickableTextureComponent(new Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom), "", "", Game1.mouseCursors, new Rectangle(435, 463, 6, 10), (float)Game1.pixelZoom);
			this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, this.height - Game1.tileSize - this.upArrow.bounds.Height - Game1.pixelZoom * 7);
			this.okDeleteButton = new ClickableTextureComponent(new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X - Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), "OK", null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false, false);
			this.cancelDeleteButton = new ClickableTextureComponent(new Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X + Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), "Cancel", null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1), 1f, false, false);
			this.gamesToLoadButton.Clear();
			this.deleteButtons.Clear();
			for (int i = 0; i < 4; i++)
			{
				this.gamesToLoadButton.Add(new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4 + i * (this.height / 4), this.width - Game1.tileSize / 2, this.height / 4 + Game1.pixelZoom), string.Concat(i)));
				this.deleteButtons.Add(new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize - Game1.pixelZoom, this.yPositionOnScreen + Game1.tileSize / 2 + Game1.pixelZoom + i * (this.height / 4), 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "Delete File", Game1.mouseCursors, new Rectangle(322, 498, 12, 12), (float)Game1.pixelZoom * 3f / 4f));
			}
		}

		public override void performHoverAction(int x, int y)
        {
            ////////////////////////////////////////
            if (loading && timerToLoad <= 0)
            {
                base.performHoverAction(x, y);
                Rectangle r = new Rectangle( buttonX, buttonY1, buttonW, buttonH );
                /*
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
                }*/
                return;
            }
            ////////////////////////////////////////

			this.hoverText = "";
			base.performHoverAction(x, y);
			if (!this.deleteConfirmationScreen)
			{
				this.upArrow.tryHover(x, y, 0.1f);
				this.downArrow.tryHover(x, y, 0.1f);
				this.scrollBar.tryHover(x, y, 0.1f);
				foreach (ClickableTextureComponent current in this.deleteButtons)
				{
					current.tryHover(x, y, 0.2f);
					if (current.containsPoint(x, y))
					{
						this.hoverText = "Delete File";
						return;
					}
				}
				if (!this.scrolling)
				{
					for (int i = 0; i < this.gamesToLoadButton.Count<ClickableComponent>(); i++)
					{
						if (this.currentItemIndex + i < this.saveGames.Count<Farmer>() && this.gamesToLoadButton[i].containsPoint(x, y))
						{
							if (this.gamesToLoadButton[i].scale == 1f)
							{
								Game1.playSound("Cowboy_gunshot");
							}
							this.gamesToLoadButton[i].scale = Math.Min(this.gamesToLoadButton[i].scale + 0.03f, 1.1f);
						}
						else
						{
							this.gamesToLoadButton[i].scale = Math.Max(1f, this.gamesToLoadButton[i].scale - 0.03f);
						}
					}
					return;
				}
				return;
			}
			this.okDeleteButton.tryHover(x, y, 0.1f);
			this.cancelDeleteButton.tryHover(x, y, 0.1f);
			if (this.okDeleteButton.containsPoint(x, y))
			{
				this.hoverText = "";
				return;
			}
			if (this.cancelDeleteButton.containsPoint(x, y))
			{
				this.hoverText = "Cancel";
			}
		}

		public override void leftClickHeld(int x, int y)
		{
			base.leftClickHeld(x, y);
			if (this.scrolling)
			{
				int y2 = this.scrollBar.bounds.Y;
				this.scrollBar.bounds.Y = Math.Min(this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this.scrollBar.bounds.Height, Math.Max(y, this.yPositionOnScreen + this.upArrow.bounds.Height + Game1.pixelZoom * 5));
				float num = (float)(y - this.scrollBarRunner.Y) / (float)this.scrollBarRunner.Height;
				this.currentItemIndex = Math.Min(this.saveGames.Count - 4, Math.Max(0, (int)((float)this.saveGames.Count * num)));
				this.setScrollBarToCurrentIndex();
				if (y2 != this.scrollBar.bounds.Y)
				{
					Game1.playSound("shiny4");
				}
			}
		}

		public override void releaseLeftClick(int x, int y)
		{
			base.releaseLeftClick(x, y);
			this.scrolling = false;
		}

		private void setScrollBarToCurrentIndex()
		{
			if (this.saveGames.Count<Farmer>() > 0)
			{
				this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.saveGames.Count - 4 + 1) * this.currentItemIndex + this.upArrow.bounds.Bottom + Game1.pixelZoom;
				if (this.currentItemIndex == this.saveGames.Count<Farmer>() - 4)
				{
					this.scrollBar.bounds.Y = this.downArrow.bounds.Y - this.scrollBar.bounds.Height - Game1.pixelZoom;
				}
			}
		}

		public override void receiveScrollWheelAction(int direction)
		{
			base.receiveScrollWheelAction(direction);
			if (direction > 0 && this.currentItemIndex > 0)
			{
				this.upArrowPressed();
				return;
			}
			if (direction < 0 && this.currentItemIndex < Math.Max(0, this.saveGames.Count<Farmer>() - 4))
			{
				this.downArrowPressed();
			}
		}

		private void downArrowPressed()
		{
			this.downArrow.scale = this.downArrow.baseScale;
			this.currentItemIndex++;
			Game1.playSound("shwip");
			this.setScrollBarToCurrentIndex();
		}

		private void upArrowPressed()
		{
			this.upArrow.scale = this.upArrow.baseScale;
			this.currentItemIndex--;
			Game1.playSound("shwip");
			this.setScrollBarToCurrentIndex();
		}

		public void deleteFile(int which)
		{
			Farmer farmer = this.saveGames[which];
			string favoriteThing = farmer.favoriteThing;
			string path = Path.Combine(new string[]
			{
				Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), favoriteThing)
			});
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
				this.saveGames.Remove(farmer);
			}
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            ////////////////////////////////////////
            if (loading && timerToLoad <= 1)
            {
                base.receiveLeftClick(x, y, playSound);

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
                    }
                    r.Y = buttonY3;
                    if (r.Contains(x, y))
                    {
                        Multiplayer.mode = Mode.Client;
                        didModeSelect = true;
                    }
                }
                else if ( modeInit == null )
                {
                    if (ipBox != null) ipBox.Update();
                    if (portBox != null) portBox.Update();

                    Rectangle r = new Rectangle(buttonX, buttonY3, buttonW, buttonH);
                    if (r.Contains(x, y))
                    {
                        Multiplayer.portStr = portBox.Text;
                        if (Multiplayer.mode == Mode.Host)
                        {
                            modeInit = new Thread(Multiplayer.startHost);
                        }
                        else if ( Multiplayer.mode == Mode.Client )
                        {
                            Multiplayer.ipStr = ipBox.Text;
                            modeInit = new Thread(Multiplayer.startClient);
                        }
                        modeInit.Start();
                        ChatMenu.chat.Add(new ChatEntry(null, "NOTE: Chat doesn't work on the connection menu."));
                    }
                }
                else if ( Multiplayer.mode == Mode.Host )
                {
                    Rectangle r = new Rectangle(buttonX, buttonY3, buttonW, buttonH);
                    if (r.Contains(x, y))
                    {
                        StardewModdingAPI.Log.Async("Stopping listener, beginning loading");
                        Multiplayer.listener.Server.Close();
                        readyToLoad = true;
                    }
                }

                return;
            }
            ////////////////////////////////////////

			if (this.timerToLoad > 0 || this.loading)
			{
				return;
			}
			if (!this.deleteConfirmationScreen)
			{
				base.receiveLeftClick(x, y, playSound);
				if (this.downArrow.containsPoint(x, y) && this.currentItemIndex < Math.Max(0, this.saveGames.Count<Farmer>() - 4))
				{
					this.downArrowPressed();
				}
				else if (this.upArrow.containsPoint(x, y) && this.currentItemIndex > 0)
				{
					this.upArrowPressed();
				}
				else if (this.scrollBar.containsPoint(x, y))
				{
					this.scrolling = true;
				}
				else if (!this.downArrow.containsPoint(x, y) && x > this.xPositionOnScreen + this.width && x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 && y > this.yPositionOnScreen && y < this.yPositionOnScreen + this.height)
				{
					this.scrolling = true;
					this.leftClickHeld(x, y);
					this.releaseLeftClick(x, y);
				}
				if (this.selected == -1)
				{
					for (int i = 0; i < this.deleteButtons.Count<ClickableTextureComponent>(); i++)
					{
						if (this.deleteButtons[i].containsPoint(x, y) && i < this.saveGames.Count<Farmer>() && !this.deleteConfirmationScreen)
						{
							this.deleteConfirmationScreen = true;
							Game1.playSound("drumkit6");
							this.selectedForDelete = this.currentItemIndex + i;
							return;
						}
					}
				}
				if (!this.deleteConfirmationScreen)
				{
					for (int j = 0; j < this.gamesToLoadButton.Count<ClickableComponent>(); j++)
					{
						if (this.gamesToLoadButton[j].containsPoint(x, y) && j < this.saveGames.Count<Farmer>())
						{
							this.timerToLoad = 2150;
							this.loading = true;
							Game1.playSound("select");
							this.selected = this.currentItemIndex + j;
							return;
						}
					}
				}
				this.currentItemIndex = Math.Max(0, Math.Min(this.saveGames.Count<Farmer>() - 4, this.currentItemIndex));
				return;
			}
			if (this.cancelDeleteButton.containsPoint(x, y))
			{
				this.deleteConfirmationScreen = false;
				this.selectedForDelete = -1;
				Game1.playSound("smallSelect");
				return;
			}
			if (this.okDeleteButton.containsPoint(x, y))
			{
				this.deleteFile(this.selectedForDelete);
				this.deleteConfirmationScreen = false;
				Game1.playSound("trashcan");
			}
		}

        ////////////////////////////////////////
        // NOTES:
        // Start like this
        // Once a mode is selected, didModeSelect -> true
        // If singleplayer, load as if the mod wasn't here.
        //  Otherwise, wait for IP/port and Listen/Connect to be pressed. This will set modeInit to something and start it
        // (This is in another thread so the game doesn't freeze up while listening for clients and stuff.)
        // After that, go to the 'lobby'.
        // If something goes wrong in the modeInit thread, go back to character select.
        //  Otherwise, wait for the game to start.
        private TextBox ipBox;
        private TextBox portBox;
        public bool readyToLoad = false;
        public bool didModeSelect = false;
        private Thread modeInit;
        public static Farmer pendingSelected = null;
        ////////////////////////////////////////
		public override void update(GameTime time)
		{
			base.update(time);
			if (this.timerToLoad > 0)
			{
				this.timerToLoad -= time.ElapsedGameTime.Milliseconds;
				if (this.timerToLoad <= 0)
                {
                    ////////////////////////////////////////
                    this.timerToLoad = 1;

                    pendingSelected = saveGames[selected];
                    if ( didModeSelect )
                    {
                        if (modeInit == null)
                        {
                            if (portBox == null)
                            {
                                if (Multiplayer.mode == Mode.Client)
                                {
                                    ipBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
                                    ipBox.X = buttonX + buttonW / 2 - (SpriteText.getWidthOfString("IP Address:") + ipBox.Width + 20) / 2 + SpriteText.getWidthOfString("IP Address:") + 20;
                                    ipBox.Y = buttonY1 + buttonH / 2 - ipBox.Height / 2;
                                    ipBox.Text = Multiplayer.ipStr;
                                }

                                portBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
                                portBox.X = buttonX + buttonW / 2 - (SpriteText.getWidthOfString("IP Address:") + portBox.Width + 20) / 2 + SpriteText.getWidthOfString("IP Address:") + 20;
                                portBox.Y = buttonY2 + buttonH / 2 - portBox.Height / 2;
                                portBox.Text = Multiplayer.portStr;
                            }
                        }
                        else
                        {
                            if (Multiplayer.mode == Mode.Client && Multiplayer.client != null)
                                readyToLoad = true;
                        }
                    }

                    if (Multiplayer.problemStarting)
                    {
                        didModeSelect = false;
                        this.loading = false;
                        this.timerToLoad = 0;
                        this.selected = -1;
                        Util.SetInstanceField(typeof(TitleMenu), Game1.activeClickableMenu, "subMenu", new NewLoadMenu());
                        return;
                    }

                    if (!readyToLoad) return;

					if ( !NewSaveGame.Load(this.saveGames[this.selected].favoriteThing) )
                    {
                        didModeSelect = false;
                        this.loading = false;
                        this.timerToLoad = 0;
                        this.selected = -1;
                        Util.SetInstanceField(typeof(TitleMenu), Game1.activeClickableMenu, "subMenu", new NewLoadMenu());
                        return;
                    }
                    Multiplayer.lobby = false;
                    this.timerToLoad = 0;
                    
                    ////////////////////////////////////////

					for (int i = 0; i < this.saveGames.Count; i++)
					{
						if (i != this.selected)
						{
							this.saveGames[i].unload();
						}
					}

                    //if (Multiplayer.mode == Mode.Singleplayer) ////////////////////////////////////////
					    Game1.exitActiveMenu();
				}
			}
		}

        ////////////////////////////////////////
        private int buttonX;
        private int buttonY1;
        private int buttonY2;
        private int buttonY3;
        private int buttonW;
        private int buttonH;
        ////////////////////////////////////////
		public override void draw(SpriteBatch b)
        {
            ////////////////////////////////////////
            if ( loading && timerToLoad <= 1 )
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
                else if ( modeInit == null )
                {
                    int x = buttonX, y = buttonY3, w = buttonW, h = buttonH;
                    String str = (Multiplayer.mode == Mode.Host ? "Listen" : "Connect");

                    if (ipBox != null)
                    {
                        SpriteText.drawString(b, "IP Address:", ipBox.X - SpriteText.getWidthOfString( "IP Address:" ) - 20, ipBox.Y );
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
                else if ( Multiplayer.server != null)
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
                    foreach (KeyValuePair< byte, Farmer > other in Multiplayer.client.others)
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
                return;
            }
            ////////////////////////////////////////

			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height + Game1.tileSize / 2, Color.White, (float)Game1.pixelZoom, true);
			for (int i = 0; i < this.gamesToLoadButton.Count<ClickableComponent>(); i++)
			{
				if (this.currentItemIndex + i < this.saveGames.Count<Farmer>())
				{
					IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), this.gamesToLoadButton[i].bounds.X, this.gamesToLoadButton[i].bounds.Y, this.gamesToLoadButton[i].bounds.Width, this.gamesToLoadButton[i].bounds.Height, ((this.currentItemIndex + i == this.selected && this.timerToLoad % 150 > 75 && this.timerToLoad > 1000) || (this.selected == -1 && this.gamesToLoadButton[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !this.scrolling && !this.deleteConfirmationScreen)) ? (this.deleteButtons[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.White : Color.Wheat) : Color.White, (float)Game1.pixelZoom, false);
					SpriteText.drawString(b, this.currentItemIndex + i + 1 + ".", this.gamesToLoadButton[i].bounds.X + Game1.pixelZoom * 7 + Game1.tileSize / 2 - SpriteText.getWidthOfString(this.currentItemIndex + i + 1 + ".") / 2, this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 9, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
					SpriteText.drawString(b, this.saveGames[this.currentItemIndex + i].Name, this.gamesToLoadButton[i].bounds.X + Game1.tileSize * 2 + Game1.pixelZoom * 9, this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 9, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
					b.Draw(Game1.shadowTexture, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize + Game1.tileSize - Game1.pixelZoom), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize * 2 + Game1.pixelZoom * 4)), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, 0.8f);
					this.saveGames[this.currentItemIndex + i].FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(0, 0, false, false, null, false), 0, new Rectangle(0, 0, 16, 32), new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize / 4 + Game1.tileSize + Game1.pixelZoom * 3), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 5)), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, this.saveGames[this.currentItemIndex + i]);
					Utility.drawTextWithShadow(b, this.saveGames[this.currentItemIndex + i].dateStringForSaveGame, Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize * 2 + Game1.pixelZoom * 8), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 10)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					Utility.drawTextWithShadow(b, this.saveGames[this.currentItemIndex + i].farmName + " Farm", Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 2) - Game1.dialogueFont.MeasureString(this.saveGames[this.currentItemIndex + i].farmName + " Farm").X, (float)(this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 11)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					int num = (int)Game1.dialogueFont.MeasureString(Utility.getNumberWithCommas(this.saveGames[this.currentItemIndex + i].Money) + "g").X;
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 25 - num), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11)), new Rectangle(193, 373, 9, 9), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Utility.getNumberWithCommas(this.saveGames[this.currentItemIndex + i].Money) + "g", Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 15 - num), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 11), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 9)), new Rectangle(595, 1748, 9, 11), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
					Utility.drawTextWithShadow(b, Utility.getHoursMinutesStringFromMilliseconds(this.saveGames[this.currentItemIndex + i].millisecondsPlayed), Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
					if (this.deleteButtons.Count<ClickableTextureComponent>() > i)
					{
						this.deleteButtons[i].draw(b, Color.White * 0.75f, 1f);
					}
				}
			}
			if (this.saveGames.Count<Farmer>() == 0)
			{
				SpriteText.drawStringHorizontallyCenteredAt(b, "No Saved Games Found", Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.X, Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.Y, 999999, -1, 999999, 1f, 0.88f, false, -1);
			}
			this.upArrow.draw(b);
			this.downArrow.draw(b);
			if (this.saveGames.Count<Farmer>() > 4)
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.scrollBarRunner.X, this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White, (float)Game1.pixelZoom, false);
				this.scrollBar.draw(b);
			}
			if (this.deleteConfirmationScreen)
			{
				b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * 0.75f);
				SpriteText.drawString(b, "Really delete file: " + this.saveGames[this.selectedForDelete].name + "?", (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize * 8, Game1.tileSize, 0, 0).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize * 3, Game1.tileSize, 0, 0).Y, 9999, -1, 9999, 1f, 1f, false, -1, "", 4);
				this.okDeleteButton.draw(b);
				this.cancelDeleteButton.draw(b);
			}
			base.draw(b);
			if (this.hoverText.Count<char>() > 0)
			{
				IClickableMenu.drawHoverText(b, this.hoverText, Game1.dialogueFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
			}
			if (this.selected != -1 && this.timerToLoad < 1000)
			{
				b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * (1f - (float)this.timerToLoad / 1000f));
			}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
		}
    }
}
