﻿using BetterSongList.Interfaces;
using BetterSongList.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BetterSongList.SortModels {
	public class BasicSongDetailsSorterWithLegend : ISorterWithLegend, ISorterPrimitive, IAvailabilityCheck {
		public bool isReady => SongDetailsUtil.FinishedInitAttempt;

#nullable enable
		Func<object, float?> sortValueTransformer;
		Func<object, string?> legendValueTransformer;

		public BasicSongDetailsSorterWithLegend(Func<object, float?> sortValueTransformer, Func<object, string?> legendValueTransformer) {
			this.sortValueTransformer = sortValueTransformer;
			this.legendValueTransformer = legendValueTransformer;
		}
#nullable disable
		public BasicSongDetailsSorterWithLegend(Func<object, float?> sortValueTransformer) {
			this.sortValueTransformer = sortValueTransformer;
			this.legendValueTransformer = (x) => sortValueTransformer(x).ToString();
		}

		public IEnumerable<KeyValuePair<string, int>> BuildLegend(BeatmapLevel[] levels) {
			if(SongDetailsUtil.SongDetails == null)
				return null;

			try {
				return SongListLegendBuilder.BuildFor(levels, (level) => {
					//if(!GetSongFromBeatmap(level, out var song))
					//	return null;

					var h = BeatmapsUtil.GetHashOfLevel(level);
					if(h == null || !SongDetailsUtil.SongDetails.Instance.songs.FindByHash(h, out var song))
						return "N/A";

					return legendValueTransformer(song);
				});
			} catch(Exception ex) {
				Plugin.Log.Debug("Building legend failed:");
				Plugin.Log.Debug(ex);
			}
			return null;
		}

		public float? GetValueFor(BeatmapLevel x) {
			// Make N/A always end up at the bottom in either sort direction
			if(SongDetailsUtil.SongDetails == null)
				return null;

			float? _Get(BeatmapLevel y) {
				var h = BeatmapsUtil.GetHashOfLevel(y);
				if(h == null || !SongDetailsUtil.SongDetails.Instance.songs.FindByHash(h, out var song))
					return null;

				return sortValueTransformer(song);
			}

			return _Get(x);
		}

		public string GetUnavailabilityReason() => SongDetailsUtil.GetUnavailabilityReason();

		public Task Prepare(CancellationToken cancelToken)
		{
			return !isReady ? SongDetailsUtil.TryGet() : Task.CompletedTask;
		}
	}
}
