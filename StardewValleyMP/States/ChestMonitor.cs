using StardewValley.Objects;
using StardewValleyMP.Packets;

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
