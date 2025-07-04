using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BetterSongList.FilterModels;
using BetterSongList.HarmonyPatches;
using BetterSongList.Interfaces;
using BetterSongList.SortModels;
using BetterSongList.Util;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BetterSongList;
using TMPro;
using UnityEngine;

namespace BetterSongList.UI {
#if DEBUG
	public
#endif
	class FilterUI {
		internal static bool initialized { get; private set; }

		internal static readonly FilterUI persistentNuts = new FilterUI();
#pragma warning disable 649
		[UIComponent("root")] private readonly RectTransform rootTransform;
#pragma warning restore
		[UIParams] readonly BSMLParserParams parserParams = null;

		FilterUI() { }

		[UIComponent("sortDropdown")] readonly DropdownWithTableView _sortDropdown = null;
		[UIComponent("filterDropdown")] readonly DropdownWithTableView _filterDropdown = null;


		static Dictionary<string, ISorter> sortOptions = null;
		static Dictionary<string, IFilter> filterOptions = null;

		// ReSharper disable FieldCanBeMadeReadOnly.Local
		[UIValue("_sortOptions")] static List<object> _sortOptions = new List<object>();
		[UIValue("_filterOptions")] static List<object> _filterOptions = new List<object>();
		// ReSharper restore FieldCanBeMadeReadOnly.Local

		static void UpdateVisibleTransformers() {
			static bool CheckIsVisible(ITransformerPlugin plugin) {
				plugin.ContextSwitch(HookSelectedCategory.lastSelectedCategory, HookSelectedCollection.lastSelectedCollection);
				return plugin.visible;
			}

			sortOptions = SortMethods.methods
				.Where(x => !(x.Value is ITransformerPlugin plugin) || CheckIsVisible(plugin))
				.OrderBy(x => (x.Value is ITransformerPlugin) ? 0 : 1).ToDictionary(x => x.Key, x => x.Value);

			_sortOptions.Clear();
			_sortOptions.AddRange(sortOptions.Select(x => x.Key));

			// ReSharper disable SuspiciousTypeConversion.Global
			filterOptions = FilterMethods.methods
				.Where(x => !(x.Value is ITransformerPlugin plugin) || CheckIsVisible(plugin))
				.OrderBy(x => (x.Value is ITransformerPlugin) ? 0 : 1)
				.ToDictionary(x => x.Key, x => x.Value);
			// ReSharper restore SuspiciousTypeConversion.Global

			_filterOptions.Clear();
			_filterOptions.AddRange(filterOptions.Select(x => x.Key));
		}

		private void UpdateDropdowns() {
			if(_sortDropdown != null)
				HackDropdown(_sortDropdown);

			if(_filterDropdown != null)
				HackDropdown(_filterDropdown);
		}

		public void UpdateTransformerOptionsAndDropdowns() {
			UpdateVisibleTransformers();
			UpdateDropdowns();
		}

		public void InitializeSortsAndFilters() {
			initialized = true;

			T BLA<T>(string x, Dictionary<string, T> y) where T : class
			{
				return y.TryGetValue(x, out var z) ? z : null;
			}

			_sortDropdown.selectedIndex = Math.Max(0, _sortOptions.IndexOf(BLA(Config.Instance.LastSort, sortOptions)));
			_filterDropdown.selectedIndex = Math.Max(0, _filterOptions.IndexOf(BLA(Config.Instance.LastFilter, filterOptions)));
		}


		void _SetSort(string selected) => SetSort(selected);
		internal static void SetSort(string selected, bool storeToConfig = true, bool refresh = true) {
#if DEBUG
			Plugin.Log.Warn(string.Format("Trying to set Sort to {0}", selected));
#endif
			if(selected == null || !sortOptions.ContainsKey(selected))
				selected = sortOptions.Keys.Last();

			var newSort = sortOptions[selected];
			var unavReason = (newSort as IAvailabilityCheck)?.GetUnavailabilityReason();

			if(unavReason != null) {
				persistentNuts?.ShowErrorASAP($"Can't sort by {selected} - {unavReason}");
				// ReSharper disable once TailRecursiveCall
				SetSort(null, false, false);
				return;
			}

#if DEBUG
			Plugin.Log.Warn(string.Format("Setting Sort to {0}", selected));
#endif
			if(HookLevelCollectionTableSet.sorter != newSort) {
				if(storeToConfig)
					Config.Instance.LastSort = selected;

				HookLevelCollectionTableSet.sorter = newSort;
				RestoreTableScroll.ResetScroll();
				if(refresh)
					HookLevelCollectionTableSet.Refresh(true);
			}

			XD.FunnyNull(persistentNuts._sortDropdown)?.SelectCellWithIdx(_sortOptions.IndexOf(selected));
		}

