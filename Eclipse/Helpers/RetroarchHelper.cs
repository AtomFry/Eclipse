using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Helpers
{
    class RetroarchHelper
    {
        // used for finding retroarch bezels
        // converts LaunchBox default platform names (i.e. Nintendo Entertainment System) to Retroarch default platform names (i.e. NES)
        // todo: read this from a config file so we can change it without needing to recompile
        public static Dictionary<string, string> RetroarchPlatformLookup = new Dictionary<string, string>()
        {
            { "Atari Jaguar", "AtariJaguar" },
            { "NEC TurboGrafx-16", "TG16" },
            { "NEC TurboGrafx-CD", "TG-CD" },
            { "Nintendo 64", "N64" },
            { "Nintendo Entertainment System", "NES" },
            { "Nintendo Game Boy Advance", "GBA" },
            { "Nintendo Game Boy Color", "GBC" },
            { "Sega CD", "SegaCD" },
            { "Sega Genesis", "Megadrive" },
            { "Sega Saturn", "Saturn" },
            { "Sony Playstation", "PSX" },
            { "Super Nintendo Entertainment System", "SNES" }
        };
    }
}
