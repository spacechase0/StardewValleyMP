using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValleyMP.Connections;
using StardewValleyMP.Interface;
using StardewValleyMP.Packets;
using StardewValleyMP.Vanilla;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static StardewValleyMP.Interface.LanSelectorWidget;
using SFarmer = StardewValley.Farmer;

/*
farmer pos
	anim
	movement
	warp
object change
switch held
tool action
game clock
building change
debris pickup
player intro
debris create
npc behavior
npc move
check action
 * */

namespace StardewValleyMP
{
    public enum Mode
    {
        Singleplayer,
        Host,
        Client
    };

    class Multiplayer
    {
        public const string DEFAULT_PORT = "24644";
        public const byte PROTOCOL_VERSION = 3;
        public static bool COOP { get { return MultiplayerMod.ModConfig.Coop; } }

        public static Mode mode = Mode.Singleplayer;

        public static Server server;
        public static Client client;
        public static Action<Packet> sendFunc;

        public static Dictionary<string, LocationCache> locations = new Dictionary< string, LocationCache >();
        public static string[] checkMail = new string[] { "ccCraftsRoom", "ccBoilerRoom", "ccVault", "ccFishTank", "ccBulletin", "ccPantry", "JojaMember"  };

        // Network benchmarking
#if NETWORKING_BENCHMARK
        public static long serverToClientBytesTransferred;
        public static long clientToServerBytesTransferred;
#endif
        public static byte getMyId()
        {
            if ( mode == Mode.Client )
            {
                return client.id;
            }

            return 0;
        }

        public static int getFarmerCount()
        {
            if (mode == Mode.Host && server != null)
                return server.clients.Count + 1;
            else if (mode == Mode.Client && client != null)
                return client.others.Count + 1;
            else return 1;
        }

        public static byte getFarmerId( SFarmer target )
        {
            if ( mode == Mode.Host && server != null )
            {
                if (target == Game1.player) return 0;

                foreach ( Server.Client other in server.clients )
                {
                    if (other.farmer == target)
                    {
                        return other.id;
                    }
                }
            }
            else if ( mode == Mode.Client && client != null )
            {
                if (target == Game1.player) return client.id;

                foreach ( KeyValuePair< byte, SFarmer > other in client.others )
                {
                    if ( other.Value == target )
                    {
                        return other.Key;
                    }
                }
            }

            throw new ArgumentException("Invalid farmer?");
        }

        public static SFarmer getFarmer(byte target)
        {
            if (mode == Mode.Host && server != null)
            {
                if (target == 0) return Game1.player;

                foreach (Server.Client other in server.clients)
                {
                    if (other.id == target)
                    {
                        return other.farmer;
                    }
                }
            }
            else if (mode == Mode.Client && client != null)
            {
                if (target == client.id) return Game1.player;

                foreach (KeyValuePair<byte, SFarmer> other in client.others)
                {
                    if (other.Key == target)
                    {
                        return other.Value;
                    }
                }
            }

            return null;
        }

        public static SFarmer getFarmer(string target)
        {
            // Weird bug where Game1.player is always a different save of mine for some reason
            // SaveGame.loaded.player is correct, so just give that priority
            if (SaveGame.loaded != null && SaveGame.loaded.player != null && target == SaveGame.loaded.player.name)
                return SaveGame.loaded.player;
            if (Game1.player != null && target == Game1.player.name)
                return Game1.player;

            if (mode == Mode.Host && server != null)
            {
                foreach (Server.Client other in server.clients)
                {
                    if (other.farmer.name == target)
                    {
                        return other.farmer;
                    }
                }
            }
            else if (mode == Mode.Client && client != null)
            {
                foreach (KeyValuePair<byte, SFarmer> other in client.others)
                {
                    if (other.Value.name == target)
                    {
                        return other.Value;
                    }
                }
            }

            Log.warn("WARNING: Failed to find player " + target );
            //Log.Async( Game1.player.name + " " + Game1.player.spouse + " " + Game1.player.dateStringForSaveGame);
            //Log.Async("Or is it " + SaveGame.loaded.player.name + "? " + SaveGame.loaded.player.spouse + " " + SaveGame.loaded.player.dateStringForSaveGame);

            return null;
        }
        public static bool isPlayerUnique( string location, bool despiteCoopSetting = false )
        {
            if (location == "FarmHouse") return true;
            if (location == "Cellar") return true;
            if (!Multiplayer.COOP || despiteCoopSetting)
            {
                if (location == "Farm") return true;
                if (location == "FarmCave") return true;
                if (location == "Greenhouse") return true;
                if (location == "ArchaelogyHouse") return true;
                if (location == "CommunityCenter") return true;
            }

            return false;
        }
        
