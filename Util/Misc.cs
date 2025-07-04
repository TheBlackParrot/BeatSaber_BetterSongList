namespace BetterSongList.Util {
	static class XD {
		public static T FunnyNull<T>(T a) where T : UnityEngine.Object
		{
			return a == null ? null : a;
		}
	}
}
