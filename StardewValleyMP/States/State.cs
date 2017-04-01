namespace StardewValleyMP.States
{
    public abstract class State
    {
        // Always call the newer one first
        public abstract bool isDifferentEnoughFromOldStateToSend(State obj);
    }
}
