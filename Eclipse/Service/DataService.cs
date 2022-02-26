using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    public class DataService
    {
        public static List<IGame> GetGames()
        {
            return new List<IGame>(PluginHelper.DataManager.GetAllGames());
        }
    }
}