        public static bool waitingOnOthers()
        {
            if ( mode == Mode.Host && server != null )
            {
                if (server.clients == null)
                    return false;

                bool othersReady = true;
                foreach ( Server.Client client in server.clients )
                {
                    othersReady = othersReady && client.stage == Server.Client.NetStage.WaitingForStart;
                }

                return !othersReady;
            }
            else if ( mode == Mode.Client && client != null)
            {
                return client.stage == Client.NetStage.Waiting;
            }

            return false;
        }

        // Changes generic player locations into their specific versions
        public static string processLocationNameForPlayerUnique( SFarmer from, string loc )
        {
            if (loc == null) return loc;

            SFarmer me = NewLoadMenu.pendingSelected;
            if (SaveGame.loaded != null && SaveGame.loaded.player != null)
                me = SaveGame.loaded.player;
            if (me == null && Game1.player != null)
                me = Game1.player;
            if ( me == null )
            {
                Log.warn("processLocationNameForPlayerUnique called without us having a player");
            }

            if ( loc == "BathHouse_Entry" || loc == "BathHouse_MensLocker" ||
                 loc == "BathHouse_WomensLocker" || loc == "BathHouse_Pool" )
            {
                // This function would act really weird if a player had a name like this.
                // And by really weird, I mean it would break things.
                return loc;
            }

            if (mode == Mode.Client) from = client.others[0];
            // Huh. This is the same for client and server.
            // Assuming it is used correctly.

            if (loc.EndsWith("_" + me.name)) // They say it is our version
            {
                return loc.Substring(0, loc.Length - 1 - me.name.Length);
            }
            else if ( isPlayerUnique( loc ) ) // Exact match of original, ie. theirs
            {
                return loc + "_" + from.name;
            }
            
            return loc;
        }

        // Farm buildings have a unique name which uses their coordinates.
        // For some reason GameLocation.name is the non-unique version. Ugh
        public static string getUniqueLocationName( GameLocation loc )
        {
            if (loc.name == "Temp") return loc.name;
            if ( Game1.getLocationFromName( loc.name ) != null ) return loc.name;

            foreach ( GameLocation checkLoc in Game1.locations )
            {
                if ( checkLoc is Farm )
                {
                    Farm farm = checkLoc as Farm;
                    foreach ( Building building in farm.buildings )
                    {
                        if ( loc == building.indoors )
                        {
                            return building.nameOfIndoors;
                        }
                    }
                }
            }

            // Hopefully won't happen?
            Log.warn("Bad location or something");
            return null;
        }

        // loc oldName
        public static void fixLocations( List< GameLocation > locations, SFarmer from, Action<GameLocation, string, object> onceFixed = null, object extra = null )
        {
            if (mode == Mode.Client) from = client.others[0];

            foreach (GameLocation loc in locations)
            {
                string oldName = loc.name;
                loc.name = Multiplayer.processLocationNameForPlayerUnique(from, loc.name);
                foreach (Warp warp in loc.warps)
                {
                    Util.SetInstanceField(typeof(Warp), warp, "targetName", Multiplayer.processLocationNameForPlayerUnique(from, warp.TargetName));
                }
                foreach (NPC npc in loc.characters)
                {
                    if (npc.defaultMap == null) continue;
                    npc.defaultMap = Multiplayer.processLocationNameForPlayerUnique(from, npc.defaultMap);
                }
                if ( loc is Farm )
                {
                    Farm farm = loc as Farm;
                    foreach ( Building building in farm.buildings )
                    {
                        // TODO
                    }
                }

                if ( isPlayerUnique( oldName ) && onceFixed != null ) onceFixed( loc, oldName, extra );
            }
        }

