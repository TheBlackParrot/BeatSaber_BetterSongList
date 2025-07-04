using System.Collections.Generic;
using System.Linq;
using BeatSaberPlaylistsLib.Types;

namespace BetterSongList.Util
{
	public static class PlaylistsUtil
	{
		public static bool HasPlaylistLib;

		public static void Init()
		{
			HasPlaylistLib = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null;
		}

		private static Dictionary<string, BeatmapLevelPack> _packs;

		private static BeatmapLevelPack Wrapper(string packName)
		{
			if (!SongCore.Loader.AreSongsLoaded)
			{
				return null;
			}

			return BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true)
				.Select(x => x.PlaylistLevelPack)
				.FirstOrDefault(playlistLevelPack => playlistLevelPack.packName == packName);
		}

		public static BeatmapLevelPack GetPack(string packName)
		{
			if (packName == null)
			{
				return null;
			}

			_packs ??= SongCore.Loader.BeatmapLevelsModelSO._allLoadedBeatmapLevelsRepository.beatmapLevelPacks
				// There shouldnt be any duplicate name basegame playlists... But better be safe
				.GroupBy(x => x.shortPackName)
				.Select(x => x.First())
				.ToDictionary(x => x.shortPackName, x => x);

			if(_packs.TryGetValue(packName, out var p))
			{
				return p;
			}
			return HasPlaylistLib ? Wrapper(packName) : null;
		}

		public static BeatmapLevel[] GetLevelsForLevelCollection(BeatmapLevelPack levelCollection)
		{
			if (levelCollection is PlaylistLevelPack playlist)
			{
				return playlist.playlist.BeatmapLevels;
			}
			return null;
		}
	}
}
