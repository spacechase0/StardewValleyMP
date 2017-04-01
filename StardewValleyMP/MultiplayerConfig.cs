using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyMP
{
    public class MultiplayerConfig
    {
        public string DefaultIP { get; set; } = "127.0.0.1";
        public string DefaultPort { get; set; } = Multiplayer.DEFAULT_PORT;
        public bool Debug { get; set; } = false;
    }
}
