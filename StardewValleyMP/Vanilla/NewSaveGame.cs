using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using StardewModdingAPI;
using StardewValleyMP.Packets;
using xTile;
using xTile.Tiles;
using xTile.Format;
using xTile.ObjectModel;
using Object = StardewValley.Object;

namespace StardewValleyMP.Vanilla
{
    public class NewSaveGame : SaveGame
    {
        public static bool Load(string filename, bool skip = false)
        {
            if (skip) goto skipTo;

            skipTo:
            Game1.currentLoader = NewSaveGame.getLoadEnumerator(filename, skip);
            Game1.gameMode = 6;
            Game1.loadingMessage = "Loading...";

            return true;
        }

        public static IEnumerator<int> getLoadEnumerator(string file, bool skip = false)
        {
            SaveGame saveGame = new SaveGame();
            yield return 1;
            string[] folderPath = new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Saves", file, file };
            string str = Path.Combine(folderPath);
            if (skip) goto skipTo; // I had this one section lower, but since I use MEOW
                                   // as the filename when changing days, things messed up.
            if (!File.Exists(str))
            {
                str = string.Concat(str, ".xml");
                if (!File.Exists(str))
                {
                    Game1.gameMode = 9;
                    Game1.debugOutput = "File does not exist (-_-)";
                    yield break;
                }
            }
            yield return 5;
            Stream stream = null;
            try
            {
                stream = File.Open(str, FileMode.Open);
                goto Label0;
            }
            catch (IOException oException)
            {
                Game1.gameMode = 9;
                Game1.debugOutput = Game1.parseText(oException.Message);
                if (stream != null)
                {
                    stream.Close();
                }
            }
            yield break;
        Label0:
            Game1.loadingMessage = "Loading (Deserializing)...";
            yield return 7;
            SaveGame.loaded = (SaveGame)SaveGame.serializer.Deserialize(stream);
            Game1.loadingMessage = "Creating Base World...";
            yield return 20;
            stream.Close();

        ////////////////////////////////////////
        skipTo:
            Log.Async("Initial loading done");
            if (Multiplayer.mode == Mode.Host)
            {
                foreach ( Server.Client client in Multiplayer.server.clients )
                {
                    foreach ( KeyValuePair< string, GameLocation > pair in client.addDuringLoading )
                    {
                        ClientFarmerDataPacket.addFixedLocationToOurWorld(pair.Value, pair.Key, client);
                    }
                    client.addDuringLoading.Clear();
                }

                Multiplayer.server.getPlayerInfo();
                Multiplayer.server.broadcastInfo();
            }
            else if (Multiplayer.mode == Mode.Client)
            {
                while (Multiplayer.client.stage != Client.NetStage.Waiting)
                {
                    try
                    {
                        Multiplayer.client.update();
                        if (Multiplayer.client == null)
                        {
                            Log.Async("Bad connection or something");
                            yield break;
                        }
                        if (Multiplayer.client.stage == Client.NetStage.WaitingForID && Multiplayer.client.id != 255)
                        {
                            String xml = File.ReadAllText(str);
                            ClientFarmerDataPacket farmerData = new ClientFarmerDataPacket(xml);
                            Multiplayer.client.send(farmerData);

                            Multiplayer.client.stage = Client.NetStage.WaitingForWorldData;
                        }
                    }
                    catch (Exception e) { Log.Async("Exception loading world: " + e); }
                    yield return 20;
                }
            }
            Multiplayer.locations.Clear();
            NPCMonitor.reset();
            Log.Async("MP loading done");
            ////////////////////////////////////////


            Game1.whichFarm = SaveGame.loaded.whichFarm;
            Game1.stats = SaveGame.loaded.stats;
            Game1.year = SaveGame.loaded.year;
            if (!skip) // I can't figure out all these day2+ location glitches. So this quick patch is here instead
                Game1.loadForNewGame(true);
            ////////////////////////////////////////
            Multiplayer.addOtherLocations();
            ////////////////////////////////////////
            Game1.uniqueIDForThisGame = SaveGame.loaded.uniqueIDForThisGame;
            Game1.random = new Random((int)((int)Game1.uniqueIDForThisGame / 2 + Game1.stats.DaysPlayed + 1));
            Game1.weatherForTomorrow = SaveGame.loaded.weatherForTomorrow;
            Game1.spawnMonstersAtNight = SaveGame.loaded.shouldSpawnMonsters;
            Game1.dayOfMonth = SaveGame.loaded.dayOfMonth;
            Game1.year = SaveGame.loaded.year;
            Game1.currentSeason = SaveGame.loaded.currentSeason;
            Game1.loadingMessage = "Loading Player...";
            if (SaveGame.loaded.mine_permanentMineChanges != null)
            {
                Game1.mine = new MineShaft()
                {
                    mineLevel = SaveGame.loaded.mine_mineLevel,
                    nextLevel = SaveGame.loaded.mine_nextLevel,
                    permanentMineChanges = SaveGame.loaded.mine_permanentMineChanges,
                    resourceClumps = SaveGame.loaded.mine_resourceClumps,
                    lowestLevelReached = SaveGame.loaded.mine_lowestLevelReached
                };
            }
            yield return 26;
            Game1.isRaining = SaveGame.loaded.isRaining;
            Game1.isLightning = SaveGame.loaded.isLightning;
            Game1.isSnowing = SaveGame.loaded.isSnowing;
            SaveGame.loadDataToFarmer(SaveGame.loaded.player, null);
            Game1.loadingMessage = "Loading Maps...";
            yield return 36;
            ////////////////////////////////////////
            Farmer oldPlayer = Game1.player;
            try
            {
                /*SaveGame.*/
                loadDataToLocations(SaveGame.loaded.locations);

                Game1.getLocationFromName("FarmHouse").resetForPlayerEntry();
            }
            catch (Exception e)
            {
                Log.Async("Exception loading locations: " + e);
            }
            Game1.player = oldPlayer;
            ////////////////////////////////////////
            yield return 50;
            yield return 51;
            Game1.isDebrisWeather = SaveGame.loaded.isDebrisWeather;
            if (!Game1.isDebrisWeather)
            {
                Game1.debrisWeather.Clear();
            }
            else
            {
                Game1.populateDebrisWeatherArray();
            }
            yield return 53;
            Game1.dailyLuck = SaveGame.loaded.dailyLuck;
            yield return 54;
            yield return 55;
            try
            {
                Game1.bloomDay = SaveGame.loaded.bloomDay;
                /*Game1.*/setGraphicsForSeason();
            }
            catch (Exception e) { Log.Async("Exception doing seasonal graphics: " + e); }
    yield return 56;
            Game1.samBandName = SaveGame.loaded.samBandName;
            Game1.elliottBookName = SaveGame.loaded.elliottBookName;
            Game1.shippingTax = SaveGame.loaded.shippingTax;
            Game1.cropsOfTheWeek = SaveGame.loaded.cropsOfTheWeek;
            yield return 58;
            Game1.mailbox = new Queue<string>(SaveGame.loaded.mailbox);
            yield return 60;
            FurniturePlacer.addAllFurnitureOwnedByFarmer();
            yield return 63;
            Game1.weddingToday = SaveGame.loaded.weddingToday;
            Game1.loadingMessage = "Loading Mines...";
            yield return 64;
            Game1.loadingMessage = "Performing Miscellaneous Tasks...";
            yield return 73;
            Game1.farmerWallpaper = SaveGame.loaded.farmerWallpaper;
            yield return 75;
            Game1.updateWallpaperInFarmHouse(Game1.farmerWallpaper);
            yield return 77;
            Game1.FarmerFloor = SaveGame.loaded.FarmerFloor;
            yield return 79;
            Game1.updateFloorInFarmHouse(Game1.FarmerFloor);
            Game1.options.musicVolumeLevel = SaveGame.loaded.musicVolume;
            Game1.options.soundVolumeLevel = SaveGame.loaded.soundVolume;
            yield return 83;
            Game1.countdownToWedding = SaveGame.loaded.countdownToWedding;
            yield return 85;
            yield return 87;
            Game1.chanceToRainTomorrow = SaveGame.loaded.chanceToRainTomorrow;
            yield return 88;
            yield return 95;
            Game1.currentSongIndex = SaveGame.loaded.currentSongIndex;
            Game1.fadeToBlack = true;
            Game1.fadeIn = false;
            Game1.fadeToBlackAlpha = 0.99f;
            Vector2 vector2 = Game1.player.mostRecentBed;
            if (Game1.player.mostRecentBed.X <= 0f)
            {
                Game1.player.position = new Vector2(192f, (float)(Game1.tileSize * 6));
            }
            Game1.removeFrontLayerForFarmBuildings();
            Game1.addNewFarmBuildingMaps();
            Game1.currentLocation = Game1.getLocationFromName("FarmHouse");
            Game1.currentLocation.map.LoadTileSheets(Game1.mapDisplayDevice);
            Game1.player.CanMove = true;
            Game1.player.position = Utility.PointToVector2((Game1.getLocationFromName("FarmHouse") as FarmHouse).getBedSpot()) * (float)Game1.tileSize;
            Game1.player.position.Y = Game1.player.position.Y + (float)(Game1.tileSize / 2);
            Game1.player.position.X = Game1.player.position.X - (float)Game1.tileSize;
            Game1.player.faceDirection(1);
            Game1.minecartHighScore = SaveGame.loaded.minecartHighScore;
            Game1.currentWallpaper = SaveGame.loaded.currentWallpaper;
            Game1.currentFloor = SaveGame.loaded.currentFloor;
            Game1.questOfTheDay = Utility.getQuestOfTheDay();
            Game1.dishOfTheDay = SaveGame.loaded.dishOfTheDay;
            Game1.options = SaveGame.loaded.options;
            if (Game1.options != null)
            {
                Game1.options.reApplySetOptions();
            }
            else
            {
                Game1.options = new Options();
            }
            if (Game1.soundBank != null)
            {
                Game1.soundCategory.SetVolume(Game1.options.soundVolumeLevel);
                Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                Game1.ambientCategory.SetVolume(Game1.options.ambientVolumeLevel);
                Game1.footstepCategory.SetVolume(Game1.options.footstepVolumeLevel);
            }
            MultiplayerUtility.latestID = SaveGame.loaded.latestID;
            Multiplayer.prevLatestId = MultiplayerUtility.latestID; // MINE
            if (Game1.isRaining)
            {
                Game1.changeMusicTrack("rain");
            }
            Game1.checkForWedding();
            Game1.updateWeatherIcon();
            ////////////////////////////////////////
            // This line was in the original.
            //SaveGame.loaded = null;
            ////////////////////////////////////////
            Game1.currentLocation = Utility.getHomeOfFarmer(Game1.player);
            Game1.currentLocation.lastTouchActionLocation = Game1.player.getTileLocation();
            foreach (Item item in Game1.player.items)
            {
                if (item == null || !(item is Object))
                {
                    continue;
                }
                (item as Object).reloadSprite();
            }
            Game1.gameMode = 3;
            ////////////////////////////////////////
            //if ( Multiplayer.mode != Mode.Singleplayer )
            //    Game1.exitActiveMenu();

            if (Multiplayer.client != null)
            {
                Multiplayer.client.stage = Client.NetStage.Playing;
                Multiplayer.client.tempStopUpdating = false;
                Game1.player.position = Utility.PointToVector2((Game1.getLocationFromName("FarmHouse") as FarmHouse).getBedSpot()) * (float)Game1.tileSize;
                Farmer expr_777_cp_0 = Game1.player;
                expr_777_cp_0.position.Y = expr_777_cp_0.position.Y + (float)(Game1.tileSize / 2);
                Farmer expr_795_cp_0 = Game1.player;
                expr_795_cp_0.position.X = expr_795_cp_0.position.X - (float)Game1.tileSize;
            }
            if (Multiplayer.server != null)
                Multiplayer.server.playing = true;
            ////////////////////////////////////////
            try
            {
                Game1.fixProblems();
            }
            catch (Exception exception)
            {
            }
            Game1.playMorningSong();
            if (Game1.weddingToday)
            {
                Game1.prepareSpouseForWedding();
                Game1.checkForWedding();
            }
            ////////////////////////////////////////
            // Since this runs each new day, the client will save too.
            // Also, getting spouses to work correctly on each end. I think.
            if (Multiplayer.mode == Mode.Host)
            {
                foreach (Server.Client client in Multiplayer.server.clients)
                {
                    if (client.farmer.spouse == null) continue;
                    NPC npc = Game1.getCharacterFromName(client.farmer.spouse);
                    if (npc == null) continue;
                    npc.setMarried(true);
                }
            }
            else if (Multiplayer.mode == Mode.Client)
            {
                foreach (KeyValuePair<byte, Farmer> other in Multiplayer.client.others)
                {
                    if (other.Value.spouse == null) continue;
                    NPC npc = Game1.getCharacterFromName(other.Value.spouse);
                    if (npc == null) continue;
                    npc.setMarried(true);
                }
                var it = NewSaveGame.Save();
                while (it.Current < 100)
                {
                    it.MoveNext();
                    Thread.Sleep(10);
                }
            }
            if (Game1.player.spouse != null)
            {
                var npc = Game1.getCharacterFromName(Game1.player.spouse);
                Multiplayer.sendFunc(new NPCUpdatePacket(npc));
            };
            ////////////////////////////////////////
            yield return 100;
            yield break;
        }

