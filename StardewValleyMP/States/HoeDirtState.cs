using StardewValley.TerrainFeatures;

namespace StardewValleyMP.States
{
    public class HoeDirtState : State
    {
        public bool crop;
        public int cropPhase = 0, cropDayPhase = 0;
        public bool cropGrown = false;

        public int state, fertilizer;

        public HoeDirtState(HoeDirt dirt)
        {
            crop = (dirt.crop != null);
            if (crop)
            {
                cropPhase = dirt.crop.currentPhase;
                cropDayPhase = dirt.crop.dayOfCurrentPhase;
                cropGrown = dirt.crop.fullyGrown;
            }

            state = dirt.state;
            fertilizer = dirt.fertilizer;
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            HoeDirtState dirt = obj as HoeDirtState;
            if (dirt == null) return false;

            if (crop != dirt.crop) return true;
            if (crop && cropPhase != dirt.cropPhase) return true;
            if (crop && cropDayPhase != dirt.cropDayPhase) return true;
            if (crop && cropGrown != dirt.cropGrown) return true;
            if (state != dirt.state) return true;
            if (fertilizer != dirt.fertilizer) return true;

            return false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + crop + " " + cropPhase + " " + cropDayPhase + " " + cropGrown + " " + state + " " + fertilizer;
        }
    };
}