        public static void addOtherLocations()
        {
            // Remove stuff for any farmers that aren't present - stuff like FarmHouse will crash otherwise
            List<GameLocation> toRemove = new List<GameLocation>();
            foreach ( GameLocation loc in SaveGame.loaded.locations )
            {
                if ( loc.name.Contains( '_' ) && isPlayerUnique( loc.name.Substring( 0, loc.name.IndexOf( '_' ) ) ) )
                {
                    string other = loc.name.Substring(loc.name.IndexOf('_') + 1);
                    if ( getFarmer( other ) == null )
                    {
                        Log.debug("Farmer " + other + " is not online, removing his " + loc.name);
                        toRemove.Add(loc);
                    }
                }
            }
            foreach ( GameLocation loc in toRemove )
            {
                SaveGame.loaded.locations.Remove(loc);
            }

            foreach ( GameLocation loc in SaveGame.loaded.locations )
            {
                if ( loc.name.Contains( "_" ) && Game1.getLocationFromName( loc.name ) == null )
                {
                    Log.debug(loc.name + " missing from game, copying from save");
                    Game1.locations.Add(loc);
                }
            }
        }

        public static LanEntry lanOverride;
        public static  String ipStr = "127.0.0.1";
        public static  String portStr = DEFAULT_PORT;
        public static  TcpListener listener = null;
        
        private static TcpListener makeRealTcpListener( int port )
        {
            return TcpListener.Create(port);
        }

        public static bool lobby = true;
        public static bool problemStarting = false;
        public static void startHost()
        {
            mode = Mode.Host;
            problemStarting = false;

            try
            {
                int port = Int32.Parse(portStr);
                // http://stackoverflow.com/questions/1777629/how-to-listen-on-multiple-ip-addresses
                listener = null;
                if (Util.UsingMono)
                    listener = new TcpListener(IPAddress.Any, port);
                else
                    listener = makeRealTcpListener( port );
                listener.Start();

                client = null;
                server = new Server();

                while ( Multiplayer.mode == Mode.Host)
                {
                    Log.info("Waiting for connection...");
                    TcpClient socket = listener.AcceptTcpClient();
                    socket.NoDelay = true;
                    server.addClient(new NetworkConnection(socket));
                }

            }
            catch (Exception e)
            {
                if (e is SocketException && ( ( ( SocketException ) e ).Message.IndexOf( "A blocking operation was interrupted" ) != -1 ||
                                              ( ( SocketException ) e ).Message.IndexOf( "WSACancelBlockingCall" ) != -1  ||
                                              ( ( SocketException ) e ).Message.IndexOf( "interrupted" ) != -1) )
                    return;

                Log.error("Exception while listening: " + e);
                ChatMenu.chat.Add(new ChatEntry(null, "Exception while listening for clients: "));
                ChatMenu.chat.Add(new ChatEntry(null, e.Message));
                ChatMenu.chat.Add(new ChatEntry(null, "Check your log file for more details."));
                problemStarting = true;
            }
            finally
            {
                if ( listener != null )
                {
                    listener.Stop();
                    listener = null;
                }
            }
        }

        public static void startClient()
        {
            mode = Mode.Client;
            problemStarting = false;

            try
            {
                Log.info("Connecting to " + ipStr + ":" + portStr);
                IPAddress ip;
                IPAddress.TryParse(ipStr, out ip );
                int port = Int32.Parse(portStr);
                
                TcpClient socket = new TcpClient(AddressFamily.InterNetworkV6);
                try
                {
                    socket.Client.DualMode = true;
                }
                catch ( Exception e )
                {
                    Log.warn("Exception setting socket to dual-mode. Looks like Mono messed up again. " + e);
                }
                if (lanOverride == null)
                    socket.Connect(ip, port);
                else
                {
                    lanOverride.server.Port = lanOverride.port;
                    socket.Connect(lanOverride.server);
                }
                socket.NoDelay = true;
                ChatMenu.chat.Add(new ChatEntry(null, "Connection established."));

                client = new Client(new NetworkConnection( socket ) );
                server = null;
            }
            catch ( Exception e )
            {
                Log.error("Exception while connecting: " + e);
                ChatMenu.chat.Add(new ChatEntry(null, "Exception while connecting to server: "));
                ChatMenu.chat.Add(new ChatEntry(null, e.Message));
                ChatMenu.chat.Add(new ChatEntry(null, "Check your log file for more details."));
                problemStarting = true;
            }
        }
        
        public static bool prevFreezeControls = false;
        public static bool sentNextDayPacket = false;
        public static long prevLatestId;

