using SongDetailsCache;
using System.Threading.Tasks;

namespace BetterSongList.Util {
	internal static class SongDetailsUtil {
		public class AntiBox
		{
			public readonly SongDetails Instance;

			public AntiBox(SongDetails instance)
			{
				Instance = instance;
			}
		}

		public static bool FinishedInitAttempt { get; private set; }

		private static bool CheckAvailable() {
			var v = IPA.Loader.PluginManager.GetPluginFromId("SongDetailsCache");

			if (v == null)
			{
				return false;
			}

			return v.HVersion >= new Hive.Versioning.Version("1.1.5");
		}
		public static bool IsAvailable => CheckAvailable();
		public static AntiBox SongDetails;

		public static string GetUnavailabilityReason() {
			if (!IsAvailable)
			{
				return "Your Version of 'SongDetailsCache' is either outdated, or you are missing it entirely";
			}

			if (FinishedInitAttempt && SongDetails == null)
			{
				return "SongDetailsCache failed to initialize for some reason. Try restarting your game, that might fix it";
			}

			return null;
		}

		public static async Task<AntiBox> TryGet() {
			if (FinishedInitAttempt)
			{
				return SongDetails;
			}

			try {
				if (IsAvailable)
				{
					return SongDetails = new AntiBox(await SongDetailsCache.SongDetails.Init());
				}
			}
			catch
			{
				// ignored
			}
			finally
			{
				FinishedInitAttempt = true;
			}
			
			return SongDetails;
		}
	}
}
