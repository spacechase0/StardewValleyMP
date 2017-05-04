using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.IO;
using System.Xml.Serialization;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to debris.
    public class DebrisPacket : Packet
    {
        public bool create;
        public string location;
        public int debrisId;

        // Create only
        public float posX, posY;
        public float velX, velY;
        public string itemStr;
        public int type;
        public int chunkType;
        public int quality;
        public int chunkObj;
        public int chunkCount;

        public DebrisPacket()
            : base(ID.Debris)
        {
        }

        public DebrisPacket(GameLocation loc, Debris deb, bool theCreate)
            : this()
        {
            create = theCreate;
            location = Multiplayer.getUniqueLocationName(loc); ;
            debrisId = deb.uniqueID;

            if ( create )
            {
                Chunk chunk = deb.Chunks[ 0 ];
                posX = chunk.position.X;
                posY = chunk.position.Y;
                velX = chunk.xVelocity;
                velY = chunk.yVelocity;
                itemStr = (deb.item == null) ? "" : Util.serialize<Item>(deb.item);
                type = (int)deb.debrisType;
                chunkType = deb.chunkType;
                quality = deb.itemQuality;
                chunkObj = deb.Chunks[0].debrisType;
                chunkCount = deb.Chunks.Count;
            }
        }

        protected override void read(BinaryReader reader)
        {
            create = reader.ReadBoolean();
            location = reader.ReadString();
            debrisId = reader.ReadInt32();

            if ( create )
            {
                posX = reader.ReadSingle();
                posY = reader.ReadSingle();
                velX = reader.ReadSingle();
                velY = reader.ReadSingle();
                itemStr = reader.ReadString();
                type = reader.ReadInt32();
                chunkType = reader.ReadInt32();
                quality = reader.ReadInt32();
                chunkObj = reader.ReadInt32();
                chunkCount = reader.ReadInt32();
            }
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(create);
            writer.Write(location);
            writer.Write(debrisId);
            
            if ( create )
            {
                writer.Write(posX);
                writer.Write(posY);
                writer.Write(velX);
                writer.Write(velY);
                writer.Write(itemStr);
                writer.Write(type);
                writer.Write(chunkType);
                writer.Write(quality);
                writer.Write(chunkObj);
                writer.Write(chunkCount);
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
                Multiplayer.locations[location].destroyDebris(debrisId);
                return;
            }
            //*
            // I am so tired of trying to get deserializing to work with just 'Item' for the call.
            // It complains because it doesn't expect 'Object' or 'Ring'. It wants the exact type.
            // Ugh. Whatever. I can do that.
            Type[] itemTypes = new Type[]
            {
                typeof( Item ),
                typeof( Wand ),
                typeof( Raft ),
                typeof( MagnifyingGlass ),
                typeof( Lantern ),
                typeof( Blueprints ),
                typeof( Seeds ),
                typeof( MeleeWeapon ),
                typeof( FishingRod ),
                typeof( Axe ),
                typeof( Hoe ),
                typeof( MilkPail ),
                typeof( Pan ),
                typeof( Pickaxe ),
                typeof( Shears ),
                typeof( Slingshot ),
                typeof( WateringCan ),
                typeof( Sword ),
                typeof( Stackable ),
                typeof( Torch ),
                typeof( Fence ),
                typeof( ColoredObject ),
                typeof( Furniture ),
                typeof( BreakableContainer ),
                typeof( Chest ),
                typeof( CrabPot ),
                typeof( Door ),
                typeof( SwitchFloor ),
                typeof( Wallpaper ),
                typeof( WickedStatue ),
                typeof( Boots ),
                typeof( Hat ),
                typeof( Ring ),

                typeof( StardewValley.Object ), // Keep last. Or at least after fishing rod
            };

            Item item = null;
            if (itemStr != "")
            {
                try
                {
                    foreach (Type itemType in itemTypes)
                    {
                        if (itemStr.IndexOf("<" + itemType.Name) != -1)
                        {
                            item = (Item)new XmlSerializer(itemType).Deserialize(Util.stringStream(itemStr));
                            break;
                        }
                    }
                    if (item == null)
                    {
                        int begin = itemStr.IndexOf("<");
                        string type = itemStr.Substring(begin + 1, itemStr.IndexOf(" xmlns") - begin - 1);
                        Log.warn("!!! An unknown item type (" + type + ") was dropped and received by us.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Log.error("Exception deserializing item: " + e);
                }
            }

            Vector2 pos = new Vector2(posX, posY);
            Debris deb = null;
            if (Game1.objectInformation.ContainsKey(chunkObj))
            {
                deb = new Debris(chunkObj, pos, pos);
            }
            if (item != null)
            {
                deb = new Debris(item, pos);
            }

            if (deb == null)
            {
                deb = new Debris(chunkType, chunkCount, pos, pos);
                //return; // Bah, fix this later
            }
            //else
            {
                deb.debrisType = (Debris.DebrisType)this.type;
                deb.chunkType = chunkType;
                deb.itemQuality = quality;
                deb.uniqueID = debrisId;
                foreach (Chunk chunk in deb.Chunks)
                {
                    chunk.debrisType = chunkObj;
                    // Should I set position here?
                    chunk.xVelocity = velX;
                    chunk.yVelocity = velY;
                }
            }
            
            Multiplayer.locations[location].addDebris(deb);
        }

        public override string ToString()
        {
            return base.ToString() + " " + create + " " + location + " " + debrisId + " " + ( create ? ( posX + " " + posY + " " + velX + " " + velY + " size:" + (itemStr == null ? 0 : itemStr.Length) + " " + type + " " + chunkType + " " + quality + " " + chunkObj + " " + chunkCount ) : "" );
        }
    }
}