        public static void onBeforeSave()
        {
            Log.trace("Before save");
            if (Game1.activeClickableMenu is SaveGameMenu && Game1.activeClickableMenu.GetType() != typeof(NewSaveGameMenu))
            {
                Log.debug("Replacing save game menu");
                Game1.activeClickableMenu = new NewSaveGameMenu();
            }
            else if (Game1.activeClickableMenu is ShippingMenu && Game1.activeClickableMenu.GetType() != typeof(NewShippingMenu))
            {
                Log.debug("Replacing shipping menu");

                // The fifth element of categoryItems is a total of everything else.
                List<Item> allItems = ((List<List<Item>>)Util.GetInstanceField(typeof(ShippingMenu), Game1.activeClickableMenu, "categoryItems"))[ 5 ];
                Log.trace("Shipped items: " + allItems.Count);
                foreach (var item in allItems)
                    ;// Log.trace("\t" + item + " " + ( item == null ? "" : ( item.Stack + " " + item.Name + " " + item.parentSheetIndex )));

                // A bunch of important things get changed in the ShippingMenu constructor.
                // We kinda don't want to sell things twice though.
                var oldMoney = Game1.player.money;
                var oldShippedItems = Game1.stats.itemsShipped;
                var oldShippedCrops = Game1.stats.CropsShipped;
                var oldShipped = new SerializableDictionary<int, int>();
                foreach (var entry in Game1.player.basicShipped)
                    oldShipped.Add(entry.Key, entry.Value);
                Game1.activeClickableMenu = new NewShippingMenu( allItems );
                Game1.player.money = oldMoney;
                Game1.stats.itemsShipped = oldShippedItems;
                Game1.stats.CropsShipped = oldShippedCrops;
                Game1.player.basicShipped = oldShipped;
            }
        }
        
