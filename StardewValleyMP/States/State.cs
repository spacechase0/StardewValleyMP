using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace StardewValleyMP.States
{
    public abstract class State
    {
        // Always call the newer one first
        public abstract bool isDifferentEnoughFromOldStateToSend(State obj);
    }
}
