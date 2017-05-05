using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValleyMP.Packets;
using StardewValleyMP.States;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Object = StardewValley.Object;

namespace StardewValleyMP
{
    public class LocationCache
    {
        public readonly GameLocation loc;

        private List<Debris> trackedDebris = new List<Debris>();
        private Dictionary<Building, BuildingState> trackedBuildings = new Dictionary<Building, BuildingState>();
        private Dictionary< Type, Monitor > monitors = new Dictionary< Type, Monitor >();
        public bool ignoreUpdates = false;
        
        public LocationCache(GameLocation theLoc)
        {
            loc = theLoc;

            // This could be done even better with macros.
            // (Although it wouldn't be as bad in the first place if I could use templates.)
            // Sigh. Still cleaner than copying those methods for every new TerrainFeature/Object type.
            monitors.Add( typeof( HoeDirt ),
                          new SpecificMonitor< TerrainFeature, HoeDirt, HoeDirtState, TerrainFeatureUpdatePacket<HoeDirt> > (
                              this, loc.terrainFeatures,
                              (obj) => new HoeDirtState( obj ),
                              (loc_, pos) => new TerrainFeatureUpdatePacket<HoeDirt>(loc_, pos)
                          ));
            monitors.Add( typeof(Tree),
                          new SpecificMonitor<TerrainFeature, Tree, TreeState, TerrainFeatureUpdatePacket<Tree>>(
                              this, loc.terrainFeatures,
                              (obj) => new TreeState(obj),
                              (loc_, pos) => new TerrainFeatureUpdatePacket<Tree>(loc_, pos)
                          ));
            monitors.Add( typeof( FruitTree ),
                          new SpecificMonitor<TerrainFeature, FruitTree, FruitTreeState, TerrainFeatureUpdatePacket<FruitTree>>(
                              this, loc.terrainFeatures,
                              (obj) => new FruitTreeState( obj ),
                              (loc_, pos) => new TerrainFeatureUpdatePacket<FruitTree>(loc_, pos)
                          ));
            monitors.Add(typeof(Door),
                          new SpecificMonitor<Object, Door, DoorState, ObjectUpdatePacket<Door>>(
                              this, loc.objects,
                              (obj) => new DoorState(obj),
                              (loc_, pos) => new ObjectUpdatePacket<Door>(loc_, pos)
                          ));
            monitors.Add(typeof(Fence),
                          new SpecificMonitor<Object, Fence, FenceState, FenceUpdatePacket>(
                              this, loc.objects,
                              (obj) => new FenceState(obj),
                              (loc_, pos) => new FenceUpdatePacket(loc_, pos)
                          ));
            monitors.Add(typeof(Object),
                          new SpecificMonitor<Object, Object, ObjectState, ObjectUpdatePacket<Object>>(
                              this, loc.objects,
                              (obj) => new ObjectState(obj),
                              (loc_, pos) => new ObjectUpdatePacket<Object>(loc_, pos)
                          ));
            monitors.Add(typeof(CrabPot),
                          new SpecificMonitor<Object, CrabPot, CrabPotState, ObjectUpdatePacket<CrabPot>>(
                              this, loc.objects,
                              (obj) => new CrabPotState(obj),
                              (loc_, pos) => new ObjectUpdatePacket<CrabPot>(loc_, pos)
                          ));
            monitors.Add(typeof(Chest), new ChestMonitor( this ) );

            loc.terrainFeatures.CollectionChanged += new NotifyCollectionChangedEventHandler( terrainFeaturesChanged );
            loc.objects.CollectionChanged += new NotifyCollectionChangedEventHandler(objectsChanged);

            // Pre-populate monitor caches so it knows what to watch. Otherwise it would only do new things
            foreach ( KeyValuePair< Vector2, TerrainFeature > tf in loc.terrainFeatures )
            {
                Type type = tf.Value.GetType();
                if ( monitors.ContainsKey( type ) )
                {
                    monitors[ type ].addCache( tf.Key, tf.Value );
                }
            }

            foreach (KeyValuePair<Vector2, Object> obj in loc.objects)
            {
                Type type = obj.Value.GetType();
                if (monitors.ContainsKey(type))
                {
                    monitors[type].addCache(obj.Key, obj.Value);
                }
            }

            ignoreUpdates = true;
            checkBuildings();
            checkLocationSpecificStuff();
            ignoreUpdates = false;

            NPCMonitor.ignoreUpdates = true;
            NPCMonitor.check(loc);
            NPCMonitor.ignoreUpdates = false;
        }

