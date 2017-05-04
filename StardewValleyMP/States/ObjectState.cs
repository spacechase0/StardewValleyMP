using Object = StardewValley.Object;

namespace StardewValleyMP.States
{
    public class ObjectState : State
    {
        public bool hasSomething;
        public int ready;

        public ObjectState(Object obj)
        {
            hasSomething = (obj.heldObject != null);
            ready = obj.minutesUntilReady;
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            ObjectState state = obj as ObjectState;
            if (state == null) return false;

            if (hasSomething != state.hasSomething) return true;
            if (ready > state.ready) return true;

            return false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + hasSomething + " " + ready;
        }
    }
}
