using Eclipse.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Models
{
    public class ResourceImages
    {
        public static Uri DefaultBezelVertical { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/DefaultBezelVertical.png");
        public static Uri DefaultBezelHorizontal { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/DefaultBezelHorizontal.png");
        public static Uri VoiceRecognitionGif { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/VoiceRecognitionGif.gif");
        public static Uri SettingsIconGrey { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/SettingsIcon_Grey.png");
        public static Uri SettingsIconWhite { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/SettingsIcon_White.png");
        public static Uri GameFrontDummy { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/NES_BoxFront_Template.png");
        public static Uri DefaultFrontImage { get; } = new Uri(DirectoryInfoHelper.Instance.DefaultBoxFrontImageFullPath);
        public static Uri DefaultBackground { get; } = new Uri(DirectoryInfoHelper.Instance.DefaultBackgroundImagePath);
        public static Uri PlayButtonSelected { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/PlayButton_Selected.png");
        public static Uri PlayButtonUnSelected { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/PlayButton_Unselected.png");
        public static Uri MoreInfoSelected { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/MoreInfo_Selected.png");
        public static Uri MoreInfoUnSelected { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/MoreInfo_Unselected.png");
        public static Uri LaunchBoxLogo { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/LaunchBoxLogo.png");
        public static Uri EclipseSettingsIcon1 { get; } = new Uri($"{DirectoryInfoHelper.ResourceFolder}/EclipseSettingsIcon1.png");
    }
}
