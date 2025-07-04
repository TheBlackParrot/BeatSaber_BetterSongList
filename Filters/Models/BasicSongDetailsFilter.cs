using BetterSongList.Interfaces;
using BetterSongList.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterSongList.FilterModels {
	public class BasicSongDetailsFilter : IFilter, IAvailabilityCheck {
		public bool isReady => SongDetailsUtil.FinishedInitAttempt;

		Func<object, bool> filterValueTransformer;

		public BasicSongDetailsFilter(Func<object, bool> filterValueTransformer) {
			this.filterValueTransformer = filterValueTransformer;
		}

		public Task Prepare(CancellationToken cancelToken) {
			if(!isReady)
				return SongDetailsUtil.TryGet();

			return Task.CompletedTask;
		}

		public string GetUnavailabilityReason() => SongDetailsUtil.GetUnavailabilityReason();

		public bool GetValueFor(BeatmapLevel level) {
			if(SongDetailsUtil.SongDetails == null)
				return false;

			//if(!GetSongFromBeatmap(level, out var song))
			//	return false;

			var h = BeatmapsUtil.GetHashOfLevel(level);
			if(h == null)
				return false;

			bool wrapper() {
				if(!SongDetailsUtil.SongDetails.Instance.songs.FindByHash(h, out var song))
					return false;

				return filterValueTransformer(song);
			}
			return wrapper();
		}
	}
}
