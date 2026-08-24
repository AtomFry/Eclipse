using System;
using System.Runtime.InteropServices;

namespace Eclipse.Helpers
{
    [StructLayout(LayoutKind.Sequential)]
    struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }


    /// <summary>
    /// The display Eclipse is running on, measured once.
    ///
    /// This is the only place the display's size is read. Box art is pre-scaled for a particular
    /// display and cached in a folder named after it, and those two used to come from different
    /// sources: the folder name from EnumDisplaySettings, which reports the *primary* monitor,
    /// and the scaling from the monitor Big Box's own window is on. On a single display machine
    /// they agree. Anywhere else - a laptop docked to a television, Big Box opened on the second
    /// monitor - the folder was named for one display and filled with artwork sized for another,
    /// and because cache entries are never invalidated it stayed that way.
    ///
    /// The window's own monitor is the right answer, since that is where the theme is drawn.
    /// EnumDisplaySettings remains as a fallback for the case where there is no window yet.
    /// </summary>
    public sealed class DisplayInfoHelper
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        // Used when neither the window nor the display mode can be read at all. Matches the
        // defaults this replaced, and stays a 16:9 pair so the design unit comes out square.
        private const int DefaultWidth = 2560;
        private const int DefaultHeight = 1440;

        public int displayHeight { get; private set; }
        public int displayWidth { get; private set; }

        private static readonly DisplayInfoHelper instance = new DisplayInfoHelper();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static DisplayInfoHelper()
        {
        }

        private DisplayInfoHelper()
        {
            if (TryReadWindowMonitor(out int width, out int height)
                || TryReadPrimaryDisplayMode(out width, out height))
            {
                displayWidth = width;
                displayHeight = height;
                return;
            }

            LogHelper.Log($"Display size not found - defaulting to {DefaultWidth}x{DefaultHeight}");
            displayWidth = DefaultWidth;
            displayHeight = DefaultHeight;
        }

        // The monitor Big Box's window is on. Before the window exists this returns the primary
        // monitor, which is the same answer the fallback below would give.
        private static bool TryReadWindowMonitor(out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                IntPtr window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.FromHandle(window).Bounds;

                width = bounds.Width;
                height = bounds.Height;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "read the display Big Box is running on");
            }

            return width > 0 && height > 0;
        }

        private static bool TryReadPrimaryDisplayMode(out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                const int ENUM_CURRENT_SETTINGS = -1;

                DEVMODE devMode = default;
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode);

                width = devMode.dmPelsWidth;
                height = devMode.dmPelsHeight;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "read the primary display mode");
            }

            return width > 0 && height > 0;
        }

        public static DisplayInfoHelper Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
