using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to terrain features.
    public class TerrainFeaturePacket : Packet
    {
        public bool create;
        public string location;
        public float posX, posY;

        // Create only
        string featureStr;

        public TerrainFeaturePacket()
            : base(ID.TerrainFeature)
        {
        }

        public TerrainFeaturePacket(GameLocation loc, TerrainFeature tf)
            : this()
        {
            create = true;
            location = Multiplayer.getUniqueLocationName( loc );
            Vector2 pos = loc.terrainFeatures.First(pair => pair.Value == tf).Key;
            posX = pos.X;
            posY = pos.Y;
            featureStr = Util.serialize<TerrainFeature>(tf);
        }

        public TerrainFeaturePacket(GameLocation loc, Vector2 pos)
            : this()
        {
            create = false;
            location = Multiplayer.getUniqueLocationName(loc);
            posX = pos.X;
            posY = pos.Y;
        }

        protected override void read(BinaryReader reader)
        {
            create = reader.ReadBoolean();
            location = reader.ReadString();
            posX = reader.ReadSingle();
            posY = reader.ReadSingle();

            if ( create )
            {
                featureStr = reader.ReadString();
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(create);
            writer.Write(location);
            writer.Write(posX);
            writer.Write(posY);
            
            if ( create )
            {
                writer.Write(featureStr);
            }
        }

        public override void process(Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(null, location);

            process();
        }

        public override void process( Server server, Server.Client client )
        {
            location = Multiplayer.processLocationNameForPlayerUnique(client.farmer, location);

            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            if ( !create )
            {
                //Log.Async(Game1.gameMode + " " + ((Multiplayer.server != null) ? (Multiplayer.server.playing?"true":"false") : "NOTSERVER")+" "+((Game1.currentLoader!=null)?Game1.currentLoader.Current:-1));
                //Log.Async("Want " + location+" "+Multiplayer.locations.Count);
                //foreach (KeyValuePair<string,LocationCache> cache in Multiplayer.locations) Log.Async("\tLoc: " + cache.Key + " " + cache.Value+" "+cache.Value.loc);
                if (!Multiplayer.locations.ContainsKey(location)) return;
                Multiplayer.locations[location].destroyTerrainFeature(new Vector2(posX, posY));
                return;
            }

            // See whining at DebrisPacket.process
            Type[] featureTypes = new Type[]
            {
                typeof( TerrainFeature ),
                typeof( Grass ),
                typeof( DiggableWall ),
                typeof( HoeDirt ),
                typeof( Flooring ),
                typeof( FruitTree ),
                typeof( LargeTerrainFeature ),
                typeof( Quartz ),
                typeof( ResourceClump ),
                typeof( Stalagmite ),
                typeof( Tree ),
                typeof( CosmeticPlant ),
                typeof( Bush ),
                typeof( GiantCrop ),
            };

            TerrainFeature tf = null;
            try
            {
                foreach (Type type in featureTypes)
                {
                    if (featureStr.IndexOf("<" + type.Name) != -1)
                    {
                        tf = (TerrainFeature)new XmlSerializer(type).Deserialize(Util.stringStream(featureStr));
                        break;
                    }
                }
                if (tf == null)
                {
                    int begin = featureStr.IndexOf("<");
                    string type = featureStr.Substring(begin + 1, featureStr.IndexOf(" xmlns") - begin - 1);
                    Log.warn("!!! An unknown terrain feature type (" + type + ") was created and received by us.");
                    return;
                }
            }
            catch ( Exception e )
            {
                Log.error("Exception deserializing terrain feature: " + e);
            }

            tf.loadSprite();
            Multiplayer.locations[location].addTerrainFeature(new Vector2(posX, posY), tf);
        }

        public override string ToString()
        {
            return base.ToString() + " " + create + " " + location + " " + posX + " " + posY + " size:" + (featureStr == null ? 0 : featureStr.Length);
        }
    }
}