        public static void update()
        {
            // Really don't understand why it breaks without this
            // But as soon as you get to the second day, it does. Ugh.
            Game1.player.FarmerSprite.setOwner(Game1.player);

            if (Multiplayer.mode == Mode.Singleplayer) return;

            if ( MultiplayerUtility.latestID > prevLatestId )
            {
                sendFunc(new LatestIdPacket());
            }
            prevLatestId = MultiplayerUtility.latestID;

            //Log.Async("pos:" + Game1.player.position.X + " " + Game1.player.position.Y);
            // Clients sometimes get stuck in the top-right corner and can't move on second day+
            if ( Game1.player.currentLocation != null && Game1.player.currentLocation.name == "FarmHouse" &&
                 Game1.player.currentLocation == Game1.currentLocation && Game1.player.currentLocation != Game1.getLocationFromName( Game1.player.currentLocation.name ) )
            {
                Game1.player.currentLocation = Game1.getLocationFromName( Game1.player.currentLocation.name );
                Game1.currentLocation = Game1.player.currentLocation;
                Game1.currentLocation.resetForPlayerEntry();
            }

            if (Game1.newDay)
            {
                Game1.freezeControls = prevFreezeControls = true;
                Game1.player.CanMove = false;
                if ( !sentNextDayPacket )
                {
                    ChatMenu.chat.Add(new ChatEntry(null, Game1.player.name + " is in bed."));
                    if ( mode == Mode.Host )
                    {
                        server.broadcast(new ChatPacket(255, Game1.player.name + " is in bed."));
                    }
                    else if ( mode == Mode.Client )
                    {
                        client.stage = Client.NetStage.Waiting;

                        try
                        {
                            SaveGame oldLoaded = SaveGame.loaded;
                            var it = NewSaveGame.Save(true);
                            while (it.Current < 100)
                            {
                                it.MoveNext();
                                Thread.Sleep(5);
                            }

                            foreach ( GameLocation loc in SaveGame.loaded.locations )
                            {
                                List<NPC> toRemove = new List<NPC>();
                                foreach ( NPC npc in loc.characters )
                                {
                                    if ( npc is StardewValley.Monsters.RockGolem || npc is StardewValley.Monsters.Bat )
                                    {
                                        toRemove.Add(npc);
                                    }
                                }
                                foreach ( NPC npc in toRemove )
                                {
                                    loc.characters.Remove(npc);
                                }
                            }

                            MemoryStream tmp = new MemoryStream();
                            SaveGame.serializer.Serialize(tmp, SaveGame.loaded);
                            sendFunc(new NextDayPacket());
                            sendFunc(new ClientFarmerDataPacket(Encoding.UTF8.GetString(tmp.ToArray())));
                            //SaveGame.loaded = oldLoaded;
                        }
                        catch ( Exception e )
                        {
                            Log.error("Exception transitioning to next day: " + e);
                            ChatMenu.chat.Add(new ChatEntry(null, "Something went wrong transitioning days."));
                            ChatMenu.chat.Add(new ChatEntry(null, "Report this bug, providing the full log file."));
                            ChatMenu.chat.Add(new ChatEntry(null, "You might be stuck in bed now. Attempting to unstuck you, more stuff might go wrong though."));
                            Game1.freezeControls = prevFreezeControls = false;
                            Game1.newDay = false;
                            Game1.fadeToBlackAlpha = 0;
                            Game1.player.CanMove = true;
                            client.stage = Client.NetStage.Playing;
                        }
                    }
                    sentNextDayPacket = true;
                }

                if ( waitingOnOthers() && Game1.fadeToBlackAlpha > 0.625f )
                {
                    Game1.fadeToBlackAlpha = 0.625f;
                }
            }
            else sentNextDayPacket = false;

            // We want people to wait for everyone
            //Log.Async("menu:"+Game1.activeClickableMenu);

            if (Game1.currentLocation != null && Game1.currentLocation.currentEvent != null)
                Events.fix();
            else
                Events.reset();
            
            // Causing issues after going a day? Maybe?
            // Plus it only fixes a few of the time pauses
            /*Game1.player.forceTimePass = true;
            Game1.paused = false;
            if ( prevFreezeControls != Game1.freezeControls )
            {
                sendFunc( new PauseTimePacket() );
            }
            prevFreezeControls = Game1.freezeControls;*/

            if (Multiplayer.mode == Mode.Host && server != null )
            {
                server.update();
                if (server == null) return;

                if (server.clients == null) return;
                foreach (Server.Client client_ in server.clients)
                {
                    if (client_.stage != Server.Client.NetStage.Playing) continue;
                    if (client_.farmer == null) continue;
                    doUpdatePlayer(client_.farmer);
                }
            }
            else if (Multiplayer.mode == Mode.Client && client != null)
            {
                client.update();
                if (client == null) return;

                if (client.others == null) return;
                foreach (KeyValuePair<byte, SFarmer> other in client.others)
                {
                    if (other.Value == null) continue;
                    doUpdatePlayer(other.Value);
                }
            }

            if (Game1.gameMode == 6) return; // Loading?
            // ^ TODO: Check if != 3 works

            if ( Multiplayer.mode == Mode.Host && server != null && server.playing ||
                 Multiplayer.mode == Mode.Client && client !=  null && client.stage == Client.NetStage.Playing )
            {
                if ( Game1.newDay )
                {
                    return;
                }
                NPCMonitor.startChecks();
                foreach (GameLocation loc in Game1.locations)
                {
                    if (!locations.ContainsKey(loc.name))
                        locations.Add(loc.name, new LocationCache(loc));

                    locations[loc.name].miniUpdate();
                    if (Game1.player.currentLocation == loc)
                    {
                        locations[loc.name].update();
                    }

                    if ( loc is Farm )
                    {
                        BuildableGameLocation farm = loc as BuildableGameLocation;
                        foreach ( Building building in farm.buildings )
                        {
                            if ( building.indoors == null ) continue;

                            if ( !locations.ContainsKey( building.nameOfIndoors ) )
                                locations.Add(building.nameOfIndoors, new LocationCache(building.indoors));

                            locations[loc.name].miniUpdate();
                            if (Game1.currentLocation != loc)
                                locations[building.nameOfIndoors].update();

                            NPCMonitor.check(building.indoors);
                        }
                    }

                    if (loc.name == "FarmHouse")
                    {
                        //Log.Async("Terrain features count for " + loc.name + " " + loc + ": " + loc.terrainFeatures.Count);
                        //Log.Async("Object count for " + loc.name + " " + loc + ": " + loc.objects.Count);
                    }
                }
                NPCMonitor.endChecks();
            }
            
            Game1.player.FarmerSprite.setOwner(Game1.player);
        }

