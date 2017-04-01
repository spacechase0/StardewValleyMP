using StardewValley;
using System.Collections.Generic;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewValleyMP.Vanilla
{
    public class FurniturePlacer
    {
        public static void addAllFurnitureOwnedByFarmer()
        {
            foreach (string current in Game1.player.furnitureOwned)
            {
                FurniturePlacer.addFurniture(current);
            }
        }

        public static void addFurniture(string furnitureName)
        {
            if (furnitureName.Equals("Television"))
            {
                GameLocation locationFromName = Game1.getLocationFromName("FarmHouse");
                locationFromName.Map.GetLayer("Buildings").Tiles[6, 3] = new StaticTile(locationFromName.Map.GetLayer("Buildings"), locationFromName.Map.GetTileSheet("Farmhouse"), BlendMode.Alpha, 12);
                locationFromName.Map.GetLayer("Buildings").Tiles[6, 3].Properties.Add("Action", new PropertyValue("TV"));
                locationFromName.Map.GetLayer("Buildings").Tiles[7, 3] = new StaticTile(locationFromName.Map.GetLayer("Buildings"), locationFromName.Map.GetTileSheet("Farmhouse"), BlendMode.Alpha, 13);
                locationFromName.Map.GetLayer("Buildings").Tiles[7, 3].Properties.Add("Action", new PropertyValue("TV"));
                locationFromName.Map.GetLayer("Buildings").Tiles[6, 2] = new StaticTile(locationFromName.Map.GetLayer("Buildings"), locationFromName.Map.GetTileSheet("Farmhouse"), BlendMode.Alpha, 4);
                locationFromName.Map.GetLayer("Buildings").Tiles[7, 2] = new StaticTile(locationFromName.Map.GetLayer("Buildings"), locationFromName.Map.GetTileSheet("Farmhouse"), BlendMode.Alpha, 5);
            }
            else if (furnitureName.Equals("Incubator"))
            {
                GameLocation locationFromName2 = Game1.getLocationFromName("Coop");
                locationFromName2.map.GetLayer("Buildings").Tiles[1, 3] = new StaticTile(locationFromName2.map.GetLayer("Buildings"), locationFromName2.map.TileSheets[0], BlendMode.Alpha, 44);
                locationFromName2.map.GetLayer("Buildings").Tiles[1, 3].Properties.Add(new KeyValuePair<string, PropertyValue>("Action", new PropertyValue("Incubator")));
                locationFromName2.map.GetLayer("Front").Tiles[1, 2] = new StaticTile(locationFromName2.map.GetLayer("Front"), locationFromName2.map.TileSheets[0], BlendMode.Alpha, 45);
            }
            if (!Game1.player.furnitureOwned.Contains(furnitureName))
            {
                Game1.player.furnitureOwned.Add(furnitureName);
            }
        }
    }
}
