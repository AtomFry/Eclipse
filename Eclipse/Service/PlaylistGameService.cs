using Eclipse.Models;
using System.Collections.Generic;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    public sealed class PlaylistGameService
    {
        private bool playlistGamesSet = false;
        private List<PlaylistGame> playlistGames;
        public List<PlaylistGame> PlaylistGames
        {
            get
            {
                if (!playlistGamesSet)
                {
                    playlistGames = new List<PlaylistGame>();

                    IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
                    foreach (IPlaylist playlist in allPlaylists)
                    {
                        if (playlist.HasGames(false, false))
                        {
                            IGame[] games = playlist.GetAllGames(true);
                            foreach (IGame game in games)
                            {
                                playlistGames.Add(new PlaylistGame() { GameId = game.Id, Playlist = playlist.SortTitleOrTitle });
                            }
                        }
                    }

                    playlistGamesSet = true;
                }
                return playlistGames;
            }
        }

        #region singleton implementation 
        public static PlaylistGameService Instance => instance;

        private static readonly PlaylistGameService instance = new PlaylistGameService();

        static PlaylistGameService()
        {
        }

        private PlaylistGameService()
        {
        }
        #endregion
    }
}