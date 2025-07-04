using BeatSaberMarkupLanguage;
using HarmonyLib;
using HMUI;
using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace BetterSongList.HarmonyPatches.UI {
	[HarmonyPatch(typeof(LevelCollectionTableView), nameof(LevelCollectionTableView.Init), new Type[] { })]
	internal static class ScrollEnhancements {
		private static GameObject[] _buttons;
		// ReSharper disable once InconsistentNaming
		[UsedImplicitly]
		private static void Prefix(LevelCollectionTableView __instance) {
			if(!__instance._isInitialized)
				SharedCoroutineStarter.instance.StartCoroutine(DoTheFunny(__instance._tableView, __instance.transform));

			UpdateState();
		}

		public static void UpdateState() {
			_buttons?.Do(x => { if(x != null) x.SetActive(Config.Instance.ExtendSongsScrollbar); });
		}

		private static Transform BuildButton(Transform baseButton, string icon, float vOffs, float rotation, UnityAction cb) {
			var newBtn = Object.Instantiate(baseButton,
				baseButton
				.parent // ScrollBar
				.parent // LevelsTableView
				.parent // LevelCollecionViewController
			);

			// Appropriately size the button rect
			var r = (RectTransform)newBtn.transform;
			r.anchorMin = new Vector2(0.96f, 0.893f - vOffs);
			r.anchorMax = new Vector2(1, 0.953f - vOffs);

			var i = newBtn.GetComponentInChildren<ImageView>();
			if(icon?[0] == '#')
				i.SetImageAsync(icon);

			// Put the Icon in the middle of the touchable rect
			r = (RectTransform)i.transform;
			r.offsetMax = Vector2.zero;
			r.offsetMin = new Vector2(-2.5f, -2.5f);
			r.localEulerAngles = new Vector3(0, 0, rotation);


			var btn = newBtn.GetComponent<NoTransitionsButton>();
			btn.interactable = true;
			btn.onClick.AddListener(cb);

			return newBtn;
		}

		private static IEnumerator DoTheFunny(TableView table, Transform a) {
			//yield return new WaitForSeconds(2f);
			yield return new WaitForEndOfFrame();

			// Add more horizontal space to the the LevelCollecionViewController
			var r = (RectTransform)table.transform.parent.parent;
			r.sizeDelta += new Vector2(4, 0);

			// Offset the LevelsTableView to the original position
			r = (RectTransform)table.transform.parent;
			r.anchorMin += new Vector2(0.02f, 0);
			r.sizeDelta -= new Vector2(2, 0);

			// Yoink the original scrollbar button
			var buton = a.Find("ScrollBar/UpButton");

			void Scroll(float step, int direction) {
				var cells = table.dataSource.NumberOfCells();
				if(cells == 0)
					return;

				var amt = cells * step * direction;

				if(!Mathf.Approximately(step, 1))
					amt += table.GetVisibleCellsIdRange().Item1;

				table.ScrollToCellWithIdx((int)amt, TableView.ScrollPositionType.Beginning, true);
			}


			var btnUpFast = BuildButton(buton, null, 0, -90, () => Scroll(0.1f, -1));
			var btnDownFast = BuildButton(buton, null, 0.86f, 90, () => Scroll(0.1f, 1));

			_buttons = new[] {
				btnUpFast,
				BuildButton(buton, "#HeightIcon", 0.30f, 0, () => Scroll(1, 0)),
				BuildButton(buton, "#HeightIcon", 0.56f, 180, () => Scroll(1, 1)),
				btnDownFast
			}.Select(x => x.gameObject).ToArray();

			Utilities.LoadSpriteFromAssemblyAsync("BetterSongList.UI.DoubleArrowIcon.png").ContinueWith(x => {
				btnUpFast.GetComponentInChildren<ImageView>().sprite = x.Result;
				btnDownFast.GetComponentInChildren<ImageView>().sprite = x.Result;
			});
		}
	}
}