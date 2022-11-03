using Eclipse.Models;
using Eclipse.View.EclipseSettings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins;

namespace Eclipse.Plugins
{
    class EclipseSettingsMenuItem : ISystemMenuItemPlugin
    {
        public string Caption => "Manage eclipse settings";

        // todo: point to an icon image
        public Image IconImage => null;
        // public Image IconImage => Properties.Resources.EclipseSettingsIcon;

        public bool ShowInLaunchBox => true;

        public bool ShowInBigBox => false;

        public bool AllowInBigBoxWhenLocked => false;

        public void OnSelected()
        {
            // todo: show eclipse settings window 
            EclipseSettingsView eclipseSettingsView = new EclipseSettingsView();
            eclipseSettingsView.Show();
        }
    }
}
