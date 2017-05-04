using StardewValley.Objects;

namespace StardewValleyMP.States
{
    public class CrabPotState : ObjectState
    {
        public bool bait;

        public CrabPotState(CrabPot pot) : base( pot )
        {
            bait = ( pot.bait != null );
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            if (base.isDifferentEnoughFromOldStateToSend(obj)) return true;

            CrabPotState state = obj as CrabPotState;
            if (state == null) return false;

            if (bait != state.bait) return true;

            return false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + bait;
        }
    }
}
