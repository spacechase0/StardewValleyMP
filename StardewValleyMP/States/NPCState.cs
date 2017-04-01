using StardewValley;

namespace StardewValleyMP.States
{
    public class NPCState : State
    {
        public bool datingFarmer = false;
        public bool married = false;
        public string defaultMap = "";
        public float defaultX, defaultY;

        public NPCState()
        {
        }

        public NPCState(NPC npc)
        {
            datingFarmer = npc.datingFarmer;
            married = npc.isMarried();
            defaultMap = npc.defaultMap;
            defaultX = npc.DefaultPosition.X;
            defaultY = npc.DefaultPosition.Y;

            if (defaultMap == null) defaultMap = "";
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            NPCState state = obj as NPCState;
            if (obj == null) return false;

            // Why in the world did this happen?
            // Let's ignore it and hope it goes away? :P
            if (defaultMap == null) return false;

            if (datingFarmer != state.datingFarmer) return true;
            if (married != state.married) return true;
            if (defaultMap != state.defaultMap) return true;
            if (defaultX != state.defaultX) return true;
            if (defaultY != state.defaultY) return true;

            return false;
        }
    }
}
