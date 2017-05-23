using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValleyMP.Vanilla;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using xTile;

namespace StardewValleyMP.Packets
{
    // Server -> Client
    // Send them the world.
    public class WorldDataPacket : Packet
    {
        public string xml;

        public WorldDataPacket() : base( ID.WorldData )
        {
        }

        public WorldDataPacket(string theXml)
            : this()
        {
            xml = theXml;
        }

        protected override void read(BinaryReader reader)
        {
            //xml = reader.ReadString();
            int len = reader.ReadInt32();
            byte[] data = reader.ReadBytes(len);

            xml = Encoding.ASCII.GetString(MultiplayerMod.ModConfig.Compress ? Util.Decompress(data) : data);
        }

        protected override void write(BinaryWriter writer)
        {
            byte[] data = MultiplayerMod.ModConfig.Compress ? Util.Compress(Encoding.UTF8.GetBytes(xml)) : Encoding.ASCII.GetBytes(xml);

            writer.Write(data.Length);
            writer.Write(data, 0, data.Length);
            // writer.Write(xml);
        }

        public override void process(Client client)
        {
            Log.debug("Got world data");
            //Log.Async(xml);

            SaveGame mine = SaveGame.loaded;
            /*if ( mine.player.spouse != null && mine.player.spouse.EndsWith( "engaged" ) &&
                 mine.countdownToWedding == 0 && !mine.weddingToday )
            {
                // Not (entirely) sure why this is happening in the first place, but this should fix it.
                mine.player.spouse = mine.player.spouse.Replace("engaged", "");
                mine.weddingToday = true;
            }*/

            SaveGame world = ( SaveGame ) SaveGame.serializer.Deserialize(Util.stringStream(xml));

            if ( Multiplayer.COOP )
            {
                mine.player.farmName = world.player.farmName;
                mine.player.money = world.player.money;
                mine.player.clubCoins = world.player.clubCoins;
                mine.player.totalMoneyEarned = world.player.totalMoneyEarned;
                mine.player.hasRustyKey = world.player.hasRustyKey;
                mine.player.hasSkullKey = world.player.hasSkullKey;
                mine.player.hasClubCard = world.player.hasClubCard;
                mine.player.hasDarkTalisman = world.player.hasDarkTalisman;
                mine.player.dateStringForSaveGame = world.player.dateStringForSaveGame;

                foreach (string mail in Multiplayer.checkMail)
                {
                    if (world.mailbox.Contains(mail) && !mine.mailbox.Contains(mail))
                        mine.mailbox.Add(mail);
                    if (world.player.mailForTomorrow.Contains(mail) && !mine.player.mailForTomorrow.Contains(mail))
                        mine.player.mailForTomorrow.Add(mail);
                    if (world.player.mailReceived.Contains(mail) && !mine.player.mailReceived.Contains(mail))
                        mine.player.mailReceived.Add(mail);
                    if (world.mailbox.Contains(mail + "%&NL&%") && !mine.mailbox.Contains(mail + "%&NL&%"))
                        mine.mailbox.Add(mail + "%&NL&%");
                    if (world.player.mailForTomorrow.Contains(mail + "%&NL&%") && !mine.player.mailForTomorrow.Contains(mail + "%&NL&%"))
                        mine.player.mailForTomorrow.Add(mail + "%&NL&%");
                    if (world.player.mailReceived.Contains(mail + "%&NL&%") && !mine.player.mailReceived.Contains(mail + "%&NL&%"))
                        mine.player.mailReceived.Add(mail + "%&NL&%");
                }
            }
            world.player = mine.player;

            world.mailbox = mine.mailbox;
            world.samBandName = mine.samBandName;
            world.elliottBookName = mine.elliottBookName;
            // wallpaper/flooring doesn't look needed?
            world.countdownToWedding = mine.countdownToWedding;
            world.weddingToday = mine.weddingToday;
            world.musicVolume = mine.musicVolume;
            world.soundVolume = mine.soundVolume;
            world.options = mine.options;
            world.minecartHighScore = mine.minecartHighScore;
            if (!Multiplayer.COOP)
            {
                world.stats = mine.stats;
                world.incubatingEgg = mine.incubatingEgg;
                world.dailyLuck = mine.dailyLuck;
                world.whichFarm = mine.whichFarm;
                world.shouldSpawnMonsters = mine.shouldSpawnMonsters;
            }
                world.mine_mineLevel = mine.mine_mineLevel;
                world.mine_nextLevel = mine.mine_nextLevel;
                world.mine_lowestLevelReached = mine.mine_lowestLevelReached;
                world.mine_resourceClumps = mine.mine_resourceClumps;
                world.mine_permanentMineChanges = mine.mine_permanentMineChanges;
            //}

            fixPetMultiplication(mine, world);
            fixRelationships(mine, world);

            Multiplayer.fixLocations(world.locations, null, debugStuff);
            Woods woods = null;
            GameLocation toRemove = null;
            foreach (GameLocation loc in world.locations)
            {
                if (loc.name == "FarmHouse")
                {
                    toRemove = loc;
                }
                else if (loc.name == "Woods")
                {
                    woods = (Woods) loc;
                }
            }
            if (toRemove != null)
            {
                world.locations.Remove(toRemove);
            }
            foreach (GameLocation loc in mine.locations)
            {
                if (loc.name == "FarmHouse")
                {
                    world.locations.Add(loc);
                }
                else if (loc.name == "Woods" && woods != null)
                {
                    Woods myWoods = ( Woods ) loc;
                    woods.hasUnlockedStatue = myWoods.hasUnlockedStatue;
                    woods.hasFoundStardrop = myWoods.hasFoundStardrop;
                }
            }

            // See the giant block of comments in ClientFarmerDataPacket
            foreach (GameLocation theirLoc in world.locations)
            {
                if (theirLoc is FarmHouse)
                {
                    Log.debug("FarmHouse: " + theirLoc.name);
                    NewSaveGame.FarmHouse_setMapForUpgradeLevel(theirLoc as FarmHouse);
                }
                else if (theirLoc is Cellar)
                {
                    theirLoc.map = Game1.game1.xTileContent.Load<Map>("Maps\\Cellar");
                }
                else if (!Multiplayer.COOP && theirLoc is Farm)
                {
                    theirLoc.Map = Game1.game1.xTileContent.Load<Map>("Maps\\" + Farm.getMapNameFromTypeInt(world.whichFarm));
                }
                else if (!Multiplayer.COOP && theirLoc is FarmCave)
                {
                    theirLoc.Map = Game1.game1.xTileContent.Load<Map>("Maps\\FarmCave");
                }
                else if (!Multiplayer.COOP && ( theirLoc.name == "Greenhouse" || theirLoc.name.StartsWith( "Greenhouse_" ) ) )
                {
                    theirLoc.Map = Game1.game1.xTileContent.Load<Map>("Maps\\Greenhouse");
                }
                else if (!Multiplayer.COOP && theirLoc is LibraryMuseum)
                {
                    theirLoc.Map = Game1.game1.xTileContent.Load<Map>("Maps\\ArchaeologyHouse");
                }
                else if (!Multiplayer.COOP && theirLoc is CommunityCenter)
                {
                    theirLoc.Map = Game1.game1.xTileContent.Load<Map>("Maps\\CommunityCenter_Ruins");
                }
            }
            /*
            findReplaceLocation("FarmHouse", world.locations, mine.locations);
            if ( !Multiplayer.COOP )
            {
                findReplaceLocation("Farm", world.locations, mine.locations);
                findReplaceLocation("FarmCave", world.locations, mine.locations);
                findReplaceLocation("Greenhouse", world.locations, mine.locations);
                findReplaceLocation("ArchaeologyHouse", world.locations, mine.locations);
                findReplaceLocation("CommunityCenter", world.locations, mine.locations);
                // ^ How should I do rewards? The ones that affect town permanently
                // Mines?
            }*/

            SaveGame.loaded = world;
            client.stage = Client.NetStage.Waiting;
            //client.tempStopUpdating = true;
        }

