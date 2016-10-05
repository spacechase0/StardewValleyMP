using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StardewValley;
using StardewValley.Menus;

namespace StardewValleyMP.Packets
{
    // Server <-> Client
    // Co-op, not coop :P
    public class CoopUpdatePacket : Packet
    {
        public int money, clubCoins;
        public uint moneyEarned;
        public bool rustyKey, skullKey, clubCard;

        public CoopUpdatePacket()
            : base(ID.CoopUpdate)
        {
            money = Game1.player.money;
            clubCoins = Game1.player.clubCoins;
            moneyEarned = Game1.player.totalMoneyEarned;
            rustyKey = Game1.player.hasRustyKey;
            skullKey = Game1.player.hasSkullKey;
            clubCard = Game1.player.hasClubCard;
            // Should I sync dark talisman / magic ink?
        }

        protected override void read(BinaryReader reader)
        {
            money = reader.ReadInt32();
            clubCoins = reader.ReadInt32();
            moneyEarned = reader.ReadUInt32();
            rustyKey = reader.ReadBoolean();
            skullKey = reader.ReadBoolean();
            clubCard = reader.ReadBoolean();
        }

        protected override void write(BinaryWriter writer)
        {
            writer.Write(money);
            writer.Write(clubCoins);
            writer.Write(moneyEarned);
            writer.Write(rustyKey);
            writer.Write(skullKey);
            writer.Write(clubCard);
        }

        public override void process(Client client)
        {
            process();
        }

        public override void process( Server server, Server.Client client )
        {
            process();
            server.broadcast(this, client.id);
        }

        private void process()
        {
            if (!Multiplayer.COOP) return;
            if (Multiplayer.mode == Mode.Host && Game1.activeClickableMenu is ShippingMenu) return;

            Game1.player.money = money;
            Game1.player.clubCoins = clubCoins;
            Game1.player.totalMoneyEarned = moneyEarned;
            Game1.player.hasRustyKey = rustyKey;
            Game1.player.hasSkullKey = skullKey;
            Game1.player.hasClubCard = clubCard;
            Multiplayer.prevCoopState = this;
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || obj.GetType() != typeof( CoopUpdatePacket ) )
            {
                return false;
            }
            CoopUpdatePacket other = ( CoopUpdatePacket ) obj;

            if (money != other.money) return false;
            if (clubCoins != other.clubCoins) return false;
            if (moneyEarned != other.moneyEarned) return false;
            if (rustyKey != other.rustyKey) return false;
            if (skullKey != other.skullKey) return false;
            if (clubCard != other.clubCard) return false;

            return true;
        }
    }
}
