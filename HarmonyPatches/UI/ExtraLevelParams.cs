using BeatSaberMarkupLanguage;
using BetterSongList.Util;
using HarmonyLib;
using HMUI;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using JetBrains.Annotations;
using TMPro;
using Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterSongList.HarmonyPatches.UI
{
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	internal static class ExtraLevelParams
	{
		private static GameObject _extraUI;
		private static TextMeshProUGUI[] _fields;

		private static HoverHintController _hhc;
		private static Sprite _favIcon;

		private static void ModifyValue(TextMeshProUGUI text, string hoverHint, string iconName)
		{
			var icon = text.transform.parent.Find("Icon").GetComponent<ImageView>();

			if(iconName == "Favorites")
			{
				if(_favIcon)
				{
					icon.sprite = _favIcon;
				}
				else
				{
					Utilities.LoadSpriteFromAssemblyAsync("BetterSongList.UI.FavoritesIcon.png").ContinueWith(x => {
						icon.sprite = _favIcon = x.Result;
					});
				}
			}
			else
			{
				icon.SetImageAsync($"#{iconName}Icon");
			}

			Object.DestroyImmediate(text.GetComponentInParent<LocalizedHoverHint>());
			var hhint = text.GetComponentInParent<HoverHint>();

			if (!hhint)
			{
				return;
			}

			if (!_hhc)
			{
				_hhc = Object.FindObjectOfType<HoverHintController>();
			}

			// Normally zenjected, not here obviously. I dont think the Controller is ever destroyed so we dont need to explicit null check
			hhint.SetField("_hoverHintController", _hhc);
			hhint.text = hoverHint;
		}

		private static IEnumerator ProcessFields()
		{
			//Need to wait until the end of frame for reasons beyond my understanding
			yield return new WaitForEndOfFrame();

			ModifyValue(_fields[0], "Star Rating", "Favorites");
			ModifyValue(_fields[1], "Jump Speed", "FastNotes");
			ModifyValue(_fields[2], "Reaction Time", "Clock");
			ModifyValue(_fields[3], "Map Upload Date", "Height");

			foreach (var field in _fields)
			{
				field.richText = true;
			}
		}

		private static StandardLevelDetailView _lastInstance;

		public static void UpdateState()
		{
			if (_lastInstance && _lastInstance.isActiveAndEnabled)
			{
				_lastInstance.RefreshContent();
			}
		}
		
		private static void Wrapper(StandardLevelDetailView standardLevelDetailView, BeatmapKey beatmapKey)
		{
			// For now we can assume non-standard diff is unranked. Probably not changing any time soon i guess
			var ch = (SongDetailsCache.Structs.MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(beatmapKey);

			if (ch != SongDetailsCache.Structs.MapCharacteristic.Standard)
			{
				_fields[0].text = "-";
			}
			else
			{
				var mh = BeatmapsUtil.GetHashOfLevel(standardLevelDetailView._beatmapLevel);

				if (mh == null ||
				   !SongDetailsUtil.SongDetails.Instance.songs.FindByHash(mh, out var song) ||
				   !song.GetDifficulty(
					   out var diff,
					   (SongDetailsCache.Structs.MapDifficulty)beatmapKey.difficulty,
					   ch
				   ))
				{
					_fields[0].text = _fields[3].text = "-";
					return;
				}
				
				float stars = Config.Instance.PreferredLeaderboard == "ScoreSaber" ? diff.stars : diff.starsBeatleader;

				if(stars > 0)
				{
					string[] starsRaw = stars.ToString("0.00").Split('.');
					_fields[0].text = $"{starsRaw[0]}<size=85%>.{starsRaw[1]}";
				}
				else
				{
					_fields[0].text = "-";
				}

				var uploadTime = DateTimeOffset.FromUnixTimeSeconds((int)song.uploadTimeUnix);
				_fields[3].text = $"{(MonthNames)uploadTime.Month - 1} {uploadTime.Year.ToString().Substring(2, 2)}";
			}
		}
		

		// ReSharper disable UnusedMember.Local
		private enum MonthNames { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec }
		// ReSharper restore UnusedMember.Local
		
		private static readonly Color TransparentWhite = new Color(1, 1, 1, 0.75f);

		private static CanvasGroup _extraParamsCanvasGroup;
		private static CanvasGroup _baseGameParamsCanvasGroup;
		private static void FadeInParams(float f)
		{
			_extraParamsCanvasGroup.alpha = f;
			_baseGameParamsCanvasGroup.alpha = f;
		}

		[UsedImplicitly]
		// ReSharper disable once InconsistentNaming
		private static void Postfix(StandardLevelDetailView __instance)
		{
			if(!_extraUI)
			{
				_baseGameParamsCanvasGroup = __instance._levelParamsPanel.GetComponentInChildren<CanvasGroup>();
				
				__instance._levelParamsPanel._bombsCountText.color = Color.white;
				__instance._levelParamsPanel._bombsCountText.transform.parent.Find("Icon").GetComponent<ImageView>().color = TransparentWhite;
				__instance._levelParamsPanel._notesCountText.color = Color.white;
				__instance._levelParamsPanel._notesCountText.transform.parent.Find("Icon").GetComponent<ImageView>().color = TransparentWhite;
				__instance._levelParamsPanel._obstaclesCountText.color = Color.white;
				__instance._levelParamsPanel._obstaclesCountText.transform.parent.Find("Icon").GetComponent<ImageView>().color = TransparentWhite;
				__instance._levelParamsPanel._notesPerSecondText.color = Color.white;
				__instance._levelParamsPanel._notesPerSecondText.transform.parent.Find("Icon").GetComponent<ImageView>().color = TransparentWhite;
				
				// I wanted to make a custom UI for this with bsml first... But this is MUCH easier and probably looks better
				_extraUI = Object.Instantiate(__instance._levelParamsPanel, __instance._levelParamsPanel.transform.parent).gameObject;
				_extraParamsCanvasGroup = _extraUI.GetComponentInChildren<CanvasGroup>();

				Object.Destroy(_extraUI.GetComponent<LevelParamsPanel>());

				__instance._levelParamsPanel.transform.localPosition += new Vector3(0, 4f);
				//_extraUI.transform.localPosition += new Vector3(0, 1f);
				
				for(int i = 1; i < __instance._levelParamsPanel.transform.childCount; i++)
				{
					Vector3 oldPos = __instance._levelParamsPanel.transform.GetChild(i).localPosition;
					__instance._levelParamsPanel.transform.GetChild(i).localPosition = new Vector3(i * 16f, oldPos.y, oldPos.z);
					
					oldPos = _extraUI.transform.GetChild(i).localPosition;
					_extraUI.transform.GetChild(i).localPosition = new Vector3(i * 16f, oldPos.y, oldPos.z);
				}

				_fields = _extraUI.GetComponentsInChildren<TextMeshProUGUI>();
				SharedCoroutineStarter.instance.StartCoroutine(ProcessFields());

				GameObject panelBg = Object.Instantiate(__instance.transform.Find("BeatmapCharacteristic/BG"), __instance.transform).gameObject;
				panelBg.name = "LevelParamsBackground";
				panelBg.transform.SetSiblingIndex(0);

				if (panelBg.TryGetComponent(out RectTransform panelBgRectTransform))
				{
					panelBgRectTransform.offsetMin = new Vector2(2f, 29.5f);
					panelBgRectTransform.offsetMax = new Vector2(-2f, -15.5f);
				}
				
				__instance._levelParamsPanelCanvasGroupTween = new FloatTween(0.0f, 1f, FadeInParams, 0.15f, EaseType.InSine);
			}

			_extraParamsCanvasGroup.alpha = 0;
			_lastInstance = __instance;

			if(_fields == null)
			{
				return;
			}
			
			var beatmapKey = __instance.beatmapKey;

			if(!SongDetailsUtil.IsAvailable)
			{
				_fields[0].text = "N/A";
			}
			else if(SongDetailsUtil.SongDetails != null)
			{
				Wrapper(__instance, beatmapKey);
				// This might end up Double-Initing SongDetails but SongDetails handles that internally and only does it once so whatever
			}
			else if(!SongDetailsUtil.FinishedInitAttempt)
			{
				SongDetailsUtil.TryGet().ContinueWith(
					x => { if(x.Result != null) UpdateState(); },
					CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
					TaskScheduler.FromCurrentSynchronizationContext()
				);
			}

			// Basegame maps have no NJS or JD
			var basicData = __instance._beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
			
			float njs = basicData?.noteJumpMovementSpeed ?? beatmapKey.difficulty.DefaultNoteJumpMovementSpeed();
			if (njs == 0)
			{
				njs = beatmapKey.difficulty.DefaultNoteJumpMovementSpeed();
			}
			
			float rt = JumpDistanceCalculator.GetRt(__instance._beatmapLevel.beatsPerMinute, njs, basicData?.noteJumpStartBeatOffset ?? 0);
				
			string[] njsRaw = njs.ToString("0.##").Split('.');
			_fields[1].text = (njsRaw.Length == 1 ? njsRaw[0] : njsRaw[0] + "<size=80%>." + njsRaw[1]) + "<size=65%> NJS";

			_fields[2].text = rt < 1000 ? $"{rt:0}<size=65%> MS" : $"{rt / 1000:0.#}<size=65%> S";
		}
	}
}
