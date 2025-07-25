﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using System.Reflection;
using UnityEngine;

namespace BetterSongList.Util {
	static class BSMLStuff {
		public static BSMLParserParams InitSplitView(ref BSMLParserParams pparams, GameObject targetGameObject, object host, string viewName = null) {
			if(pparams != null)
				return pparams;

			viewName ??= host.GetType().Name;

			return pparams = BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), $"BetterSongList.UI.BSML.SplitViews.{viewName}.bsml"), targetGameObject, host);
		}
	}
}
