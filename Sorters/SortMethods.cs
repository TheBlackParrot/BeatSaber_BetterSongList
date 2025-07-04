﻿using System;
using System.Collections.Generic;
using System.Globalization;
using BetterSongList.Interfaces;
using BetterSongList.SortModels;
using BetterSongList.UI;
using BetterSongList.Util;
using JetBrains.Annotations;
using SongDetailsCache.Structs;

namespace BetterSongList {
	public static class SortMethods {
		public static readonly ISorter alphabeticalSongName = new ComparableFunctionSorterWithLegend(
			(songa, songb) => string.Compare(songa.songName, songb.songName, StringComparison.Ordinal),
			song => song.songName.Length > 0 ? song.songName.Substring(0, 1) : null
		);

		public static readonly ISorter bpm = new PrimitiveFunctionSorterWithLegend(
			song => song.beatsPerMinute,
			song => Math.Round(song.beatsPerMinute).ToString(CultureInfo.InvariantCulture)
		);

		[UsedImplicitly] public static readonly ISorter alphabeticalMapper = new ComparableFunctionSorterWithLegend(
			(songa, songb) => string.Compare(
				BeatmapsUtil.ConcatMappers(songa.allMappers),
				BeatmapsUtil.ConcatMappers(songb.allMappers), 
				StringComparison.Ordinal
			),
			song => {
				var authors = song.allMappers;
				return authors.Length > 0 && authors[0].Length > 0 ? authors[0].Substring(0, 1) : null;
			}
		);
		public static readonly ISorter downloadTime = new FolderDateSorter();

		internal static float? StarsProcessor(object xx) {
			var x = (Song)xx;
			if(!x.rankedStates.HasFlag(RankedStates.ScoresaberRanked))
				return null;

			float ret = 0;

			for(int i = (int)x.diffOffset; i < x.diffOffset + x.diffCount; i++) {
				var diff = SongDetailsUtil.SongDetails.Instance.difficulties[i];

				if(diff.stars == 0)
					continue;

				if(ret == 0) {
					ret = diff.stars;
					continue;
				}

				if(Config.Instance.SortAsc) {
					if(ret < diff.stars)
						continue;
				} else if(ret > diff.stars) {
					continue;
				}

				ret = diff.stars;
			}

			return ret == 0 ? (float?)null : ret;
		}

		public static readonly ISorter stars = new BasicSongDetailsSorterWithLegend(StarsProcessor, x => {
			float? y = StarsProcessor((Song)x);
			return y?.ToString("0.0");
		});

		private const float FunnyOptim = 1 / 60f;

		public static readonly ISorter songLength = new PrimitiveFunctionSorterWithLegend(
			song => song.songDuration,
			song => (song.songDuration < 60
				? "<1"
				: Math.Round(song.songDuration * FunnyOptim).ToString(CultureInfo.InvariantCulture)) + " min"
		);

		private static int GetQuarter(DateTime date) => date.Month > 9 ? 4 : date.Month > 6 ? 3 : date.Month > 3 ? 2 : 1;

		public static readonly ISorter beatSaverDate = new BasicSongDetailsSorterWithLegend(
			x => ((Song)x).uploadTimeUnix,
			x => {
			var d = ((Song)x).uploadTime;
			return d.ToString($"Q{GetQuarter(d)} yy");
		});


		internal static Dictionary<string, ISorter> methods = new Dictionary<string, ISorter> {
			{ "Song Name", alphabeticalSongName },
			{ "Download Date", downloadTime },
			{ "SS Stars", stars },
			{ "Song Length", songLength },
			{ "BPM", bpm },
			{ "BeatSaver Date", beatSaverDate },
			{ "Default", null }
		};

		public static bool Register(ITransformerPlugin sorter) {
			string name = sorter.name;

			if(name.Length > 20)
				throw new ArgumentException("The name of the Transformer cannot exceed 20 Characters");

			if(!Config.Instance.AllowPluginSortsAndFilters)
				return false;

			if(FilterUI.initialized)
				throw new ArgumentException("You must register your Transformer before the Song List UI is initialized / parsed");

			name = $"🔌{name}";

			methods.Add(name, sorter);

			return true;
		}

		public static bool RegisterPrimitiveSorter<T>(T sorter) where T : ISorterPrimitive, ITransformerPlugin => Register(sorter);
		public static bool RegisterCustomSorter<T>(T sorter) where T : ISorterCustom, ITransformerPlugin => Register(sorter);
	}
}
