using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValleyMP.Packets;
using StardewValley;
using StardewValley.Objects;

namespace StardewValleyMP.States
{
    class ChestMonitor : SpecificMonitor<StardewValley.Object, Chest, ChestState, ChestUpdatePacket>
    {
        public ChestMonitor( LocationCache loc )
        :   base( loc, loc.loc.objects, 
                  (obj) => new ChestState(obj),
                  (loc_, pos) => new ChestUpdatePacket(loc_, pos) )
        {
        }
    }
}
