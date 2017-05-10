using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValleyMP.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SFarmer = StardewValley.Farmer;

namespace StardewValleyMP.Vanilla
{
    // Whole class copied from LoadGameMenu because annoying private variables
    // Changes are SaveGame.Load -> mode select menu
    public class NewLoadMenu : IClickableMenu
    {
        public static SFarmer pendingSelected;



        public List<ClickableComponent> gamesToLoadButton = new List<ClickableComponent>();

        public List<ClickableTextureComponent> deleteButtons = new List<ClickableTextureComponent>();

        private int currentItemIndex;

        private int timerToLoad;

        private int selected = -1;

        private int selectedForDelete = -1;

        public ClickableTextureComponent upArrow;

        public ClickableTextureComponent downArrow;

        public ClickableTextureComponent scrollBar;

        public ClickableTextureComponent okDeleteButton;

        public ClickableTextureComponent cancelDeleteButton;

        public ClickableComponent backButton;

        public bool scrolling;

        public bool deleteConfirmationScreen;

        private List<SFarmer> saveGames = new List<SFarmer>();

        private Microsoft.Xna.Framework.Rectangle scrollBarRunner;

        private string hoverText = "";

        private bool loading;

        private bool deleting;

        public Task<List<SFarmer>> _initTask;
        public Task<List<SFarmer>> _initTaskMonoDebug;

        private Task _deleteTask;

        private bool disposedValue;

        public override bool readyToClose()
        {
            if (this._initTask != null || this._deleteTask != null || this.loading)
            {
                return false;
            }
            return !this.deleting;
        }

