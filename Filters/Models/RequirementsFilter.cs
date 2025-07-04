﻿using BetterSongList.Util;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterSongList.FilterModels {
	public sealed class RequirementsFilter : IFilter {
		public bool isReady => SongCore.Loader.AreSongsLoaded;

		static TaskCompletionSource<bool> wipTask = null;
		static bool inited = false;
		public Task Prepare(CancellationToken cancelToken) => Prepare(cancelToken, false);

		private static Task Prepare(CancellationToken cancelToken, bool fullReload) {
			if(wipTask?.Task.IsCompleted != false)
				wipTask = new TaskCompletionSource<bool>();

			if(!inited && (inited = true))
				SongCore.Loader.SongsLoadedEvent += (_, _2) => wipTask.SetResult(true);

			return wipTask.Task;
		}

		public bool GetValueFor(BeatmapLevel level) {
			var mid = BeatmapsUtil.GetHashOfLevel(level);

			if(mid == null)
				return false;

			return SongCore.Collections.GetCustomLevelSongData(mid)?
				._difficulties.Any(x => x.additionalDifficultyData._requirements.Any(y => y.Length != 0)) == true;
		}
	}
}
