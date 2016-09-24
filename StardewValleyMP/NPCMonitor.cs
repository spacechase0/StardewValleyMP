using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Buildings;
using StardewModdingAPI;
using StardewValleyMP.States;
using StardewValleyMP.Packets;
using StardewValley.Locations;

namespace StardewValleyMP
{
    class NPCMonitor
    {
        public static bool ignoreUpdates = false;
        private static Dictionary<string, NPCState> npcs = new Dictionary<string, NPCState>();
        private static Dictionary<long, FarmAnimalState> animals = new Dictionary<long, FarmAnimalState>();

        private static List<long> checkMissing = new List<long>();
        public static void startChecks()
        {
            checkMissing = animals.Keys.ToList();
        }

        public static void endChecks()
        {
            foreach (long id in checkMissing)
            {
                if ( !Multiplayer.COOP )
                {
                    Log.Async("NOT IMPLEMENTED:ANIMAL DELETION");
                    continue;
                }

                BuildableGameLocation farm = ( BuildableGameLocation ) Game1.getLocationFromName( "Farm" );
                Building buildingAt = null;
                foreach (Building building in farm.buildings)
                {
                    if (building.tileX == animals[ id ].homeLoc.X && building.tileY == animals[ id ].homeLoc.Y)
                    {
                        buildingAt = building;
                        break;
                    }
                }
                if (buildingAt != null)
                {
                    Log.Async("Sending animal deletion packet");
                    animals.Remove(id);
                    Multiplayer.sendFunc(new FarmAnimalPacket(buildingAt, id));
                }
            }
            checkMissing.Clear();
        }

        public static void check( GameLocation loc )
        {
            checkNPCs( loc );

            if ( loc is Farm )
            {
                Farm farm = loc as Farm;
                foreach (KeyValuePair<long, FarmAnimal> animal in farm.animals)
                {
                    checkAnimal(animal.Value);
                    if (checkMissing.Contains(animal.Key))
                        checkMissing.Remove(animal.Key);
                }
            }
            else if ( loc is AnimalHouse )
            {
                AnimalHouse house = loc as AnimalHouse;
                foreach (KeyValuePair<long, FarmAnimal> animal in house.animals)
                {
                    checkAnimal(animal.Value);
                    if (checkMissing.Contains(animal.Key))
                        checkMissing.Remove(animal.Key);
                }
            }
        }

        public static void reset()
        {
            npcs.Clear();
            animals.Clear();
            checkMissing.Clear();
        }
        
        public static void updateNPC( string name, NPCState state )
        {
            NPC npc = Game1.getCharacterFromName(name);
            if ( npc == null ) return;

            Log.Async("Updating NPC " + name);
            Log.Async("Dating: " + npc.datingFarmer + " -> " + state.datingFarmer);
            Log.Async("Married: " + npc.isMarried() + " -> " + state.married);
            Log.Async("Default Map: " + npc.defaultMap + " -> " + state.defaultMap);
            Log.Async("Default Pos: (" + npc.DefaultPosition.X + ", " + npc.DefaultPosition.Y + ") -> (" + state.defaultX + " , " + state.defaultY + ")");

            npc.datingFarmer = state.datingFarmer;
            npc.setMarried(state.married);
            npc.defaultMap = ( state.defaultMap != "" ) ? state.defaultMap : null;
            npc.DefaultPosition = new Microsoft.Xna.Framework.Vector2(state.defaultX, state.defaultY);

            npcs[name] = state;
        }

        public static void addAnimal( string loc, FarmAnimal animal )
        {
            Log.Async("DEBUG:Adding farm animal");
            AnimalHouse home = (AnimalHouse) Game1.getLocationFromName(loc);
            (home as AnimalHouse).animals.Add(animal.myID, animal);
            (home as AnimalHouse).animalsThatLiveHere.Add(animal.myID);

            if ( !Multiplayer.COOP )
            {
                Log.Async("NOT IMPLEMENTED:ANIMAL ADDITION");
                return;
            }

            BuildableGameLocation farm = ( BuildableGameLocation ) Game1.getLocationFromName( "Farm" );
            Building buildingAt = null;
            foreach (Building building in farm.buildings)
            {
                if (building.tileX == animal.homeLocation.X && building.tileY == animal.homeLocation.Y)
                {
                    buildingAt = building;
                    break;
                }
            }
            if (buildingAt != null)
            {
                animal.home = buildingAt;
            }

            animals[animal.myID] = new FarmAnimalState( animal );
        }