        private bool prevEventUp = false;

        // Mini update is done even when not the active location
        public void miniUpdate()
        {
            NPCMonitor.check(loc);
            checkBuildings();

#if false
            // Apparently this is causing A LOT of lag. Let's skip it when we can.
            if (Game1.locationAfterWarp != loc || Game1.fadeToBlackAlpha <= 0.97)
                return;

            var layer = loc.map.GetLayer("Buildings");
            NPC checkWith = Game1.getCharacterFromName("Penny");

            // This bug has something to do with GameLocation.openDoor (I think).
            // Investigate actual cause later, instead of this messy patch
            /**/Tile firstValid = null;
            Tile firstNonsolid = null;
            Vector2 nonSolidPos = new Vector2();
            for (int ix = 0; ix < layer.LayerWidth; ++ix)
            {
                for (int iy = 0; iy < layer.LayerHeight; ++iy)
                {
                    Tile tmp = layer.Tiles[ix, iy];

                    if (tmp != null)
                    {
                        if (firstValid != null)
                            firstValid = tmp;

                        Rectangle rect = checkWith.GetBoundingBox();
                        rect = new Rectangle(ix * Game1.tileSize + Game1.tileSize / 8, iy * Game1.tileSize + Game1.tileSize / 8, rect.Width, rect.Height);
                        if ( tmp.TileIndex != 0 && !loc.isCollidingPosition( rect, Game1.viewport, checkWith ) )
                        {
                            Log.Async("Found non-solid pos: " + ix + ", " + iy);
                            firstNonsolid = tmp;
                            nonSolidPos = new Vector2(ix, iy);
                            ix = layer.LayerWidth;
                            break;
                        }
                    }
                }
            }

            Tile tileFix = firstValid;
            if (firstNonsolid != null)
                tileFix = firstNonsolid;//*/

            xTile.ObjectModel.PropertyValue propertyValue7 = null;
            loc.map.Properties.TryGetValue("Doors", out propertyValue7);
            if (propertyValue7 != null)
            {
                string[] array9 = propertyValue7.ToString().Split(new char[]
				                {
					                ' '
				                });
                for (int num = 0; num < array9.Length; num += 4)
                {
                    int x = Convert.ToInt32(array9[num]);
                    int y = Convert.ToInt32(array9[num + 1]);
                    if (!loc.doorSprites.ContainsKey(new Point(x, y)))
                    {
                        Log.Async("Missing door sprite @ (" + x + ", " + y + ")?????");
                        Log.Async("Patching, hope stuff doesn't break.");

                        // There is also only one place where door sprites get created. So I stole most of this from there
                        int i = x, j = y;
                        if (loc.map.GetLayer("Buildings").Tiles[i, j] == null)
                        {
                            // I really hope this works
                            //*
                            loc.map.GetLayer("Buildings").Tiles[i, j] = tileFix.Clone(layer);
                            if (firstNonsolid != null)
                                loc.map.GetLayer("Back").Tiles[ i, j] = loc.map.GetLayer("Back").Tiles[(int)nonSolidPos.X, (int)nonSolidPos.Y].Clone( loc.map.GetLayer("Back") );
                            //*/
                            //loc.map.GetLayer("Buildings").Tiles[ i, j ] = new StaticTile()
                        }
                        int tileIndex = loc.map.GetLayer("Buildings").Tiles[i, j].TileIndex;
                        Microsoft.Xna.Framework.Rectangle sourceRect = default(Microsoft.Xna.Framework.Rectangle);
                        bool flipped = false;
                        int num_ = tileIndex; // Added underscore because conflicts with iteration above
                        if (num_ != 120)
                        {
                            switch (num_)
                            {
                                case 824:
                                    sourceRect = new Microsoft.Xna.Framework.Rectangle(640, 144, 16, 48);
                                    break;
                                case 825:
                                    sourceRect = new Microsoft.Xna.Framework.Rectangle(640, 144, 16, 48);
                                    flipped = true;
                                    break;
                                default:
                                    if (num_ == 838)
                                    {
                                        sourceRect = new Microsoft.Xna.Framework.Rectangle(576, 144, 16, 48);
                                        if (i == 10 && j == 5)
                                        {
                                            flipped = true;
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            sourceRect = new Microsoft.Xna.Framework.Rectangle(512, 144, 16, 48);
                        }
                        loc.doorSprites.Add(new Point(i, j), new TemporaryAnimatedSprite(Game1.mouseCursors, sourceRect, 100f, 4, 1, new Vector2((float)i, (float)(j - 2)) * (float)Game1.tileSize, false, flipped, (float)((j + 1) * Game1.tileSize - Game1.pixelZoom * 3) / 10000f, 0f, Color.White, (float)Game1.pixelZoom, 0f, 0f, 0f, false)
                        {
                            holdLastFrame = true,
                            paused = true,
                            endSound = /*(propertyValue.ToString().Split(new char[]
										    {
											    ' '
										    }).Count<string>() > 1) ? propertyValue.ToString().Substring(propertyValue.ToString().IndexOf(' ') + 1) :*/ null
                        });
                    }
                }
            }
#endif

        }

        public void update()
        {
            checkShippingBin();
            checkDebris();
            foreach (KeyValuePair<Type, Monitor> monitor in monitors)
            {
                monitor.Value.check();
            }
            checkLocationSpecificStuff();
        }

        public int prevBinSize = 0;
        private void checkShippingBin()
        {
            if ( !( loc is Farm ) ) return;
            Farm farm = loc as Farm;

            if ( prevBinSize < farm.shippingBin.Count )
            {
                for ( int i = prevBinSize; i < farm.shippingBin.Count; ++i )
                {
                    Multiplayer.sendFunc(new ShippingBinPacket(loc, Util.serialize<Item>(farm.shippingBin[ i ]) ));
                }
            }
            else if ( prevBinSize > farm.shippingBin.Count )
            {
                for (int i = farm.shippingBin.Count; i < prevBinSize; ++i)
                {
                    Multiplayer.sendFunc(new ShippingBinPacket(loc));
                }
            }
            prevBinSize = farm.shippingBin.Count;
        }

        public void addDebris( Debris deb )
        {
            loc.debris.Add(deb);
            trackedDebris.Add(deb);
        }

        public void destroyDebris( int id )
        {
            foreach ( Debris deb in trackedDebris )
            {
                if ( deb.uniqueID == id )
                {
                    loc.debris.Remove(deb);
                    trackedDebris.Remove(deb);
                    return;
                }
            }
        }

        private void checkDebris()
        {
            List<Debris> missing = trackedDebris.ToList<Debris>();
            foreach (Debris deb in loc.debris)
            {
                if (deb.debrisType == Debris.DebrisType.ARCHAEOLOGY || deb.debrisType == Debris.DebrisType.OBJECT || deb.debrisType == Debris.DebrisType.RESOURCE || deb.debrisType == Debris.DebrisType.CHUNKS)
                {
                    if (!trackedDebris.Contains(deb))
                    {
                        Multiplayer.sendFunc(new DebrisPacket(loc, deb, true));
                        trackedDebris.Add(deb);
                    }
                    else
                    {
                        missing.Remove(deb);
                    }
                }
            }

            // Remove debris that isn't in the world anymore. We collected it, I guess
            // If an item is no longer attached to some debris (item == null, never removed from missing),
            // it will be removed as well
            foreach (Debris deb in missing)
            {
                Multiplayer.sendFunc(new DebrisPacket(loc, deb, false));
                trackedDebris.Remove(deb);
            }
        }

        public void addBuilding(Building building)
        {
            BuildableGameLocation buildLoc = loc as BuildableGameLocation;
            if ( buildLoc == null ) return;

            buildLoc.buildings.Add(building);
            trackedBuildings.Add( building, new BuildingState( building ) );
        }

        public void destroyBuilding(string id)
        {
            BuildableGameLocation buildLoc = loc as BuildableGameLocation;
            if ( buildLoc == null ) return;

            foreach (KeyValuePair< Building, BuildingState > b in trackedBuildings)
            {
                if (b.Key.nameOfIndoors == id)
                {
                    buildLoc.buildings.Remove(b.Key);
                    trackedBuildings.Remove(b.Key);

                    if (Game1.currentLocation == buildLoc)
                    {
                        Game1.flashAlpha = 1f;
                        b.Key.showDestroyedAnimation(buildLoc);
                        Game1.playSound("explosion");
                    }
                    return;
                }
            }
        }

        public void updateBuilding( string id, BuildingState state )
        {
            BuildableGameLocation buildLoc = loc as BuildableGameLocation;
            if (buildLoc == null) return;

            foreach (Building b in buildLoc.buildings)
            {
                if ( b.nameOfIndoors == id )
                {
                    if ( state.x != b.tileX || state.y != b.tileY )
                    {
                        buildLoc.buildings.Remove(b);
                        buildLoc.buildStructure(b, new Vector2(state.x, state.y), false, null);
                    }

                    b.animalDoorOpen = state.door;
                    if (b.GetType() == typeof(Barn))
                        Util.SetInstanceField(typeof(Barn), b, "animalDoorMotion", (b.animalDoorOpen ? -3 : 2));
                    else if (b.GetType() == typeof(Coop))
                        Util.SetInstanceField(typeof(Coop), b, "animalDoorMotion", (b.animalDoorOpen ? -2 : 2));

                    if ( state.upgrade > b.daysUntilUpgrade )
                    {
                        b.daysUntilUpgrade = state.upgrade;
                        Game1.playSound("axe");
                        b.showUpgradeAnimation(buildLoc);
                    }

                    break;
                }
            }
        }

        private void checkBuildings()
        {
            if (!(loc is BuildableGameLocation)) return;
            BuildableGameLocation buildLoc = loc as BuildableGameLocation;

            Dictionary<Building, BuildingState> missing = new Dictionary<Building, BuildingState>(trackedBuildings);
            foreach (Building b in buildLoc.buildings)
            {
                if (!trackedBuildings.ContainsKey(b))
                {
                    if ( !ignoreUpdates )
                        Multiplayer.sendFunc(new BuildingPacket(loc, b));
                    trackedBuildings.Add(b, new BuildingState( b ));
                }
                else
                {
                    missing.Remove(b);
                }
            }
            
            Dictionary<Building, BuildingState> tmp = new Dictionary<Building, BuildingState>(trackedBuildings);
            foreach (KeyValuePair< Building, BuildingState > b in tmp )
            {
                BuildingState state = new BuildingState( b.Key );
                if ( state.isDifferentEnoughFromOldStateToSend( b.Value ) )
                {
                    if ( !ignoreUpdates )
                        Multiplayer.sendFunc( new BuildingUpdatePacket( loc, b.Key ) );
                    trackedBuildings[ b.Key ] = state;
                }
            }

            foreach (KeyValuePair< Building, BuildingState > b in missing)
            {
                if ( !ignoreUpdates )
                    Multiplayer.sendFunc(new BuildingPacket(loc, b.Key.nameOfIndoors));
                trackedBuildings.Remove(b.Key);
            }
        }

        public void addTerrainFeature( Vector2 pos, TerrainFeature tf )
        {
            ignoreUpdates = true;

            if (loc.terrainFeatures.ContainsKey(pos))
                loc.terrainFeatures[pos] = tf;
            else
                loc.terrainFeatures.Add(pos, tf);

            Type type = tf.GetType();
            if (monitors.ContainsKey(type))
            {
                monitors[ type ].addCache(pos, tf);
            }

            ignoreUpdates = false;
        }

        public void destroyTerrainFeature( Vector2 pos )
        {
            ignoreUpdates = true;

            foreach ( KeyValuePair< Type, Monitor > monitor in monitors )
            {
                monitor.Value.removeCache(pos);
            }
            if ( loc.terrainFeatures.ContainsKey( pos ) )
                loc.terrainFeatures.Remove(pos);

            ignoreUpdates = false;
        }

        public void terrainFeaturesChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (ignoreUpdates || Game1.newDay) return;

            try
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                {
                    foreach (Vector2 key in args.NewItems)
                    {
                        TerrainFeature tf = loc.terrainFeatures[key];
                        Type type = tf.GetType();
                        if (monitors.ContainsKey(type))
                        {
                            monitors[type].addCache(key, tf);
                        }
                        Multiplayer.sendFunc(new TerrainFeaturePacket(loc, loc.terrainFeatures[key]));
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove && args.OldItems != null)
                {
                    foreach (Vector2 key in args.OldItems)
                    {
                        foreach (KeyValuePair<Type, Monitor> monitor in monitors)
                        {
                            monitor.Value.removeCache(key);
                        }
                        Multiplayer.sendFunc(new TerrainFeaturePacket(loc, key));
                    }
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception changing (" + args.Action + ", " + args.NewItems + ", " + args.OldItems + ") terrain feature: " + e);
            }
        }

        public void addObject(Vector2 pos, Object obj)
        {
            ignoreUpdates = true;

            if (loc.objects.ContainsKey(pos))
                loc.objects[pos] = obj;
            else
                loc.objects.Add(pos, obj);

            Type type = obj.GetType();
            if (monitors.ContainsKey(type))
            {
                monitors[type].addCache(pos, obj);
            }

            ignoreUpdates = false;
        }

        public void destroyObject(Vector2 pos)
        {
            ignoreUpdates = true;

            foreach (KeyValuePair<Type, Monitor> monitor in monitors)
            {
                monitor.Value.removeCache(pos);
            }
            if ( loc.objects.ContainsKey( pos ) )
                loc.objects.Remove(pos);

            ignoreUpdates = false;
        }

        public void objectsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if ( ignoreUpdates || Game1.newDay ) return;

            try
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                {
                    foreach (Vector2 key in args.NewItems)
                    {
                        Object obj = loc.objects[key];
                        Type type = obj.GetType();
                        if (monitors.ContainsKey(type))
                        {
                            monitors[type].addCache(key, obj);
                        }
                        Multiplayer.sendFunc(new ObjectPacket(loc, loc.objects[key]));
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove && args.OldItems != null)
                {
                    foreach (Vector2 key in args.OldItems)
                    {
                        foreach (KeyValuePair<Type, Monitor> monitor in monitors)
                        {
                            monitor.Value.removeCache(key);
                        }
                        Multiplayer.sendFunc(new ObjectPacket(loc, key));
                    }
                }
            }
            catch (Exception e)
            {
                Log.error("Exception changing (" + args.Action + ", " + args.NewItems + ", " + args.OldItems + ") terrain feature: " + e);
            }
        }

        public Monitor getMonitor< TYPE >() where TYPE : class
        {
            Type type = typeof( TYPE );
            if ( !monitors.ContainsKey( type ) )
            {
                return null;
            }

            return monitors[type];
        }

        public int prevFarmHay = 0;
        public bool prevBridgeFixed = true;
        private Dictionary<Vector2, int> prevMuseumPieces = null;
        private bool[] prevCompleted = null;
        private Dictionary<int, bool> prevRewards = null;
        private Dictionary<int, bool[]> prevBundles = null;
        public List<int> prevClumps = null;
        public ResourceClump prevForestLog = null;
        private void checkLocationSpecificStuff()
        {
            if ( loc is Farm || loc is Woods )
            {
                List<ResourceClump> clumps = null;
                if (loc is Farm) clumps = (loc as Farm).resourceClumps;
                else if (loc is Woods) clumps = (loc as Woods).stumps;

                if (prevClumps != null && clumps.Count < prevClumps.Count)
                {
                    if (!ignoreUpdates)
                    {
                        List<int> missingClumps = prevClumps.FindAll((prevClump) => clumps.Find((clump) => ResourceClumpsPacket.hashVec2(clump) == prevClump) == null);
                        foreach (var hash in missingClumps)
                        {
                            Multiplayer.sendFunc(new ResourceClumpsPacket(loc, hash));
                        }
                    }
                }
                updateClumpsCache(clumps);
            }

            if ( loc is Farm )
            {
                Farm farm = loc as Farm;
                if ( farm.piecesOfHay != prevFarmHay )
                {
                    Multiplayer.sendFunc(new FarmUpdatePacket(farm));
                }
                prevFarmHay = farm.piecesOfHay;
            }
            else if ( loc is Beach )
            {
                Beach beach = loc as Beach;
                if ( beach.bridgeFixed && !prevBridgeFixed )
                {
                    if ( !ignoreUpdates )
                        Multiplayer.sendFunc(new BeachBridgeFixedPacket( loc ));
                }
                prevBridgeFixed = beach.bridgeFixed;
            }
            else if ( loc is LibraryMuseum )
            {
                LibraryMuseum lib = loc as LibraryMuseum;
                if ( prevMuseumPieces == null )
                {
                    updateMuseumCache();
                    return;
                }

                if ( !Util.AreEqual( prevMuseumPieces, lib.museumPieces ) )
                {
                    if ( !ignoreUpdates )
                        Multiplayer.sendFunc(new MuseumUpdatedPacket(loc));
                    updateMuseumCache();
                }
            }
            else if ( loc is CommunityCenter )
            {
                CommunityCenter center = loc as CommunityCenter;
                if (prevCompleted == null)
                {
                    updateCommunityCenterCache();
                    return;
                }

                if ( !ignoreUpdates )
                {
                    if (!Util.AreEqual( prevBundles, center.bundles, Enumerable.SequenceEqual< bool > ) )
                    {
                        Multiplayer.sendFunc(new CommunityCenterUpdatedPacket(loc, center.bundles));
                    }
                    if ( !Enumerable.SequenceEqual( prevCompleted, center.areasComplete ) )
                    {
                        Multiplayer.sendFunc(new CommunityCenterUpdatedPacket(loc, center.areasComplete));
                    }
                    if (!Util.AreEqual(prevRewards, center.bundleRewards))
                    {
                        Multiplayer.sendFunc(new CommunityCenterUpdatedPacket(loc, center.bundleRewards));
                    }
                }
                updateCommunityCenterCache();
            }
            else if (loc is Forest)
            {
                Forest forest = loc as Forest;
                if (forest.log == null && prevForestLog != null && !ignoreUpdates)
                    Multiplayer.sendFunc(new ResourceClumpsPacket(forest, prevForestLog));
                prevForestLog = forest.log;
            }
        }

        public void updateMuseumCache()
        {
            LibraryMuseum lib = loc as LibraryMuseum;
            if (lib == null) return;

            prevMuseumPieces = new Dictionary<Vector2, int>();
            foreach (KeyValuePair<Vector2, int> piece in lib.museumPieces)
            {
                prevMuseumPieces.Add(piece.Key, piece.Value);
            }
        }

        public void updateCommunityCenterCache()
        {
            CommunityCenter center = loc as CommunityCenter;
            if (center == null) return;

            prevCompleted = (bool[])center.areasComplete.Clone();
            prevRewards = new Dictionary<int, bool>();
            foreach (KeyValuePair<int, bool> piece in center.bundleRewards)
            {
                prevRewards.Add(piece.Key, piece.Value);
            }
            prevBundles = new Dictionary<int, bool[]>();
            foreach (KeyValuePair<int, bool[]> piece in center.bundles)
            {
                prevBundles.Add(piece.Key, (bool[]) piece.Value.Clone());
            }
        }

        public void updateClumpsCache( List< ResourceClump > clumps )
        {
            prevClumps = new List< int >();
            foreach ( var clump in clumps )
                prevClumps.Add(ResourceClumpsPacket.hashVec2(clump));
        }
    }
}