		public static void ClearFilter(bool reloadTable = false) => SetFilter(null, false, reloadTable);
		void _SetFilter(string selected) => SetFilter(selected);
		internal static void SetFilter(string selected, bool storeToConfig = true, bool refresh = true) {
#if DEBUG
			Plugin.Log.Warn(string.Format("Trying to set Filter to {0} (store: {1}, refresh: {2}):", selected, storeToConfig, refresh));
#endif
			if(selected == null || !filterOptions.ContainsKey(selected))
				selected = filterOptions.Keys.Last();

			var newFilter = filterOptions[selected];
			var unavReason = (newFilter as IAvailabilityCheck)?.GetUnavailabilityReason();

			if(unavReason != null) {
				persistentNuts?.ShowErrorASAP($"Can't filter by {selected} - {unavReason}");
				// ReSharper disable once TailRecursiveCall
				SetFilter(null, false, false);
				return;
			}

#if DEBUG
			Plugin.Log.Warn(string.Format("Setting Filter to {0}", selected));
#endif
			if(HookLevelCollectionTableSet.filter != filterOptions[selected]) {
				if(storeToConfig)
					Config.Instance.LastFilter = selected;

				HookLevelCollectionTableSet.filter = filterOptions[selected];
				RestoreTableScroll.ResetScroll();
				if(refresh)
					HookLevelCollectionTableSet.Refresh(true);
			}

			XD.FunnyNull(persistentNuts._filterDropdown)?.SelectCellWithIdx(_filterOptions.IndexOf(selected));
		}

		private static void SetSortDirection(bool ascending, bool refresh = true) {
			if(HookLevelCollectionTableSet.sorter == null)
				return;

			if(Config.Instance.SortAsc != ascending) {
				Config.Instance.SortAsc = ascending;
				RestoreTableScroll.ResetScroll();
				if(refresh)
					HookLevelCollectionTableSet.Refresh(true);
			}

			if(persistentNuts._sortDirection != null)
				persistentNuts._sortDirection.text = ascending ? "▲" : "▼";
		}

		static void ToggleSortDirection() {
			if(HookLevelCollectionTableSet.sorter == null)
				return;

			SetSortDirection(!Config.Instance.SortAsc);
		}

		static readonly System.Random ran = new System.Random();
		static void SelectRandom() {
			var x = UnityEngine.Object.FindObjectOfType<LevelCollectionTableView>();

			if(x == null)
				return;

			/*
			 * I dont think theres any place in the game where SetData is not called with an Array
			 * 
			 * .Count (Enumerable/Linq) is slower than directly accessing and Arrays Length
			 * 
			 * Not that it matters, but for now we can do this.
			 */

			if(!((HookLevelCollectionTableSet.lastOutMapList ?? 
			      HookLevelCollectionTableSet.lastInMapList) is BeatmapLevel[] ml))
				return;

			if(ml.Length < 2)
				return;

			x.SelectLevel(ml[ran.Next(0, ml.Length)]);
		}

		readonly Queue<string> warnings = new Queue<string>();
		bool warningLoadInProgress;
		public void ShowErrorASAP(string text = null) {
			if(text != null)
				warnings.Enqueue(text);
			if(!warningLoadInProgress)
				SharedCoroutineStarter.instance.StartCoroutine(_ShowError());
		}

		[UIAction("PossiblyShowNextWarning")] void PossiblyShowNextWarning() => ShowErrorASAP();

		IEnumerator _ShowError() {
			warningLoadInProgress = true;
			yield return new WaitUntil(() => _failTextLabel != null);
			var x = _failTextLabel.GetComponentInParent<ViewController>();
			if(x != null) {
				yield return new WaitUntil(() => !x.isInTransition);

				if(x.isActivated && warnings.Count > 0) {
					_failTextLabel.text = warnings.Dequeue();
					parserParams.EmitEvent("IncompatabilityNotice");
				}
			}
			warningLoadInProgress = false;
		}


		[UIComponent("filterLoadingIndicator")] internal readonly ImageView _filterLoadingIndicator = null;
		[UIComponent("sortDirection")] readonly ClickableText _sortDirection = null;
		[UIComponent("failTextLabel")] readonly TextMeshProUGUI _failTextLabel = null;

		internal static void Init() {
			initialized = true;
			UpdateVisibleTransformers();
			SetSort(Config.Instance.LastSort, false, false);
			SetFilter(Config.Instance.LastFilter, false, false);
			SetSortDirection(Config.Instance.SortAsc);
		}

		internal static void AttachTo(Transform target) {
			BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "BetterSongList.UI.BSML.MainUI.bsml"), target.gameObject, persistentNuts);
			persistentNuts.rootTransform.localScale *= 0.7f;

			((RectTransform)target).sizeDelta += new Vector2(0, 2);
			target.GetChild(0).position -= new Vector3(0, 0.02f);
		}
		
		BSMLParserParams settingsViewParams = null;
		void SettingsOpened() {
			BSMLStuff.InitSplitView(ref settingsViewParams, rootTransform.gameObject, SplitViews.Settings.instance).EmitEvent("ShowSettings");
		}

		[UIAction("#post-parse")]
		void Parsed() {
			settingsViewParams = null;
			UpdateVisibleTransformers();

			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach(var x in sortOptions)
			{
				if (x.Value != HookLevelCollectionTableSet.sorter) continue;
				SetSort(x.Key, false, false);
				break;
			}
			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach(var x in filterOptions)
			{
				if (x.Value != HookLevelCollectionTableSet.filter) continue;
				SetFilter(x.Key, false, false);
				break;
			}

			UpdateDropdowns();

			SetSortDirection(Config.Instance.SortAsc, false);
		}

		static void HackDropdown(DropdownWithTableView dropdown) {
			var c = Mathf.Min(9, dropdown.tableViewDataSource.NumberOfCells());
			dropdown._numberOfVisibleCells = c;
			dropdown.ReloadData();
		}
	}
}