        private void debugStuff(GameLocation loc, string oldName, object extra)
        {
            Log.debug("FIXED:" + oldName + "->" + loc.name);
        }

        private void findReplaceLocation( String name, List< GameLocation > find, List< GameLocation > replace )
        {
            return;
            int fi = 0;
            for ( ; fi < find.Count; ++fi)
            {
                if (find[fi].Name == name)
                {
                    break;
                }
            }

            int ri = 0;
            for (; ri < replace.Count; ++ri)
            {
                if (replace[ri].Name == name)
                {
                    break;
                }
            }

            // Just changing find[fi]'s name and adding replace[ri] breaks things
            GameLocation tmp = find[fi];
            find[fi] = replace[ri];
            tmp.name = Multiplayer.processLocationNameForPlayerUnique(null, tmp.name);
            find.Add(tmp);
        }



        private void fixPetMultiplication(SaveGame mine, SaveGame world)
        {
            // Fix pet multiplication - only have our pet
            // If our pet is in our farm, move it to their farm since we're using theirs later

            Farm myFarm = null;
            FarmHouse myHouse = null;
            for (int i = 0; i < mine.locations.Count; ++i)
            {
                if (mine.locations[i].name.Equals("Farm"))
                {
                    myFarm = mine.locations[i] as Farm;
                }
                else if (mine.locations[i].name.Equals("FarmHouse"))
                {
                    myHouse = mine.locations[i] as FarmHouse;
                }
            }
            Farm theirFarm = null;
            FarmHouse theirHouse = null;
            for (int i = 0; i < world.locations.Count; ++i)
            {
                if (world.locations[i].name.Equals("Farm"))
                {
                    theirFarm = world.locations[i] as Farm;
                }
                else if (world.locations[i].name.Equals("FarmHouse"))
                {
                    theirHouse = world.locations[i] as FarmHouse;
                }
            }
            for (int i = 0; i < theirFarm.characters.Count; ++i)
            {
                NPC npc = theirFarm.characters[i];
                if (npc is Pet)
                {
                    theirFarm.characters.Remove(npc);
                    --i;
                    continue;
                }
            }
            for (int i = 0; i < theirHouse.characters.Count; ++i)
            {
                NPC npc = theirHouse.characters[i];
                if (npc is Pet)
                {
                    theirHouse.characters.Remove(npc);
                    --i;
                    continue;
                }
            }
            for (int i = 0; i < myFarm.characters.Count; ++i)
            {
                NPC npc = myFarm.characters[i];
                if (npc is Pet)
                {
                    npc.currentLocation = theirFarm;
                    theirFarm.characters.Add(npc);
                    continue;
                }
            }
        }