        public NewLoadMenu() : base(Game1.viewport.Width / 2 - (1100 + IClickableMenu.borderWidth * 2) / 2, Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2, 1100 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false)
        {
            this.backButton = new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(Game1.viewport.Width + -198 - 48, Game1.viewport.Height - 81 - 24, 198, 81), "")
            {
                myID = 81114,
                upNeighborID = 801,
                leftNeighborID = 801
            };
            this.upArrow = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(421, 459, 11, 12), (float)Game1.pixelZoom, false)
            {
                myID = 800,
                downNeighborID = 801,
                leftNeighborID = 100
            };
            this.downArrow = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(421, 472, 11, 12), (float)Game1.pixelZoom, false)
            {
                myID = 801,
                upNeighborID = 800,
                leftNeighborID = 103,
                rightNeighborID = 81114,
                downNeighborID = 81114
            };
            this.scrollBar = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(435, 463, 6, 10), (float)Game1.pixelZoom, false);
            this.scrollBarRunner = new Microsoft.Xna.Framework.Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, this.height - Game1.tileSize - this.upArrow.bounds.Height - Game1.pixelZoom * 7);
            this.okDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10992", new object[0]), new Microsoft.Xna.Framework.Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X - Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false)
            {
                myID = 802,
                rightNeighborID = 803
            };
            this.cancelDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10993", new object[0]), new Microsoft.Xna.Framework.Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X + Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1), 1f, false)
            {
                myID = 803,
                leftNeighborID = 802
            };
            for (int i = 0; i < 4; i++)
            {
                this.gamesToLoadButton.Add(new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4 + i * (this.height / 4), this.width - Game1.tileSize / 2, this.height / 4 + Game1.pixelZoom), string.Concat(i))
                {
                    myID = i,
                    downNeighborID = (i < 3 ? i + 1 : -7777),
                    upNeighborID = (i > 0 ? i - 1 : -7777),
                    rightNeighborID = i + 100
                });
                this.deleteButtons.Add(new ClickableTextureComponent("", new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize - Game1.pixelZoom, this.yPositionOnScreen + Game1.tileSize / 2 + Game1.pixelZoom + i * (this.height / 4), 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10994", new object[0]), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(322, 498, 12, 12), (float)Game1.pixelZoom * 3f / 4f, false)
                {
                    myID = i + 100,
                    leftNeighborID = i,
                    downNeighborID = i + 1 + 100,
                    upNeighborID = i - 1 + 100,
                    rightNeighborID = (i < 2 ? 800 : 801)
                });
            }
            if (Util.UsingMono)
            {
                Log.debug("Mono, doing alternate loading test?");
                this._initTaskMonoDebug = new Task<List<SFarmer>>((Func<List<SFarmer>>)(() =>
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    Log.debug("Task started");
                    return FindSaveGames();
                }));
                this._initTaskMonoDebug.Start();
                Log.debug("Started it");
            }
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                base.populateClickableComponentList();
                this.snapToDefaultClickableComponent();
            }
        }

        private static List<SFarmer> FindSaveGames()
        {
            List<SFarmer> farmerList = new List<SFarmer>();
            string str = Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"));
            Log.debug("Checking for save dir");
            if (Directory.Exists(str))
            {
                Log.debug("It existed, good");
                foreach (string directory in Directory.GetDirectories(str))
                {
                    string path = Path.Combine(str, directory, "SaveGameInfo");
                    Log.debug("Found a dir in saves: " + path);
                    SFarmer target = (SFarmer)null;
                    try
                    {
                        using (FileStream fileStream = File.Open(path, FileMode.Open))
                        {
                            Log.debug("Loading farmer from save");
                            target = (SFarmer)SaveGame.farmerSerializer.Deserialize((Stream)fileStream);
                            Log.debug("(Load step 2)");
                            SaveGame.loadDataToFarmer(target);
                            target.favoriteThing = ((IEnumerable<string>)directory.Split(Path.DirectorySeparatorChar)).Last<string>();
                            farmerList.Add(target);
                            fileStream.Close();
                            Log.debug("Loaded farmer from save");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.debug("There was an exception: " + ex);
                        if (target != null)
                            target.unload();
                        Log.debug("Unloaded 'target'");
                    }
                }
            }
            else Log.debug("No save dir?");
            farmerList.Sort();
            Log.debug("Sorted saved farmers");
            return farmerList;
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (b == Buttons.B && this.deleteConfirmationScreen)
            {
                this.deleteConfirmationScreen = false;
                this.selectedForDelete = -1;
                Game1.playSound("smallSelect");
                if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                {
                    this.currentlySnappedComponent = base.getComponentWithID(0);
                    this.snapCursorToCurrentSnappedComponent();
                }
            }
        }

        public override void snapToDefaultClickableComponent()
        {
            this.currentlySnappedComponent = base.getComponentWithID(0);
            this.snapCursorToCurrentSnappedComponent();
        }

        protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
        {
            if (direction == 2 && this.currentItemIndex < Math.Max(0, this.saveGames.Count - 4))
            {
                this.downArrowPressed();
                this.currentlySnappedComponent = base.getComponentWithID(3);
                this.snapCursorToCurrentSnappedComponent();
                return;
            }
            if (direction == 0 && this.currentItemIndex > 0)
            {
                this.upArrowPressed();
                this.currentlySnappedComponent = base.getComponentWithID(0);
                this.snapCursorToCurrentSnappedComponent();
            }
        }

        public override void gameWindowSizeChanged(Microsoft.Xna.Framework.Rectangle oldBounds, Microsoft.Xna.Framework.Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            this.upArrow = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(421, 459, 11, 12), (float)Game1.pixelZoom, false)
            {
                myID = 800,
                downNeighborID = 801
            };
            this.downArrow = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize / 4, this.yPositionOnScreen + this.height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(421, 472, 11, 12), (float)Game1.pixelZoom, false)
            {
                myID = 801,
                upNeighborID = 800
            };
            this.scrollBar = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(this.upArrow.bounds.X + Game1.pixelZoom * 3, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom, 10 * Game1.pixelZoom), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(435, 463, 6, 10), (float)Game1.pixelZoom, false);
            this.scrollBarRunner = new Microsoft.Xna.Framework.Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + Game1.pixelZoom, this.scrollBar.bounds.Width, this.height - Game1.tileSize - this.upArrow.bounds.Height - Game1.pixelZoom * 7);
            this.okDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10992", new object[0]), new Microsoft.Xna.Framework.Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X - Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false)
            {
                myID = 802,
                rightNeighborID = 803
            };
            this.cancelDeleteButton = new ClickableTextureComponent(Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10993", new object[0]), new Microsoft.Xna.Framework.Rectangle((int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).X + Game1.tileSize, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize, Game1.tileSize, 0, 0).Y + Game1.tileSize * 2, Game1.tileSize, Game1.tileSize), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1), 1f, false)
            {
                myID = 803,
                leftNeighborID = 802
            };
            this.gamesToLoadButton.Clear();
            this.deleteButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                this.gamesToLoadButton.Add(new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4 + i * (this.height / 4), this.width - Game1.tileSize / 2, this.height / 4 + Game1.pixelZoom), string.Concat(i))
                {
                    myID = i,
                    downNeighborID = (i < 3 ? i + 1 : -7777),
                    upNeighborID = (i > 0 ? i - 1 : -7777),
                    rightNeighborID = i + 100
                });
                this.deleteButtons.Add(new ClickableTextureComponent("", new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize - Game1.pixelZoom, this.yPositionOnScreen + Game1.tileSize / 2 + Game1.pixelZoom + i * (this.height / 4), 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10994", new object[0]), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(322, 498, 12, 12), (float)Game1.pixelZoom * 3f / 4f, false)
                {
                    myID = i + 100,
                    leftNeighborID = i,
                    downNeighborID = i + 1 + 100,
                    upNeighborID = i - 1 + 100,
                    rightNeighborID = (i < 2 ? 800 : 801)
                });
            }
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverText = "";
            base.performHoverAction(x, y);
            if (this.deleteConfirmationScreen)
            {
                this.okDeleteButton.tryHover(x, y, 0.1f);
                this.cancelDeleteButton.tryHover(x, y, 0.1f);
                if (this.okDeleteButton.containsPoint(x, y))
                {
                    this.hoverText = "";
                    return;
                }
                if (this.cancelDeleteButton.containsPoint(x, y))
                {
                    this.hoverText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10993", new object[0]);
                }
                return;
            }
            this.upArrow.tryHover(x, y, 0.1f);
            this.downArrow.tryHover(x, y, 0.1f);
            this.scrollBar.tryHover(x, y, 0.1f);
            foreach (ClickableTextureComponent deleteButton in this.deleteButtons)
            {
                deleteButton.tryHover(x, y, 0.2f);
                if (!deleteButton.containsPoint(x, y))
                {
                    continue;
                }
                this.hoverText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.10994", new object[0]);
                return;
            }
            if (this.scrolling)
            {
                return;
            }
            for (int i = 0; i < this.gamesToLoadButton.Count; i++)
            {
                if (this.currentItemIndex + i < this.saveGames.Count && this.gamesToLoadButton[i].containsPoint(x, y))
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
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            if (this.scrolling)
            {
                int num = this.scrollBar.bounds.Y;
                this.scrollBar.bounds.Y = Math.Min(this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this.scrollBar.bounds.Height, Math.Max(y, this.yPositionOnScreen + this.upArrow.bounds.Height + Game1.pixelZoom * 5));
                float single = (float)(y - this.scrollBarRunner.Y) / (float)this.scrollBarRunner.Height;
                this.currentItemIndex = Math.Min(this.saveGames.Count - 4, Math.Max(0, (int)((float)this.saveGames.Count * single)));
                this.setScrollBarToCurrentIndex();
                if (num != this.scrollBar.bounds.Y)
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
            if (this.saveGames.Count > 0)
            {
                this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.saveGames.Count - 4 + 1) * this.currentItemIndex + this.upArrow.bounds.Bottom + Game1.pixelZoom;
                if (this.currentItemIndex == this.saveGames.Count - 4)
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
            if (direction < 0 && this.currentItemIndex < Math.Max(0, this.saveGames.Count - 4))
            {
                this.downArrowPressed();
            }
        }

        private void downArrowPressed()
        {
            this.downArrow.scale = this.downArrow.baseScale;
            this.currentItemIndex = this.currentItemIndex + 1;
            Game1.playSound("shwip");
            this.setScrollBarToCurrentIndex();
        }

        private void upArrowPressed()
        {
            this.upArrow.scale = this.upArrow.baseScale;
            this.currentItemIndex = this.currentItemIndex - 1;
            Game1.playSound("shwip");
            this.setScrollBarToCurrentIndex();
        }

        private void deleteFile(int which)
        {
            string item = this.saveGames[which].favoriteThing;
            string str = Path.Combine(new string[] { Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), item) });
            Thread.Sleep(Game1.random.Next(1000, 5000));
            if (Directory.Exists(str))
            {
                Directory.Delete(str, true);
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.timerToLoad > 0 || this.loading || this.deleting)
            {
                return;
            }
            if (this.deleteConfirmationScreen)
            {
                if (this.cancelDeleteButton.containsPoint(x, y))
                {
                    this.deleteConfirmationScreen = false;
                    this.selectedForDelete = -1;
                    Game1.playSound("smallSelect");
                    if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                    {
                        this.currentlySnappedComponent = base.getComponentWithID(0);
                        this.snapCursorToCurrentSnappedComponent();
                        return;
                    }
                }
                else if (this.okDeleteButton.containsPoint(x, y))
                {
                    this.deleting = true;
                    this._deleteTask = new Task(() => {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                        this.deleteFile(this.selectedForDelete);
                    });
                    this._deleteTask.Start();
                    this.deleteConfirmationScreen = false;
                    if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                    {
                        this.currentlySnappedComponent = base.getComponentWithID(0);
                        this.snapCursorToCurrentSnappedComponent();
                    }
                    Game1.playSound("trashcan");
                }
                return;
            }
            base.receiveLeftClick(x, y, playSound);
            if (this.downArrow.containsPoint(x, y) && this.currentItemIndex < Math.Max(0, this.saveGames.Count - 4))
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
                for (int i = 0; i < this.deleteButtons.Count; i++)
                {
                    if (this.deleteButtons[i].containsPoint(x, y) && i < this.saveGames.Count && !this.deleteConfirmationScreen)
                    {
                        this.deleteConfirmationScreen = true;
                        Game1.playSound("drumkit6");
                        this.selectedForDelete = this.currentItemIndex + i;
                        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
                        {
                            this.currentlySnappedComponent = base.getComponentWithID(803);
                            this.snapCursorToCurrentSnappedComponent();
                        }
                        return;
                    }
                }
            }
            if (!this.deleteConfirmationScreen)
            {
                for (int j = 0; j < this.gamesToLoadButton.Count; j++)
                {
                    if (this.gamesToLoadButton[j].containsPoint(x, y) && j < this.saveGames.Count)
                    {
                        this.timerToLoad = 2150;
                        this.loading = true;
                        Game1.playSound("select");
                        this.selected = this.currentItemIndex + j;
                        return;
                    }
                }
            }
            this.currentItemIndex = Math.Max(0, Math.Min(this.saveGames.Count - 4, this.currentItemIndex));
        }

        public override void update(GameTime time)
        {
            base.update(time);
            //if ( Util.UsingMono )
            {
                if (this._initTaskMonoDebug != null)
                {
                    if (this._initTaskMonoDebug.IsCanceled || this._initTaskMonoDebug.IsCompleted || this._initTaskMonoDebug.IsFaulted)
                    {
                        Log.trace("Mono-test debug save listing task status: " + _initTaskMonoDebug.IsCanceled + " " + _initTaskMonoDebug.IsCompleted + " " + _initTaskMonoDebug.IsFaulted);
                        if (this._initTaskMonoDebug.IsCompleted)
                        {
                            var r = _initTaskMonoDebug.Result;
                            Log.trace("The list: " + r.Count);
                            foreach ( var f in r )
                            {
                                Log.trace("\t" + f + " " + f.name + " " + f.favoriteThing);
                            }
                        }

                        if ( _initTask != null )
                        {
                            _initTask.Wait();
                            _initTask = null;
                        }
                        if ( saveGames.Count == 0 )
                        {
                            this.saveGames.AddRange(this._initTaskMonoDebug.Result);
                        }
                        this._initTaskMonoDebug = null;
                    }
                }
            }
            if (this._initTask != null)
            {
                if (this._initTask.IsCanceled || this._initTask.IsCompleted || this._initTask.IsFaulted)
                {
                    Log.trace("Save listing task status: " + _initTask.IsCanceled + " " + _initTask.IsCompleted + " " + _initTask.IsFaulted);
                    if (this._initTask.IsCompleted)
                    {
                        foreach (SFarmer saveGame in this.saveGames)
                        {
                            saveGame.unload();
                        }
                        this.saveGames.Clear();
                        this.saveGames.AddRange(this._initTask.Result);
                    }
                    this._initTask = null;
                }
                return;
            }
            if (this._deleteTask != null)
            {
                if (this._deleteTask.IsCanceled || this._deleteTask.IsCompleted || this._deleteTask.IsFaulted)
                {
                    if (!this._deleteTask.IsCompleted)
                    {
                        this.selectedForDelete = -1;
                    }
                    this._deleteTask = null;
                    this.deleting = false;
                }
                return;
            }
            if (this.selectedForDelete != -1 && !this.deleteConfirmationScreen && !this.deleting)
            {
                this.saveGames[this.selectedForDelete].unload();
                this.saveGames.RemoveAt(this.selectedForDelete);
                this.selectedForDelete = -1;
                this.gamesToLoadButton.Clear();
                this.deleteButtons.Clear();
                for (int i = 0; i < 4; i++)
                {
                    this.gamesToLoadButton.Add(new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, this.yPositionOnScreen + Game1.tileSize / 4 + i * (this.height / 4), this.width - Game1.tileSize / 2, this.height / 4 + Game1.pixelZoom), string.Concat(i)));
                    this.deleteButtons.Add(new ClickableTextureComponent("", new Microsoft.Xna.Framework.Rectangle(this.xPositionOnScreen + this.width - Game1.tileSize - Game1.pixelZoom, this.yPositionOnScreen + Game1.tileSize / 2 + Game1.pixelZoom + i * (this.height / 4), 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), "", "Delete File", Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(322, 498, 12, 12), (float)Game1.pixelZoom * 3f / 4f, false));
                }
            }
            if (this.timerToLoad > 0)
            {
                this.timerToLoad = this.timerToLoad - time.ElapsedGameTime.Milliseconds;
                if (this.timerToLoad <= 0)
                {
                    pendingSelected = saveGames[selected];
                    TitleMenu.subMenu = new ModeSelectMenu(this.saveGames[this.selected].favoriteThing);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(384, 373, 18, 18), this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height + Game1.tileSize / 2, Color.White, (float)Game1.pixelZoom, true);
            if (this.selectedForDelete == -1 || !this.deleting || this.deleteConfirmationScreen)
            {
                for (int i = 0; i < this.gamesToLoadButton.Count; i++)
                {
                    if (this.currentItemIndex + i < this.saveGames.Count)
                    {
                        SFarmer farmer = this.saveGames[this.currentItemIndex + i];
                        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(384, 396, 15, 15), this.gamesToLoadButton[i].bounds.X, this.gamesToLoadButton[i].bounds.Y, this.gamesToLoadButton[i].bounds.Width, this.gamesToLoadButton[i].bounds.Height, ((this.currentItemIndex + i == this.selected && this.timerToLoad % 150 > 75 && this.timerToLoad > 1000) || (this.selected == -1 && this.gamesToLoadButton[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !this.scrolling && !this.deleteConfirmationScreen)) ? (this.deleteButtons[i].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.White : Color.Wheat) : Color.White, (float)Game1.pixelZoom, false);
                        SpriteText.drawString(b, this.currentItemIndex + i + 1 + ".", this.gamesToLoadButton[i].bounds.X + Game1.pixelZoom * 7 + Game1.tileSize / 2 - SpriteText.getWidthOfString(this.currentItemIndex + i + 1 + ".") / 2, this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 9, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                        SpriteText.drawString(b, farmer.Name, this.gamesToLoadButton[i].bounds.X + Game1.tileSize * 2 + Game1.pixelZoom * 9, this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 9, 999999, -1, 999999, 1f, 0.88f, false, -1, "", -1);
                        b.Draw(Game1.shadowTexture, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize + Game1.tileSize - Game1.pixelZoom), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize * 2 + Game1.pixelZoom * 4)), new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, 0.8f);
                        farmer.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(0, 0, false, false, null, false), 0, new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 32), new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize / 4 + Game1.tileSize + Game1.pixelZoom * 3), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 5)), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, farmer);
                        string text;
                        if (farmer.dayOfMonthForSaveGame.HasValue && farmer.seasonForSaveGame.HasValue && farmer.yearForSaveGame.HasValue)
                        {
                            text = Utility.getDateStringFor(farmer.dayOfMonthForSaveGame.Value, farmer.seasonForSaveGame.Value, farmer.yearForSaveGame.Value);
                        }
                        else
                        {
                            text = farmer.dateStringForSaveGame;
                        }
                        Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + Game1.tileSize * 2 + Game1.pixelZoom * 8), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 10)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11019", new object[]
                        {
                            farmer.farmName
                        }), Game1.dialogueFont, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 2) - Game1.dialogueFont.MeasureString(farmer.farmName + " Farm").X, (float)(this.gamesToLoadButton[i].bounds.Y + Game1.pixelZoom * 11)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        string text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", new object[]
                        {
                            Utility.getNumberWithCommas(farmer.Money)
                        });
                        if (farmer.Money == 1 && LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt)
                        {
                            text2 = text2.Substring(0, text2.Length - 1);
                        }
                        int num = (int)Game1.dialogueFont.MeasureString(text2).X;
                        Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 25 - num), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11)), new Microsoft.Xna.Framework.Rectangle(193, 373, 9, 9), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
                        Vector2 position = new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 15 - num), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11));
                        if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en)
                        {
                            position.Y += 5f;
                        }
                        Utility.drawTextWithShadow(b, text2, Game1.dialogueFont, position, Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        position = new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom * 11), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 9));
                        Utility.drawWithShadow(b, Game1.mouseCursors, position, new Microsoft.Xna.Framework.Rectangle(595, 1748, 9, 11), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 1f, -1, -1, 0.35f);
                        position = new Vector2((float)(this.gamesToLoadButton[i].bounds.X + this.width - Game1.tileSize * 3 - Game1.pixelZoom), (float)(this.gamesToLoadButton[i].bounds.Y + Game1.tileSize + Game1.pixelZoom * 11));
                        if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en)
                        {
                            position.Y += 5f;
                        }
                        Utility.drawTextWithShadow(b, Utility.getHoursMinutesStringFromMilliseconds(farmer.millisecondsPlayed), Game1.dialogueFont, position, Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        if (this.deleteButtons.Count > i)
                        {
                            this.deleteButtons[i].draw(b, Color.White * 0.75f, 1f);
                        }
                    }
                }
            }
            string text3 = null;
            if (this.saveGames.Count == 0)
            {
                text3 = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11022", new object[0]);
            }
            if (this._initTask != null)
            {
                text3 = Game1.content.LoadString("Strings\\UI:LoadGameMenu_LookingForSavedGames", new object[0]);
            }
            if (this.deleting)
            {
                text3 = Game1.content.LoadString("Strings\\UI:LoadGameMenu_Deleting", new object[0]);
            }
            if (text3 != null)
            {
                SpriteText.drawStringHorizontallyCenteredAt(b, text3, Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.X, Game1.graphics.GraphicsDevice.Viewport.Bounds.Center.Y, 999999, -1, 999999, 1f, 0.88f, false, -1);
            }
            this.upArrow.draw(b);
            this.downArrow.draw(b);
            if (this.saveGames.Count > 4)
            {
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(403, 383, 6, 6), this.scrollBarRunner.X, this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White, (float)Game1.pixelZoom, false);
                this.scrollBar.draw(b);
            }
            if (this.deleteConfirmationScreen)
            {
                b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * 0.75f);
                string s = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11023", new object[]
                {
                    this.saveGames[this.selectedForDelete].name
                });
                int num2 = this.okDeleteButton.bounds.X + (this.cancelDeleteButton.bounds.X - this.okDeleteButton.bounds.X) / 2 + this.okDeleteButton.bounds.Width / 2;
                SpriteText.drawString(b, s, num2 - SpriteText.getWidthOfString(s) / 2, (int)Utility.getTopLeftPositionForCenteringOnScreen(Game1.tileSize * 3, Game1.tileSize, 0, 0).Y, 9999, -1, 9999, 1f, 1f, false, -1, "", 4);
                this.okDeleteButton.draw(b);
                this.cancelDeleteButton.draw(b);
            }
            base.draw(b);
            if (this.hoverText.Length > 0)
            {
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.dialogueFont, 0, 0, -1, null, -1, null, null, 0, -1, -1, -1, -1, 1f, null);
            }
            if (this.selected != -1 && this.timerToLoad < 1000)
            {
                b.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * (1f - (float)this.timerToLoad / 1000f));
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.saveGames != null)
                    {
                        foreach (SFarmer saveGame in this.saveGames)
                        {
                            saveGame.unload();
                        }
                        this.saveGames.Clear();
                        this.saveGames = null;
                    }
                    if (this._initTask != null)
                    {
                        this._initTask = null;
                    }
                    if (this._deleteTask != null)
                    {
                        this._deleteTask = null;
                    }
                }
                this.disposedValue = true;
            }
        }

        ~NewLoadMenu()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
