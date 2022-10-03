using Auxiliary.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorldEdit.Configuration
{
    public class WorldEditSettings : ISettings
    {
        [JsonPropertyName("default-undo-amount")]
        public int DefaultUndoAmount { get; set; }

        [JsonPropertyName("schematic-directory")]
        public string SchematicDirectory { get; set; } = string.Empty;

        [JsonPropertyName("magicwand-tile-limit")]
        public int MagicWandTileLimit { get; set; }
    }
}
