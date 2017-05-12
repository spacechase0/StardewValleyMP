namespace StardewValleyMP
{
    public class MultiplayerConfig
    {
        public enum PacketLogAmount
        {
            None,
            Filtered,
            All,
        }

        public string DefaultIP { get; set; } = "127.0.0.1";
        public string DefaultPort { get; set; } = Multiplayer.DEFAULT_PORT;
        public bool AllowFriends { get; set; } = true;
        public bool AllowLanDiscovery { get; set; } = true;
        public bool Debug { get; set; } = false;
        public bool Coop { get; set; } = true;
        public bool Compress { get; set; } = true;
        public PacketLogAmount PacketLogging { get; set; } = PacketLogAmount.Filtered;
    }
}
