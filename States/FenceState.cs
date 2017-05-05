using StardewValley;

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

        public override string ToString()
        {
            return base.ToString() + " " + pos;
        }
    }
}