        private void fixRelationships( SaveGame mine, SaveGame world )
        {
            // Fix dating status persisting to clients.

            Dictionary<string, bool> dating = new Dictionary<string, bool>();
            foreach ( GameLocation loc in mine.locations )
            {
                foreach (NPC npc in loc.characters)
                    if (npc.isVillager() && !dating.ContainsKey(npc.name))
                        dating.Add(npc.name, npc.datingFarmer);
                if ( loc is BuildableGameLocation )
                {
                    foreach ( Building building in ( loc as BuildableGameLocation ).buildings )
                    {
                        if (building.indoors == null) continue;
                        foreach (NPC npc in building.indoors.characters)
                            if (npc.isVillager() && !dating.ContainsKey(npc.name))
                                dating.Add(npc.name, npc.datingFarmer);
                    }
                }
            }
            foreach (GameLocation loc in world.locations)
            {
                foreach (NPC npc in loc.characters)
                    if (npc.isVillager() && dating.ContainsKey(npc.name))
                        npc.datingFarmer = dating[npc.name];
                if (loc is BuildableGameLocation)
                {
                    foreach (Building building in (loc as BuildableGameLocation).buildings)
                    {
                        if (building.indoors == null) continue;
                        foreach (NPC npc in building.indoors.characters)
                            if (npc.isVillager() && dating.ContainsKey(npc.name))
                                npc.datingFarmer = dating[npc.name];
                    }
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + " size:" + (xml == null ? 0 : xml.Length);
        }
    }
}
