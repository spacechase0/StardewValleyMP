using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.IO;
using System.Xml.Serialization;

namespace StardewValleyMP.Packets
{
    // Client <-> Server
    // Someone shipped something in their farm.
    public class ShippingBinPacket : Packet
    {
        public string location;
        public bool add;
        public string xml;

        public ShippingBinPacket()
            : base(ID.ShippingBin)
        {
        }

        public ShippingBinPacket( GameLocation loc, string theXml ) : this()
        {
            location = loc.name;
            add = true;
            xml = theXml;
        }

        public ShippingBinPacket(GameLocation loc)
            : this()
        {
            location = loc.name;
            add = false;
            xml = "";
        }

        protected override void read(BinaryReader reader)
        {
            location = reader.ReadString();
            add = reader.ReadBoolean();
            xml = reader.ReadString();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(location);
            writer.Write(add);
            writer.Write(xml);
        }

        public override void process(Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(null, location);

            process();
        }

        public override void process(Server server, Server.Client client)
        {
            location = Multiplayer.processLocationNameForPlayerUnique(client.farmer, location);

            process();

            server.broadcast(this, client.id);
        }
        
        private void process()
        {
            GameLocation loc = Multiplayer.locations[location].loc;
            if (!(loc is Farm)) return;
            Farm farm = loc as Farm;

            if ( add )
            {
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
                if (xml != "")
                {
                    try
                    {
                        foreach (Type itemType in itemTypes)
                        {
                            if (xml.IndexOf("<" + itemType.Name) != -1)
                            {
                                item = (Item)new XmlSerializer(itemType).Deserialize(Util.stringStream(xml));
                                break;
                            }
                        }
                        if (item == null)
                        {
                            int begin = xml.IndexOf("<");
                            string type = xml.Substring(begin + 1, xml.IndexOf(" xmlns") - begin - 1);
                            Log.warn("!!! An unknown item type (" + type + ") was shipped and received by us.");
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.error("Exception deserializing item: " + e);
                    }
                }
                if (item == null) return;

                farm.shippingBin.Add( item );
                farm.lastItemShipped = item;
            }
            else if ( !add && farm.shippingBin.Count > 0 && farm.lastItemShipped != null )
            {
                farm.shippingBin.Remove(farm.lastItemShipped);
                farm.lastItemShipped = null;
            }
            Multiplayer.locations[location].prevBinSize = farm.shippingBin.Count;
        }

        public override string ToString()
        {
            return base.ToString() + " " + location + " " + add + " size:" + (xml == null ? 0 : xml.Length);
        }
    }
}
