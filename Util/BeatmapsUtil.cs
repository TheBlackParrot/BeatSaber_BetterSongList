namespace BetterSongList.Util {
	static class BeatmapsUtil {

		public static string GetHashOfLevel(BeatmapLevel level) {
			return level == null ? null : GetHashOfLevelId(level.levelID);
		}
		
		public static string GetHashOfBeatmapKey(BeatmapKey key) {
			return GetHashOfLevelId(key.levelId);
		}
		
		private static string GetHashOfLevelId(string id) {
			if(id.Length < 53)
				return null;

			// ReSharper disable once ConvertIfStatementToReturnStatement
			if(id[12] != '_') // custom_level_<hash, 40 chars>
				return null;

			return id.Substring(13, 40);
		}

		public static int GetCharacteristicFromDifficulty(BeatmapKey diff) {
			var d = diff.beatmapCharacteristic?.sortingOrder;

			if(d == null || d > 4)
				return 0;

			d = d switch
			{
				// 360 and 90 are "flipped" as far as the enum goes
				3 => 4,
				4 => 3,
				_ => d
			};

			return (int)d + 1;
		}

		public static string ConcatMappers(string[] allmappers)
		{
			return allmappers.Length == 1 ? allmappers[0] : string.Join(" ", allmappers);
		}
	}
}