        private static void doUpdatePlayer(SFarmer farmer)
        {
            // Caused problems during weddings
            // (A week or so later) Might have been caused by something else - check if this is needed at some point
            if ( Game1.eventUp && ( farmer.currentLocation != Game1.currentLocation || Game1.currentLocation == null || Game1.currentLocation.currentEvent == null ) )
            {
                return;
            }

            int currAnimFrame = (int)Util.GetInstanceField(typeof(FarmerSprite), farmer.FarmerSprite, "currentAnimationFrames");
            if (farmer.CurrentTool != null &&
                farmer.FarmerSprite.indexInCurrentAnimation >= currAnimFrame - 1 - 1)
            {
                farmer.CurrentTool = null;
            }

            GameTime gt = new GameTime(new TimeSpan(), new TimeSpan(TimeSpan.TicksPerMillisecond * 16));
            farmer.FarmerSprite.setOwner(farmer); // Not sure why this is necessary
            farmer.UpdateIfOtherPlayer(gt);
        }

        public static void draw( SpriteBatch sb )
        {
            /*
            sb.End();
            sb.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (Multiplayer.mode == Mode.Host)
            {
                if (server.clients == null) return;
                foreach (Server.Client client in server.clients)
                {
                    // Handled in GameLocation, since LocationPacket adds them to the farmers
                    // Better that way because they are sorted properly instead of showing on top of buildings and such
                    //client.farmer.draw(sb);
                }
            }
            else if (Multiplayer.mode == Mode.Client)
            {
                if (client.others == null) return;
                foreach ( KeyValuePair< byte, SFarmer > other in client.others )
                {
                    //other.Value.draw(sb);
                }
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            */
        }

        public static void drawNetworkingDebug(object sender, EventArgs args)
        {
#if NETWORKING_BENCHMARK
            // Render networking benchmark info
            Game1.spriteBatch.DrawString(Game1.smoothFont, "Server -> client bytes transferred: " + Multiplayer.serverToClientBytesTransferred, new Vector2(0, 30), Color.White);
            Game1.spriteBatch.DrawString(Game1.smoothFont, "Client <- server bytes transferred: " + Multiplayer.serverToClientBytesTransferred, new Vector2(0, 50), Color.White);
#endif
        }

        public static bool goingToFestival = false;
        public static void locationChange( GameLocation oldLoc, GameLocation newLoc )
        {
            string newLocName = getUniqueLocationName( newLoc );
            if ( newLocName == "" )
            {
                // Not sure how this happened... But it did
                return;
            }

            Log.debug("(Me) " + SaveGame.loaded.player.name + " moved to " + newLocName + " (" + newLoc + ")");
            MovingStatePacket move = new MovingStatePacket(getMyId(), Game1.player);
            LocationPacket loc = new LocationPacket(getMyId(), newLocName);
            if (!goingToFestival) Multiplayer.sendFunc(loc);
            Multiplayer.sendFunc(move);

            if (Multiplayer.mode == Mode.Host && server.playing)
            {
                // Move everyone to the festival
                if (newLocName == "Temp" && Game1.player.currentLocation.currentEvent != null)
                {
                    foreach (Server.Client other in server.clients)
                    {
                        if (other.farmer.currentLocation != null)
                            other.farmer.currentLocation.farmers.Remove(other.farmer);

                        other.farmer.currentLocation = Game1.player.currentLocation;
                        other.farmer.currentLocation.farmers.Add(other.farmer);
                    }
                    goingToFestival = false;
                }
                else if ( oldLoc != null && oldLoc.Name == "Temp" )
                {
                    foreach (Server.Client other in server.clients)
                    {
                        if (other.farmer.currentLocation != oldLoc)
                            continue;

                        other.farmer.currentLocation.farmers.Remove(other.farmer);

                        other.farmer.currentLocation = Game1.player.currentLocation;
                        other.farmer.currentLocation.farmers.Add(other.farmer);
                    }
                }
            }
            else if (Multiplayer.mode == Mode.Client && client.stage == Client.NetStage.Playing)
            {
                if (newLocName == "Temp" && Game1.player.currentLocation.currentEvent != null)
                {
                    foreach (KeyValuePair< byte, SFarmer > other in client.others)
                    {
                        if (other.Value.currentLocation != null)
                            other.Value.currentLocation.farmers.Remove(other.Value);

                        other.Value.currentLocation = Game1.player.currentLocation;
                        other.Value.currentLocation.farmers.Add(other.Value);
                    }
                    goingToFestival = false;
                }
                else if (oldLoc != null && oldLoc.Name == "Temp")
                {
                    foreach (KeyValuePair< byte, SFarmer > other in client.others)
                    {
                        if (other.Value.currentLocation != oldLoc)
                            continue;

                        other.Value.currentLocation.farmers.Remove(other.Value);

                        other.Value.currentLocation = Game1.player.currentLocation;
                        other.Value.currentLocation.farmers.Add(other.Value);
                    }
                }
            }
        }