        public static void destroyAnimal(long id)
        {
            Log.Async("DEBUG:Removing farm animal");
            foreach (GameLocation loc in Game1.locations)
            {
                if (!(loc is Farm)) continue;
                Farm farm = loc as Farm;

                foreach (KeyValuePair<long, FarmAnimal> pair in farm.animals)
                {
                    if (pair.Key == id)
                    {
                        destroyAnimal(pair.Value);
                        farm.animals.Remove(pair.Key);
                        return;
                    }
                }

                foreach (Building building in farm.buildings)
                {
                    AnimalHouse house = building.indoors as AnimalHouse;
                    if (house != null)
                    {
                        foreach (KeyValuePair<long, FarmAnimal> animal in house.animals)
                        {
                            if (animal.Key == id)
                            {
                                destroyAnimal(animal.Value);
                                farm.animals.Remove(animal.Key);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void destroyAnimal( FarmAnimal animal )
        {
            AnimalHouse home = (AnimalHouse)animal.home.indoors;
            (home as AnimalHouse).animals.Remove(animal.myID);
            (home as AnimalHouse).animalsThatLiveHere.Remove(animal.myID);

            animals.Remove(animal.myID);
            if ( checkMissing.Contains( animal.myID ) )
            {
                checkMissing.Remove(animal.myID);
            }
        }

        public static void updateAnimal(long id, FarmAnimalState state)
        {
            foreach ( GameLocation loc in Game1.locations )
            {
                if (!(loc is Farm)) continue;
                Farm farm = loc as Farm;

                foreach ( KeyValuePair< long, FarmAnimal > pair in farm.animals )
                {
                    if (pair.Key == id )
                    {
                        updateAnimal(farm, pair.Value, state);
                        return;
                    }
                }

                foreach (Building building in farm.buildings)
                {
                    AnimalHouse house = building.indoors as AnimalHouse;
                    if (house != null)
                    {
                        foreach (KeyValuePair<long, FarmAnimal> animal in house.animals)
                        {
                            if (animal.Key == id)
                            {
                                updateAnimal(farm, animal.Value, state);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void updateAnimal(BuildableGameLocation farm, FarmAnimal animal, FarmAnimalState state)
        {/*
            Log.Async("Updating animal " + animal.myID);
            Log.Async("Name: " + animal.name + " -> " + state.name);
            Log.Async("Reproduction: " + animal.allowReproduction + " -> " + state.reproduce);
            Log.Async("Fullness: " + animal.fullness + " -> " + state.fullness);
            Log.Async("Product: " + animal.currentProduce + " -> " + state.product);
            Log.Async("Petted: " + animal.wasPet + " -> " + state.pet);
            Log.Async("Affection: " + animal.friendshipTowardFarmer + " -> " + state.friendship);
            Log.Async("Home: (" + animal.homeLocation.X + ", " + animal.homeLocation.Y + ") -> (" + state.homeLoc.X + ", " + state.homeLoc.Y + ")");
            */
            animal.name = state.name;
            animal.allowReproduction = state.reproduce;
            animal.fullness = (byte)state.fullness;
            animal.currentProduce = state.product;
            animal.wasPet = state.pet;
            animal.friendshipTowardFarmer = state.friendship;

            if ( animal.homeLocation.X != state.homeLoc.X && animal.homeLocation.Y != state.homeLoc.Y )
            {
                Building buildingAt = null;
                foreach ( Building building in farm.buildings )
                {
                    if ( building.tileX == state.homeLoc.X && building.tileY == state.homeLoc.Y )
                    {
                        buildingAt = building;
                        break;
                    }
                }
                if (buildingAt != null)
                {
                    // From AnimalQueryMenu
                    (animal.home.indoors as AnimalHouse).animalsThatLiveHere.Remove(animal.myID);
                    if ((animal.home.indoors as AnimalHouse).animals.ContainsKey(animal.myID))
                    {
                        (buildingAt.indoors as AnimalHouse).animals.Add(animal.myID, animal);
                        (animal.home.indoors as AnimalHouse).animals.Remove(animal.myID);
                    }

                    animal.home = buildingAt;
                    animal.homeLocation = state.homeLoc;

                    (buildingAt.indoors as AnimalHouse).animalsThatLiveHere.Add(animal.myID);
                }
            }

            animals[animal.myID] = state;
        }

        private static void checkNPCs(GameLocation loc)
        {
            foreach (NPC npc in loc.characters)
            {
                if (npc.name == "Junimo" || npc.name == "Green Slime" || npc.name == "Frost Helly" || npc.IsMonster) continue;
                if ( npc.isMarried() && npc.name != Game1.player.spouse )
                {
                    continue;
                }

                NPCState state = new NPCState(npc);
                if (!npcs.ContainsKey(npc.name))
                {
                    npcs.Add(npc.name, state);
                    continue;
                }

                NPCState oldState = npcs[npc.name];
                if (state.isDifferentEnoughFromOldStateToSend(oldState))
                {
                    npcs[npc.name] = state;
                    if ( !ignoreUpdates )
                        Multiplayer.sendFunc(new NPCUpdatePacket(npc));
                }
            }

        }

        private static void checkAnimal( FarmAnimal animal )
        {
            FarmAnimalState state = new FarmAnimalState(animal);
            if (!animals.ContainsKey(animal.myID))
            {
                animals.Add(animal.myID, state);
                if (!ignoreUpdates)
                {
                    Log.Async("Sending animal creation packet");
                    Multiplayer.sendFunc(new FarmAnimalPacket( animal ));
                }
                return;
            }

            FarmAnimalState oldState = animals[animal.myID];
            if (state.isDifferentEnoughFromOldStateToSend(oldState))
            {
                animals[animal.myID] = state;
                if (!ignoreUpdates)
                    Multiplayer.sendFunc(new FarmAnimalUpdatePacket(animal));
            }
        }
    }
}
