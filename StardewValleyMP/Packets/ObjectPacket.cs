using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.Objects;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using System.Xml;
using System.Xml.Serialization;
using Object = StardewValley.Object;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Something happened to an object.
    public class ObjectPacket : Packet
    {
        public bool create;
        public string location;
        public float posX, posY;

        // Create only
        string objectStr;

        public ObjectPacket()
            : base(ID.Object)
        {
        }

        public ObjectPacket(GameLocation loc, Object obj)
            : this()
        {
            create = true;
            location = Multiplayer.getUniqueLocationName(loc);
            Vector2 pos = loc.objects.First(pair => pair.Value == obj).Key;
            posX = pos.X;
            posY = pos.Y;
            objectStr = Util.serialize<Object>(obj);
        }

        public ObjectPacket(GameLocation loc, Vector2 pos)
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
                objectStr = reader.ReadString();
            }
        }

        protected override void write(BinaryWriter writer)
        {
            //Log.Async("Sending object packet " + Game1.newDay + " " + location);
            writer.Write(create);
            writer.Write(location);
            writer.Write(posX);
            writer.Write(posY);
            
            if ( create )
            {
                writer.Write(objectStr);
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
            if ( !Multiplayer.locations.ContainsKey( location ) )
                return;

            if ( !create )
            {
                Multiplayer.locations[location].destroyObject(new Vector2(posX, posY));
                return;
            }

            // See whining at DebrisPacket.process
            Type[] objectTypes = new Type[]
            {
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
                typeof( SpecialItem ),
                typeof( TV ),
                typeof( Cask ),
                typeof( Boots ),
                typeof( Hat ),
                typeof( Ring ),
                typeof( Object )
            };

            Object obj = null;
            try
            {
                foreach (Type type in objectTypes)
                {
                    if (objectStr.IndexOf("<" + type.Name) != -1)
                    {
                        obj = (Object)new XmlSerializer(type).Deserialize(Util.stringStream(objectStr));
                        break;
                    }
                }
                if (obj == null)
                {
                    int begin = objectStr.IndexOf("<");
                    string type = objectStr.Substring(begin + 1, objectStr.IndexOf(" xmlns") - begin - 1);
                    Log.Async("!!! An unknown object type (" + type + ") was dropped and received by us.");
                    return;
                }
            }
            catch ( Exception e )
            {
                Log.Async("Exception deserializing object: " + e);
            }

            obj.reloadSprite();
            Multiplayer.locations[location].addObject(new Vector2(posX, posY), obj);
        }
    }
}
