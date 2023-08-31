using Eclipse.Models;
using System.Collections.Generic;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    public sealed class PlaylistGameService
    {
        private bool playlistsSet = false;
        private bool playlistGamesSet = false;
        private List<PlaylistGame> playlistGames;
        private Dictionary<string, bool> playlists;

        public List<PlaylistGame> PlaylistGames
        {
            get
            {
                if (!playlistGamesSet)
                {
                    playlistGames = new List<PlaylistGame>();
                    playlists = new Dictionary<string, bool>();

                    IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
                    foreach (IPlaylist playlist in allPlaylists)
                    {
                        if (playlist.HasGames(false, false))
                        {
                            if (!playlists.ContainsKey(playlist.SortTitleOrTitle))
                            {
                                playlists.Add(playlist.SortTitleOrTitle, playlist.IncludeWithPlatforms);
                            }

                            IGame[] games = playlist.GetAllGames(true);
                            foreach (IGame game in games)
                            {
                                playlistGames.Add(new PlaylistGame() { GameId = game.Id, Playlist = playlist.SortTitleOrTitle });
                            }
                        }
                    }

                    playlistsSet = true;
                    playlistGamesSet = true;
                }
                return playlistGames;
            }
        }

        public Dictionary<string, bool> Playlists
        {
            get
            {
                if (playlistsSet)
                {
                    playlists = new Dictionary<string, bool>();

                    IPlaylist[] allPlaylists = PluginHelper.DataManager.GetAllPlaylists();
                    foreach (IPlaylist playlist in allPlaylists)
                    {
                        if (playlist.HasGames(false, false))
                        {
                            if (!playlists.ContainsKey(playlist.SortTitleOrTitle))
                            {
                                playlists.Add(playlist.SortTitleOrTitle, playlist.IncludeWithPlatforms);
                            }                            
                        }
                    }

                    playlistsSet = true;
                }

                return playlists;
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