        private static MovingStatePacket prevMoving = null;
        private static int prevAnim;
        private static float prevInterval;
        private static StardewValley.Object prevActive;
        private static int prevTool;
        public static CoopUpdatePacket prevCoopState = null;
        public static bool hadDancePartner = false;
        public static string prevSpouse = null;
        public static int prevBooks = 0;
		private static bool rcInit = false;
        private static bool rcLastLogState = true;
        private static List<int> rcLastWoodsState = new List<int>();
        private static List<int> rcLastFarmState = new List<int>();
		
        public static void doMyPlayerUpdates(byte id)
        {
            MovingStatePacket currMoving = new MovingStatePacket(id, Game1.player);
            if (prevMoving == null || prevMoving.flags != currMoving.flags)
            {
                sendFunc(currMoving);
            }
            prevMoving = currMoving;
            
            int currSingleAnim = (int)Util.GetInstanceField(typeof(FarmerSprite), Game1.player.FarmerSprite, "currentSingleAnimation");
            if (prevAnim != currSingleAnim || prevInterval != Game1.player.FarmerSprite.currentSingleAnimationInterval)
            {
                AnimationPacket anim = new AnimationPacket(id, Game1.player);
                if (anim.anim != -1 &&
                    anim.anim != FarmerSprite.walkLeft && anim.anim != FarmerSprite.walkDown &&
                    anim.anim != FarmerSprite.walkRight && anim.anim != FarmerSprite.walkUp &&
                    anim.anim != FarmerSprite.runLeft && anim.anim != FarmerSprite.runDown &&
                    anim.anim != FarmerSprite.runRight && anim.anim != FarmerSprite.runUp &&
                    anim.anim != FarmerSprite.carryWalkLeft && anim.anim != FarmerSprite.carryWalkDown &&
                    anim.anim != FarmerSprite.carryWalkRight && anim.anim != FarmerSprite.carryWalkUp &&
                    anim.anim != FarmerSprite.carryRunLeft && anim.anim != FarmerSprite.carryRunDown &&
                    anim.anim != FarmerSprite.carryRunRight && anim.anim != FarmerSprite.carryRunUp &&
                    anim.anim != FarmerSprite.showHoldingEdible /*&& anim.anim != FarmerSprite.eat*/ )
                {
                    sendFunc(anim);
                }
            }
            prevAnim = currSingleAnim;
            prevInterval = Game1.player.FarmerSprite.currentSingleAnimationInterval;

            if ( prevActive != Game1.player.ActiveObject || prevTool != Game1.player.CurrentToolIndex)
            {
                HeldItemPacket held = new HeldItemPacket(id, Game1.player);
                sendFunc(held);
            }
            prevActive = Game1.player.ActiveObject;
            prevTool = Game1.player.CurrentToolIndex;

            CoopUpdatePacket currCoopState = new CoopUpdatePacket();
            if (prevCoopState == null) prevCoopState = currCoopState;
            if ( !prevCoopState.Equals( currCoopState ) )
            {
                sendFunc(currCoopState);
            }
            prevCoopState = currCoopState;

            if ( Game1.player.dancePartner != null && !hadDancePartner )
            {
                sendFunc(new FestivalDancePartnerPacket( ( byte )(mode == Mode.Host ? 0 : client.id), Game1.player.dancePartner.name ));
                hadDancePartner = true;
            }

            if ( Game1.player.spouse != prevSpouse )
            {
                sendFunc(new SpousePacket((byte)(mode == Mode.Host ? 0 : client.id), prevSpouse));
            }
            prevSpouse = Game1.player.spouse;

            if (Game1.player.archaeologyFound.ContainsKey(102) && Game1.player.archaeologyFound[102][0] != prevBooks)
            {
                sendFunc(new LostBooksPacket());
            }
            prevBooks = Game1.player.archaeologyFound.ContainsKey(102) ? Game1.player.archaeologyFound[102][0] : 0;
        }
    }
}
