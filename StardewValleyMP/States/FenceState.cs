using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;

namespace StardewValleyMP.States
{
    public class FenceState : State
    {
        public int pos;

        public FenceState(Fence fence)
        {
            pos = fence.gatePosition;
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            FenceState state = obj as FenceState;
            if (state == null) return false;

            if (pos != state.pos)
            {
                return true;
            }

            return false;
        }
    }
}