        public static IEnumerator<int> Save( bool skipToFile = false)
        {
            return NewSaveGame.getSaveEnumerator( skipToFile );
        }

        public static IEnumerator<int> getSaveEnumerator(bool skipToFile = false)
        {
            ////////////////////////////////////////
            if (Multiplayer.mode == Mode.Host)
                Multiplayer.server.delayUpdates = true;
            ////////////////////////////////////////

            yield return 1;
            SaveGame saveGame = new SaveGame()
            {
                player = Game1.player,
                ////////////////////////////////////////
                // MINE: .ToList()
                // Not sure why this helps with all the weird bugs that come after 2-3 days in
                // since I don't entirely understand why they happen in the first place.
                locations = Game1.locations.ToList(),
                ////////////////////////////////////////
                currentSeason = Game1.currentSeason,
                samBandName = Game1.samBandName,
                elliottBookName = Game1.elliottBookName,
                mailbox = Game1.mailbox.ToList<string>(),
                dayOfMonth = Game1.dayOfMonth,
                year = Game1.year,
                farmerWallpaper = Game1.farmerWallpaper,
                FarmerFloor = Game1.FarmerFloor,
                countdownToWedding = Game1.countdownToWedding,
                chanceToRainTomorrow = Game1.chanceToRainTomorrow,
                dailyLuck = Game1.dailyLuck,
                isRaining = Game1.isRaining,
                isLightning = Game1.isLightning,
                isSnowing = Game1.isSnowing,
                isDebrisWeather = Game1.isDebrisWeather,
                shouldSpawnMonsters = Game1.spawnMonstersAtNight,
                weddingToday = Game1.weddingToday,
                stats = Game1.stats,
                whichFarm = Game1.whichFarm,
                minecartHighScore = Game1.minecartHighScore,
                uniqueIDForThisGame = Game1.uniqueIDForThisGame,
                musicVolume = Game1.options.musicVolumeLevel,
                soundVolume = Game1.options.soundVolumeLevel,
                shippingTax = Game1.shippingTax,
                cropsOfTheWeek = Game1.cropsOfTheWeek
            };
            if (Game1.mine != null)
            {
                saveGame.mine_lowestLevelReached = Game1.mine.lowestLevelReached;
                saveGame.mine_mineLevel = Game1.mine.mineLevel;
                saveGame.mine_nextLevel = Game1.mine.nextLevel;
                saveGame.mine_permanentMineChanges = Game1.mine.permanentMineChanges;
                saveGame.mine_resourceClumps = Game1.mine.resourceClumps;
            }
            saveGame.currentFloor = Game1.currentFloor;
            saveGame.currentWallpaper = Game1.currentWallpaper;
            saveGame.bloomDay = Game1.bloomDay;
            saveGame.dishOfTheDay = Game1.dishOfTheDay;
            saveGame.latestID = MultiplayerUtility.latestID;
            saveGame.options = Game1.options;
            saveGame.currentSongIndex = Game1.currentSongIndex;
            saveGame.weatherForTomorrow = Game1.weatherForTomorrow;
            ////////////////////////////////////////
            //if ( saveToLoaded )
            if (skipToFile) Multiplayer.locations.Clear();
            if (skipToFile) NPCMonitor.reset();
            if (Multiplayer.mode == Mode.Host)
            {
                Log.Async("Broadcasting on save.");
                try
                {
                    SaveGame.loaded = saveGame;
                    Multiplayer.server.broadcastInfo();
                    Multiplayer.server.broadcast(new NextDayPacket());
                }
                catch (Exception e)
                {
                    Log.Async("Exception during broadcast: " + e);
                }
                //yield return 100;
                //yield break;
            }
            else if (Multiplayer.mode == Mode.Client)
            {
                SaveGame.loaded = saveGame;
            }
            if (skipToFile)
            {
                if (Multiplayer.mode == Mode.Host)
                    Multiplayer.server.delayUpdates = false;

                yield return 100;
                yield break;
            }
            ////////////////////////////////////////
            string str = "_STARDEWVALLEYSAVETMP";
            string name = Game1.player.Name;
            string str1 = name;
            for (int i = 0; i < str1.Length; i++)
            {
                char chr = str1[i];
                if (!char.IsLetterOrDigit(chr))
                {
                    string str2 = name;
                    string str3 = chr.ToString();
                    if (str3 == null)
                    {
                        str3 = "";
                    }
                    name = str2.Replace(str3, "");
                }
            }
            string str4 = string.Concat(name, "_", Game1.uniqueIDForThisGame);
            object[] objArray = new object[] { name, "_", Game1.uniqueIDForThisGame, str };
            string str5 = string.Concat(objArray);
            string str6 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), str5);
            SaveGame.ensureFolderStructureExists("");
            string str7 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), string.Concat("SaveGameInfo", str));
            if (File.Exists(str6))
            {
                File.Delete(str6);
            }
            if (File.Exists(str7))
            {
                File.Delete(str7);
            }
            Stream stream = null;
            try
            {
                stream = File.Create(str6);
            }
            catch (IOException oException)
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
                Game1.gameMode = 9;
                Game1.debugOutput = Game1.parseText(oException.Message);
                goto Label0;
            }
            XmlWriterSettings xmlWriterSetting = new XmlWriterSettings()
            {
                CloseOutput = true
            };
            using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSetting))
            {
                xmlWriter.WriteStartDocument();
                SaveGame.serializer.Serialize(xmlWriter, saveGame);
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
            }
            Farmer totalMinutes = Game1.player;
            TimeSpan utcNow = DateTime.UtcNow - new DateTime(2012, 6, 22);
            totalMinutes.saveTime = (int)utcNow.TotalMinutes;
            try
            {
                stream = File.Create(str7);
                goto Label1;
            }
            catch (IOException oException1)
            {
                if (stream != null)
                {
                    stream.Close();
                }
                Game1.gameMode = 9;
                Game1.debugOutput = Game1.parseText(oException1.Message);
            }
        Label0:
            ////////////////////////////////////////
            if (Multiplayer.mode == Mode.Host)
                Multiplayer.server.delayUpdates = false;
            ////////////////////////////////////////
            yield break;
        Label1:
            xmlWriterSetting = new XmlWriterSettings()
            {
                CloseOutput = true
            };
            using (XmlWriter xmlWriter1 = XmlWriter.Create(stream, xmlWriterSetting))
            {
                xmlWriter1.WriteStartDocument();
                SaveGame.farmerSerializer.Serialize(xmlWriter1, Game1.player);
                xmlWriter1.WriteEndDocument();
                xmlWriter1.Flush();
            }
            str6 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), str4);
            str7 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), "SaveGameInfo");
            str6 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), str4);
            str7 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), "SaveGameInfo");
            string str8 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), string.Concat(str4, "_old"));
            string str9 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), "SaveGameInfo_old");
            if (File.Exists(str8))
            {
                File.Delete(str8);
            }
            if (File.Exists(str9))
            {
                File.Delete(str9);
            }
            try
            {
                File.Move(str6, str8);
                File.Move(str7, str9);
            }
            catch (Exception exception)
            {
            }
            if (File.Exists(str6))
            {
                File.Delete(str6);
            }
            if (File.Exists(str7))
            {
                File.Delete(str7);
            }
            str6 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), str5);
            str7 = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley"), "Saves"), str4), string.Concat("SaveGameInfo", str));
            if (File.Exists(str6))
            {
                File.Move(str6, str6.Replace(str, ""));
            }
            if (File.Exists(str7))
            {
                File.Move(str7, str7.Replace(str, ""));
            }
            yield return 100;
            ////////////////////////////////////////
            if (Multiplayer.mode == Mode.Host)
                Multiplayer.server.delayUpdates = false;
            ////////////////////////////////////////
            yield break;
        }


        private static Farmer findOwnerOf( FarmHouse house )
        {
            if (!house.name.Contains('_')) return Game1.player;

            string name = house.name.Substring(house.name.LastIndexOf('_') + 1);
            return Multiplayer.getFarmer(name);
        }

        public static void loadDataToFarmer(Farmer tmp, Farmer target = null)
        {
            if (target == null)
            {
                target = Game1.player;
            }
            target = tmp;
            target.items = tmp.items;
            target.canMove = true;
            target.sprite = new FarmerSprite(null);
            target.FarmerSprite.setOwner(target);
            target.reloadLivestockSprites();
            if (target.cookingRecipes == null || target.cookingRecipes.Count == 0)
            {
                target.cookingRecipes.Add("Fried Egg", 0);
            }
            if (target.craftingRecipes == null || target.craftingRecipes.Count == 0)
            {
                target.craftingRecipes.Add("Lumber", 0);
            }
            if (!target.songsHeard.Contains("title_day"))
            {
                target.songsHeard.Add("title_day");
            }
            if (!target.songsHeard.Contains("title_night"))
            {
                target.songsHeard.Add("title_night");
            }
            if (target.addedSpeed > 0)
            {
                target.addedSpeed = 0;
            }
            target.maxItems = tmp.maxItems;
            for (int i = 0; i < target.maxItems; i++)
            {
                if (target.items.Count <= i)
                {
                    target.items.Add(null);
                }
            }
            if (target.FarmerRenderer == null)
            {
                target.FarmerRenderer = new FarmerRenderer(target.getTexture());
            }
            target.changeGender(tmp.isMale);
            target.changeAccessory(tmp.accessory);
            target.changeShirt(tmp.shirt);
            target.changePants(tmp.pantsColor);
            target.changeSkinColor(tmp.skin);
            target.changeHairColor(tmp.hairstyleColor);
            target.changeHairStyle(tmp.hair);
            if (target.boots != null)
            {
                target.changeShoeColor(tmp.boots.indexInColorSheet);
            }
            target.Stamina = tmp.Stamina;
            target.health = tmp.health;
            target.MaxStamina = tmp.MaxStamina;
            target.mostRecentBed = tmp.mostRecentBed;
            target.position = target.mostRecentBed;
            Farmer expr_1E2_cp_0_cp_0 = target;
            expr_1E2_cp_0_cp_0.position.X = expr_1E2_cp_0_cp_0.position.X - (float)Game1.tileSize;
            //Game1.player = target;
            /*Game1.player*/target.checkForLevelTenStatus();
            if (!/*Game1.player*/target.craftingRecipes.ContainsKey("Wood Path"))
            {
                /*Game1.player*/
                target.craftingRecipes.Add("Wood Path", 1);
            }
            if (!/*Game1.player*/target.craftingRecipes.ContainsKey("Gravel Path"))
            {
                /*Game1.player*/
                target.craftingRecipes.Add("Gravel Path", 1);
            }
            if (!/*Game1.player*/target.craftingRecipes.ContainsKey("Cobblestone Path"))
            {
                /*Game1.player*/
                target.craftingRecipes.Add("Cobblestone Path", 1);
            }
        }

        public static void loadDataToLocations(List<GameLocation> gamelocations)
        {
            foreach (GameLocation current in gamelocations)
            {
                if (current is FarmHouse)
                {
                    GameLocation locationFromName = Game1.getLocationFromName(current.name);
                    //(Game1.getLocationFromName("FarmHouse") as FarmHouse).upgradeLevel = (current as FarmHouse).upgradeLevel;
                    (locationFromName as FarmHouse).upgradeLevel = (current as FarmHouse).upgradeLevel;
                    //(locationFromName as FarmHouse).setMapForUpgradeLevel((locationFromName as FarmHouse).upgradeLevel, true);
                    ////////////////////////////////////////
                    try
                    {
                        FarmHouse_setMapForUpgradeLevel((FarmHouse)locationFromName);
                    }
                    catch (Exception e) { Log.Async("EXception setting farmhouse map for " + Game1.player.name + ": " + e); }
                    ////////////////////////////////////////
                    (locationFromName as FarmHouse).wallPaper = (current as FarmHouse).wallPaper;
                    (locationFromName as FarmHouse).floor = (current as FarmHouse).floor;
                    (locationFromName as FarmHouse).furniture = (current as FarmHouse).furniture;
                    (locationFromName as FarmHouse).fireplaceOn = (current as FarmHouse).fireplaceOn;
                    (locationFromName as FarmHouse).fridge = (current as FarmHouse).fridge;
                    (locationFromName as FarmHouse).farmerNumberOfOwner = (current as FarmHouse).farmerNumberOfOwner;
                    //(locationFromName as FarmHouse).resetForPlayerEntry();
                    ////////////////////////////////////////
                    Farmer oldPlayer = Game1.player;
                    Game1.player = findOwnerOf(current as FarmHouse);
                    try
                    {
                        (locationFromName as FarmHouse).resetForPlayerEntry();
                    }
                    catch (Exception e) { Log.Async("Exception reseting " + Game1.player.name + "'s house: " + e); }
                    Game1.player = oldPlayer;
                    ////////////////////////////////////////
                    using (List<Furniture>.Enumerator enumerator2 = (locationFromName as FarmHouse).furniture.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            enumerator2.Current.updateDrawPosition();
                        }
                    }
                    locationFromName.lastTouchActionLocation = Game1.player.getTileLocation();
                }
                if (current.name.Equals("Farm"))
                {
                    GameLocation locationFromName2 = Game1.getLocationFromName(current.name);
                    using (List<Building>.Enumerator enumerator3 = ((Farm)current).buildings.GetEnumerator())
                    {
                        while (enumerator3.MoveNext())
                        {
                            enumerator3.Current.load();
                        }
                    }
                    ((Farm)locationFromName2).buildings = ((Farm)current).buildings;
                    using (Dictionary<long, FarmAnimal>.ValueCollection.Enumerator enumerator4 = ((Farm)current).animals.Values.GetEnumerator())
                    {
                        while (enumerator4.MoveNext())
                        {
                            enumerator4.Current.reload();
                        }
                    }
                }
            }
            foreach (GameLocation current2 in gamelocations)
            {
                GameLocation locationFromName3 = Game1.getLocationFromName(current2.name);
                current2.name.Equals("Farm");
                for (int i = current2.characters.Count - 1; i >= 0; i--)
                {
                    if (!current2.characters[i].DefaultPosition.Equals(Vector2.Zero))
                    {
                        current2.characters[i].position = current2.characters[i].DefaultPosition;
                    }
                    current2.characters[i].currentLocation = locationFromName3;
                    if (i < current2.characters.Count)
                    {
                        current2.characters[i].reloadSprite();
                    }
                }
                using (Dictionary<Vector2, TerrainFeature>.ValueCollection.Enumerator enumerator5 = current2.terrainFeatures.Values.GetEnumerator())
                {
                    while (enumerator5.MoveNext())
                    {
                        enumerator5.Current.loadSprite();
                    }
                }
                foreach (KeyValuePair<Vector2, Object> current3 in current2.objects)
                {
                    current3.Value.initializeLightSource(current3.Key);
                    current3.Value.reloadSprite();
                }
                if (current2.name.Equals("Farm"))
                {
                    ((Farm)locationFromName3).buildings = ((Farm)current2).buildings;
                    using (Dictionary<long, FarmAnimal>.ValueCollection.Enumerator enumerator4 = ((Farm)current2).animals.Values.GetEnumerator())
                    {
                        while (enumerator4.MoveNext())
                        {
                            enumerator4.Current.reload();
                        }
                    }
                    foreach (Building current4 in Game1.getFarm().buildings)
                    {
                        Vector2 tile = new Vector2((float)current4.tileX, (float)current4.tileY);
                        if (current4.indoors is Shed)
                        {
                            (current4.indoors as Shed).furniture = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).furniture;
                            (current4.indoors as Shed).wallPaper = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).wallPaper;
                            (current4.indoors as Shed).floor = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).floor;
                        }
                        current4.load();
                        if (current4.indoors is Shed)
                        {
                            (current4.indoors as Shed).furniture = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).furniture;
                            (current4.indoors as Shed).wallPaper = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).wallPaper;
                            (current4.indoors as Shed).floor = ((current2 as Farm).getBuildingAt(tile).indoors as Shed).floor;
                        }
                    }
                }
                if (locationFromName3 != null)
                {
                    locationFromName3.characters = current2.characters;
                    locationFromName3.objects = current2.objects;
                    locationFromName3.numberOfSpawnedObjectsOnMap = current2.numberOfSpawnedObjectsOnMap;
                    locationFromName3.terrainFeatures = current2.terrainFeatures;
                    if (locationFromName3.name.Equals("Farm"))
                    {
                        ((Farm)locationFromName3).animals = ((Farm)current2).animals;
                        (locationFromName3 as Farm).piecesOfHay = (current2 as Farm).piecesOfHay;
                        (locationFromName3 as Farm).resourceClumps = (current2 as Farm).resourceClumps;
                        (locationFromName3 as Farm).hasSeenGrandpaNote = (current2 as Farm).hasSeenGrandpaNote;
                        (locationFromName3 as Farm).grandpaScore = (current2 as Farm).grandpaScore;
                    }
                    if (locationFromName3 is Sewer)
                    {
                        (locationFromName3 as Sewer).populateShopStock(Game1.dayOfMonth);
                    }
                    if (locationFromName3 is Beach)
                    {
                        (locationFromName3 as Beach).bridgeFixed = (current2 as Beach).bridgeFixed;
                    }
                    if (locationFromName3 is Woods)
                    {
                        (locationFromName3 as Woods).stumps = (current2 as Woods).stumps;
                        (locationFromName3 as Woods).hasFoundStardrop = (current2 as Woods).hasFoundStardrop;
                        (locationFromName3 as Woods).hasUnlockedStatue = (current2 as Woods).hasUnlockedStatue;
                    }
                    if (locationFromName3 is LibraryMuseum)
                    {
                        (locationFromName3 as LibraryMuseum).museumPieces = (current2 as LibraryMuseum).museumPieces;
                    }
                    if (locationFromName3 is CommunityCenter)
                    {
                        (locationFromName3 as CommunityCenter).bundleRewards = (current2 as CommunityCenter).bundleRewards;
                        (locationFromName3 as CommunityCenter).bundles = (current2 as CommunityCenter).bundles;
                        (locationFromName3 as CommunityCenter).areasComplete = (current2 as CommunityCenter).areasComplete;
                    }
                    if (locationFromName3 is SeedShop)
                    {
                        (locationFromName3 as SeedShop).itemsFromPlayerToSell = (current2 as SeedShop).itemsFromPlayerToSell;
                        (locationFromName3 as SeedShop).itemsToStartSellingTomorrow = (current2 as SeedShop).itemsToStartSellingTomorrow;
                    }
                    if (locationFromName3 is Forest)
                    {
                        if (Game1.dayOfMonth % 7 % 5 == 0)
                        {
                            (locationFromName3 as Forest).travelingMerchantDay = true;
                            (locationFromName3 as Forest).travelingMerchantBounds = new List<Rectangle>();
                            (locationFromName3 as Forest).travelingMerchantBounds.Add(new Rectangle(23 * Game1.tileSize, 10 * Game1.tileSize, 123 * Game1.pixelZoom, 28 * Game1.pixelZoom));
                            (locationFromName3 as Forest).travelingMerchantBounds.Add(new Rectangle(23 * Game1.tileSize + 45 * Game1.pixelZoom, 10 * Game1.tileSize + 26 * Game1.pixelZoom, 19 * Game1.pixelZoom, 12 * Game1.pixelZoom));
                            (locationFromName3 as Forest).travelingMerchantBounds.Add(new Rectangle(23 * Game1.tileSize + 85 * Game1.pixelZoom, 10 * Game1.tileSize + 26 * Game1.pixelZoom, 26 * Game1.pixelZoom, 12 * Game1.pixelZoom));
                            (locationFromName3 as Forest).travelingMerchantStock = Utility.getTravelingMerchantStock();
                            using (List<Rectangle>.Enumerator enumerator7 = (locationFromName3 as Forest).travelingMerchantBounds.GetEnumerator())
                            {
                                while (enumerator7.MoveNext())
                                {
                                    Utility.clearObjectsInArea(enumerator7.Current, locationFromName3);
                                }
                            }
                        }
                        (locationFromName3 as Forest).log = (current2 as Forest).log;
                    }
                }
            }
            Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
        }
     
        public static void FarmHouse_setMapForUpgradeLevel(FarmHouse house)
		{
            int level = house.upgradeLevel;
            bool persist = true;

			if (persist)
			{
				house.upgradeLevel = level;
			}
            Util.SetInstanceField(typeof(FarmHouse),house,"currentlyDisplayedUpgradeLevel", level);
            Farmer tmp = findOwnerOf( house );
			bool flag = ( tmp != null ) ? tmp.isMarried() : false;/*
            if (Utility.getFarmerFromFarmerNumber(house.farmerNumberOfOwner) == null)
			{
				flag = Game1.player.isMarried();
			}
			else
			{
                flag = Utility.getFarmerFromFarmerNumber(house.farmerNumberOfOwner).isMarried();
			}*/
            if (level == 0) flag = false;
            Util.SetInstanceField(typeof(FarmHouse),house,"displayingSpouseRoom", flag);
            house.map = Game1.content.Load<Map>("Maps\\FarmHouse" + ((level == 0) ? "" : ((level == 3) ? "2" : string.Concat(level))) + (flag ? "_marriage" : ""));
            house.map.LoadTileSheets(Game1.mapDisplayDevice);
			if (flag)
			{
                FarmHouse_loadSpouseRoom(house);
            }
            house.loadObjects();
			if (level == 3)
			{
                house.setMapTileIndex(3, 22, 162, "Front", 0);
                house.removeTile(4, 22, "Front");
                house.removeTile(5, 22, "Front");
                house.setMapTileIndex(6, 22, 163, "Front", 0);
                house.setMapTileIndex(3, 23, 64, "Buildings", 0);
                house.setMapTileIndex(3, 24, 96, "Buildings", 0);
                house.setMapTileIndex(4, 24, 165, "Front", 0);
                house.setMapTileIndex(5, 24, 165, "Front", 0);
                house.removeTile(4, 23, "Back");
                house.removeTile(5, 23, "Back");
                house.setMapTileIndex(4, 23, 1043, "Back", 0);
                house.setMapTileIndex(5, 23, 1043, "Back", 0);
                house.setTileProperty(4, 23, "Back", "NoFurniture", "t");
                house.setTileProperty(5, 23, "Back", "NoFurniture", "t");
                house.setMapTileIndex(4, 24, 1075, "Back", 0);
                house.setMapTileIndex(5, 24, 1075, "Back", 0);
                house.setTileProperty(4, 24, "Back", "NoFurniture", "t");
                house.setTileProperty(5, 24, "Back", "NoFurniture", "t");
                house.setMapTileIndex(6, 23, 68, "Buildings", 0);
                house.setMapTileIndex(6, 24, 130, "Buildings", 0);
                house.setMapTileIndex(4, 25, 0, "Front", 0);
                house.setMapTileIndex(5, 25, 0, "Front", 0);
                house.removeTile(4, 23, "Buildings");
                house.removeTile(5, 23, "Buildings");
                house.warps.Add(new Warp(4, 25, "Cellar", 3, 2, false));
                house.warps.Add(new Warp(5, 25, "Cellar", 4, 2, false));
				if (!Game1.player.craftingRecipes.ContainsKey("Cask"))
				{
					Game1.player.craftingRecipes.Add("Cask", 0);
				}
			}
			if (house.wallPaper.Count > 0 && house.floor.Count > 0)
			{
				List<Microsoft.Xna.Framework.Rectangle> list = FarmHouse.getWalls(house.upgradeLevel);
				if (persist)
				{
					while (house.wallPaper.Count < list.Count)
					{
                        house.wallPaper.Add(0);
					}
				}
				list = FarmHouse.getFloors(house.upgradeLevel);
				if (persist)
				{
					while (house.floor.Count < list.Count)
					{
                        house.floor.Add(0);
					}
				}
				if (house.upgradeLevel == 1)
				{
                    house.setFloor(house.floor[0], 1, true);
                    house.setFloor(house.floor[0], 2, true);
                    house.setFloor(house.floor[0], 3, true);
                    house.setFloor(22, 0, true);
				}
				if (house.upgradeLevel == 2)
				{
                    house.setWallpaper(house.wallPaper[0], 4, true);
                    house.setWallpaper(house.wallPaper[2], 6, true);
                    house.setWallpaper(house.wallPaper[1], 5, true);
                    house.setWallpaper(11, 0, true);
                    house.setWallpaper(61, 1, true);
                    house.setWallpaper(61, 2, true);
					int which = house.floor[3];
					house.setFloor(house.floor[2], 5, true);
                    house.setFloor(house.floor[0], 3, true);
                    house.setFloor(house.floor[1], 4, true);
                    house.setFloor(which, 6, true);
                    house.setFloor(1, 0, true);
                    house.setFloor(31, 1, true);
                    house.setFloor(31, 2, true);
				}
			}
            house.lightGlows.Clear();
		}

        private static void FarmHouse_loadSpouseRoom( FarmHouse house )
		{
			NPC spouse = findOwnerOf( house ).getSpouse();/*
			if (Utility.getFarmerFromFarmerNumber(this.farmerNumberOfOwner) == null)
			{
				spouse = Game1.player.getSpouse();
			}
			else
			{
				spouse = Utility.getFarmerFromFarmerNumber(this.farmerNumberOfOwner).getSpouse();
			}*/
			if (spouse != null)
			{
				int num = -1;
				string name;
				switch (name = spouse.name)
				{
				case "Abigail":
					num = 0;
					break;
				case "Penny":
					num = 1;
					break;
				case "Leah":
					num = 2;
					break;
				case "Haley":
					num = 3;
					break;
				case "Maru":
					num = 4;
					break;
				case "Sebastian":
					num = 5;
					break;
				case "Alex":
					num = 6;
					break;
				case "Harvey":
					num = 7;
					break;
				case "Elliott":
					num = 8;
					break;
				case "Sam":
					num = 9;
					break;
                case "Shane":
                    num = 10;
                    break;
                case "Emily":
                    num = 11;
                    break;
                }
                Microsoft.Xna.Framework.Rectangle rectangle = (house.upgradeLevel == 1) ? new Microsoft.Xna.Framework.Rectangle(29, 1, 6, 9) : new Microsoft.Xna.Framework.Rectangle(35, 10, 6, 9);
				Map map = Game1.content.Load<Map>("Maps\\spouseRooms");
				Point point = new Point(num % 5 * 6, num / 5 * 9);
                house.map.Properties.Remove("DayTiles");
                house.map.Properties.Remove("NightTiles");
				for (int i = 0; i < rectangle.Width; i++)
				{
					for (int j = 0; j < rectangle.Height; j++)
					{
						if (map.GetLayer("Back").Tiles[point.X + i, point.Y + j] != null)
						{
                            house.map.GetLayer("Back").Tiles[rectangle.X + i, rectangle.Y + j] = new StaticTile(house.map.GetLayer("Back"), house.map.TileSheets[0], BlendMode.Alpha, map.GetLayer("Back").Tiles[point.X + i, point.Y + j].TileIndex);
						}
						if (map.GetLayer("Buildings").Tiles[point.X + i, point.Y + j] != null)
						{
                            house.map.GetLayer("Buildings").Tiles[rectangle.X + i, rectangle.Y + j] = new StaticTile(house.map.GetLayer("Buildings"), house.map.TileSheets[0], BlendMode.Alpha, map.GetLayer("Buildings").Tiles[point.X + i, point.Y + j].TileIndex);
                            Util.CallInstanceMethod(typeof(FarmHouse),house,"adjustMapLightPropertiesForLamp",new object[]{map.GetLayer("Buildings").Tiles[point.X + i, point.Y + j].TileIndex, rectangle.X + i, rectangle.Y + j, "Buildings"});
						}
						else
						{
                            house.map.GetLayer("Buildings").Tiles[rectangle.X + i, rectangle.Y + j] = null;
						}
						if (j < rectangle.Height - 1 && map.GetLayer("Front").Tiles[point.X + i, point.Y + j] != null)
						{
                            house.map.GetLayer("Front").Tiles[rectangle.X + i, rectangle.Y + j] = new StaticTile(house.map.GetLayer("Front"), house.map.TileSheets[0], BlendMode.Alpha, map.GetLayer("Front").Tiles[point.X + i, point.Y + j].TileIndex);
                            Util.CallInstanceMethod(typeof(FarmHouse),house,"adjustMapLightPropertiesForLamp", new object[]{map.GetLayer("Front").Tiles[point.X + i, point.Y + j].TileIndex, rectangle.X + i, rectangle.Y + j, "Front"});
						}
						else if (j < rectangle.Height - 1)
						{
                            house.map.GetLayer("Front").Tiles[rectangle.X + i, rectangle.Y + j] = null;
						}
						if (i == 4 && j == 4)
						{
                            house.map.GetLayer("Back").Tiles[rectangle.X + i, rectangle.Y + j].Properties.Add(new KeyValuePair<string, PropertyValue>("NoFurniture", new PropertyValue("T")));
						}
					}
				}
			}
		}

        public static void setGraphicsForSeason()
        {
            foreach (GameLocation current in Game1.locations)
            {
                ////////////////////////////////////////
                //Log.Async("name:" + current.name + " " + current.Map);
                try
                {
                    // Doesn't work for community center, but I don't see why I should implement visiting those
                    if (current.name.Contains( '_' ) && current.Map == null)
                    {
                        current.Map = Game1.content.Load<Map>("Maps\\" + current.name.Substring(0, current.name.IndexOf('_')));
                    }
                }
                catch ( Exception e )
                {
                    Log.Async("Exception fixing map for " + current.name + ": " + e);
                }
                ////////////////////////////////////////

                current.seasonUpdate(Game1.currentSeason, true);
                if (current.IsOutdoors)
                {
                    if (!current.Name.Equals("Desert"))
                    {
                        current.Map.TileSheets.Count();
                        for (int i = 0; i < current.Map.TileSheets.Count<TileSheet>(); i++)
                        {
                            if (!current.Map.TileSheets[i].ImageSource.Contains("path") && !current.Map.TileSheets[i].ImageSource.Contains("object"))
                            {
                                current.Map.TileSheets[i].ImageSource = "Maps\\" + Game1.currentSeason + "_" + current.Map.TileSheets[i].ImageSource.Split(new char[]
								{
									'_'
								})[1];
                                current.Map.DisposeTileSheets(Game1.mapDisplayDevice);
                                current.Map.LoadTileSheets(Game1.mapDisplayDevice);
                            }
                        }
                    }
                    if (Game1.currentSeason.Equals("spring"))
                    {
                        foreach (KeyValuePair<Vector2, Object> current2 in current.Objects)
                        {
                            if ((current2.Value.Name.Contains("Stump") || current2.Value.Name.Contains("Boulder") || current2.Value.Name.Equals("Stick") || current2.Value.Name.Equals("Stone")) && current2.Value.ParentSheetIndex >= 378 && current2.Value.ParentSheetIndex <= 391)
                            {
                                current2.Value.ParentSheetIndex -= 376;
                            }
                        }
                        Game1.eveningColor = new Color(255, 255, 0);
                    }
                    else if (Game1.currentSeason.Equals("summer"))
                    {
                        foreach (KeyValuePair<Vector2, Object> current3 in current.Objects)
                        {
                            if (current3.Value.Name.Contains("Weed"))
                            {
                                if (current3.Value.parentSheetIndex == 792)
                                {
                                    Object expr_280 = current3.Value;
                                    int parentSheetIndex = expr_280.ParentSheetIndex;
                                    expr_280.ParentSheetIndex = parentSheetIndex + 1;
                                }
                                else if (Game1.random.NextDouble() < 0.3)
                                {
                                    current3.Value.ParentSheetIndex = 676;
                                }
                                else if (Game1.random.NextDouble() < 0.3)
                                {
                                    current3.Value.ParentSheetIndex = 677;
                                }
                            }
                        }
                        Game1.eveningColor = new Color(255, 255, 0);
                    }
                    else
                    {
                        if (Game1.currentSeason.Equals("fall"))
                        {
                            foreach (KeyValuePair<Vector2, Object> current4 in current.Objects)
                            {
                                if (current4.Value.Name.Contains("Weed"))
                                {
                                    if (current4.Value.parentSheetIndex == 793)
                                    {
                                        Object expr_377 = current4.Value;
                                        int parentSheetIndex = expr_377.ParentSheetIndex;
                                        expr_377.ParentSheetIndex = parentSheetIndex + 1;
                                    }
                                    else if (Game1.random.NextDouble() < 0.5)
                                    {
                                        current4.Value.ParentSheetIndex = 678;
                                    }
                                    else
                                    {
                                        current4.Value.ParentSheetIndex = 679;
                                    }
                                }
                            }
                            Game1.eveningColor = new Color(255, 255, 0);
                            using (List<WeatherDebris>.Enumerator enumerator5 = Game1.debrisWeather.GetEnumerator())
                            {
                                while (enumerator5.MoveNext())
                                {
                                    WeatherDebris current5 = enumerator5.Current;
                                    current5.which = 2;
                                }
                                continue;
                            }
                        }
                        if (Game1.currentSeason.Equals("winter"))
                        {
                            for (int j = current.Objects.Count<KeyValuePair<Vector2, Object>>() - 1; j >= 0; j--)
                            {
                                Object @object = current.Objects[current.Objects.Keys.ElementAt(j)];
                                if (@object.Name.Contains("Weed"))
                                {
                                    current.Objects.Remove(current.Objects.Keys.ElementAt(j));
                                }
                                else if (((!@object.Name.Contains("Stump") && !@object.Name.Contains("Boulder") && !@object.Name.Equals("Stick") && !@object.Name.Equals("Stone")) || @object.ParentSheetIndex > 100) && current.IsOutdoors && !@object.isHoedirt)
                                {
                                    @object.name.Equals("HoeDirt");
                                }
                            }
                            foreach (WeatherDebris current6 in Game1.debrisWeather)
                            {
                                current6.which = 3;
                            }
                            Game1.eveningColor = new Color(245, 225, 170);
                        }
                    }
                }
            }
        }
    }
}
