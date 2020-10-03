using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Models
{
    public class ResourceImages
    {
        public static Uri VoiceRecognitionGif { get; } = new Uri($"{Helpers.ResourceFolder}/VoiceRecognitionGif.gif");

        public static Uri SettingsIconGrey { get; } = new Uri($"{Helpers.ResourceFolder}/SettingsIcon_Grey.png");

        public static Uri SettingsIconWhite { get; } = new Uri($"{Helpers.ResourceFolder}/SettingsIcon_White.png");

        public static Uri GameFrontDummy { get; } = new Uri($"{Helpers.ResourceFolder}/NES_BoxFront_Template.png");
    }
}
