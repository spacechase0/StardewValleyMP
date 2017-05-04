using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Buildings;

namespace StardewValleyMP.States
{
    public class BuildingState : State
    {
        public int x = -1, y = -1;
        public bool door = false;
        public int upgrade = -1;

        public BuildingState()
        {
        }

        public BuildingState(Building b)
        {
            x = b.tileX;
            y = b.tileY;
            door = b.animalDoorOpen;
            upgrade = b.daysUntilUpgrade;
        }

        public override bool isDifferentEnoughFromOldStateToSend(State obj)
        {
            BuildingState state = obj as BuildingState;
            if (state == null) return false;

            if (x != state.x) return true;
            if (y != state.y) return true;
            if (door != state.door) return true;
            if (upgrade > state.upgrade) return true;

            return false;
        }

        public override string ToString()
        {
            return base.ToString() + " " + x + " " + y + " " + door + " " + upgrade;
        }
    }
}
