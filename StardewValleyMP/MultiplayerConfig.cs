using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyMP
{
    public class MultiplayerConfig : Config
    {
        public string DefaultIP { get; set; }
        public string DefaultPort { get; set; }

        public override T GenerateDefaultConfig<T>()
        {
            DefaultIP = "127.0.0.1";
            DefaultPort = Multiplayer.DEFAULT_PORT;
            return this as T;
        }
    }
}
