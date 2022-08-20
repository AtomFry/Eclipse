using System.Collections.Generic;
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