using StardewValley.Objects;

namespace StardewValleyMP.States
{
    // Are these even in the game? There isn't really any reference to them, and I don't remember
    // anything you use a key to unlock. I certainly don't see any in my save file.
    public class DoorState : State
    {
        public int motion;
        public bool locked;

        public DoorState(Door door)
        {
            motion = door.doorMotion;
            locked = door.locked;
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            DoorState state = obj as DoorState;
            if (state == null) return false;

            if ( motion != state.motion ) return true;
            if ( locked != state.locked ) return true;

            return false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + motion + " " + locked;
        }
    }
}
