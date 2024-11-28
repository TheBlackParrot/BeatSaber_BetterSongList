using BeatSaberMarkupLanguage;
using BetterSongList.Util;
using HarmonyLib;
using HMUI;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using System.Reflection;

namespace BetterSongList.HarmonyPatches.UI {
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	static class ExtraLevelParams {
		static GameObject extraUI = null;
		static TextMeshProUGUI[] fields = null;

		static HoverHintController hhc = null;
		static Sprite favIcon = null;
		static IEnumerator ProcessFields() {
			//Need to wait until the end of frame for reasons beyond my understanding
			yield return new WaitForEndOfFrame();

			static void ModifyValue(TextMeshProUGUI text, string hoverHint, string iconName) {
				var icon = text.transform.parent.Find("Icon").GetComponent<ImageView>();

				if(iconName == "Favorites") {
					if(favIcon != null) {
						icon.sprite = favIcon;
					} else {
						Utilities.LoadSpriteFromAssemblyAsync("BetterSongList.UI.FavoritesIcon.png").ContinueWith(x => {
							icon.sprite = favIcon = x.Result;
						});
					}
				} else {
					icon.SetImageAsync($"#{iconName}Icon");
				}

				GameObject.DestroyImmediate(text.GetComponentInParent<LocalizedHoverHint>());
				var hhint = text.GetComponentInParent<HoverHint>();

				if(hhint == null)
					return;

				if(hhc == null)
					hhc = UnityEngine.Object.FindObjectOfType<HoverHintController>();

				// Normally zenjected, not here obviously. I dont think the Controller is ever destroyed so we dont need to explicit null check
				ReflectionUtil.SetField(hhint, "_hoverHintController", hhc);
				hhint.text = hoverHint;
			}

			ModifyValue(fields[0], "Star Rating", "Favorites");
			ModifyValue(fields[1], "Jump Speed", "FastNotes");
			ModifyValue(fields[2], "Reaction Time", "Clock");
			ModifyValue(fields[3], "Map Upload Date", "Height");
			
			foreach (var field in fields)
			{
				field.richText = true;
			}
		}

		static StandardLevelDetailView lastInstance = null;

		public static void UpdateState() {
			if(lastInstance != null && lastInstance.isActiveAndEnabled)
				lastInstance.RefreshContent();
		}
		
		private enum MonthNames { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec }

		static void Postfix(StandardLevelDetailView __instance) {
			if(extraUI == null) {
				// I wanted to make a custom UI for this with bsml first... But this is MUCH easier and probably looks better
				extraUI = GameObject.Instantiate(__instance._levelParamsPanel, __instance._levelParamsPanel.transform.parent).gameObject;
				extraUI.GetComponentInChildren<CanvasGroup>().alpha = 1;

				GameObject.Destroy(extraUI.GetComponent<LevelParamsPanel>());

				__instance._levelParamsPanel.transform.localPosition += new Vector3(0, 3.5f);
				extraUI.transform.localPosition -= new Vector3(0, 1f);

				fields = extraUI.GetComponentsInChildren<CurvedTextMeshPro>();
				SharedCoroutineStarter.instance.StartCoroutine(ProcessFields());
			}

			lastInstance = __instance;

			if(fields != null) {
				var beatmapKey = __instance.beatmapKey;

				if(!SongDetailsUtil.isAvailable) {
					fields[0].text = "N/A";
				} else if(SongDetailsUtil.songDetails != null) {
					void wrapper() {
						// For now we can assume non-standard diff is unranked. Probably not changing any time soon i guess
						var ch = (SongDetailsCache.Structs.MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(beatmapKey);

						if(ch != SongDetailsCache.Structs.MapCharacteristic.Standard) {
							fields[0].text = "-";
						} else {
							var mh = BeatmapsUtil.GetHashOfLevel(__instance._beatmapLevel);

							if(mh == null ||
								!SongDetailsUtil.songDetails.instance.songs.FindByHash(mh, out var song) ||
								!song.GetDifficulty(
									out var diff,
									(SongDetailsCache.Structs.MapDifficulty)beatmapKey.difficulty,
									ch
								)
							) {
								fields[0].text = fields[3].text = "-";
								return;
							} else {
								var isSs = Config.Instance.PreferredLeaderboard == "ScoreSaber";
								float stars = isSs ? diff.stars : diff.starsBeatleader;

								if(stars > 0) {
									string[] starsRaw = stars.ToString("0.00").Split('.');
									fields[0].text = starsRaw[0] + "<size=85%>." + starsRaw[1];
								} else {
									fields[0].text = "-";
								}
							}
							
							var uploadTime = DateTimeOffset.FromUnixTimeSeconds((int)song.uploadTimeUnix);
							fields[3].text = ((MonthNames)uploadTime.Month - 1) + " " + uploadTime.Year.ToString().Substring(2, 2);
						}
					}
					wrapper();
				// This might end up Double-Initing SongDetails but SongDetails handles that internally and only does it once so whatever
				} else if(!SongDetailsUtil.finishedInitAttempt) {
					SongDetailsUtil.TryGet().ContinueWith(
						x => { if(x.Result != null) UpdateState(); },
						CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
					);
				}

				// Basegame maps have no NJS or JD
				var basicData = __instance._beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
				float njs = basicData?.noteJumpMovementSpeed ?? 0;
				if(njs == 0)
					njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(beatmapKey.difficulty);
				float rt = JumpDistanceCalculator.GetRt(__instance._beatmapLevel.beatsPerMinute, njs, basicData?.noteJumpStartBeatOffset ?? 0);
				
				string[] njsRaw = njs.ToString("0.##").Split('.');
				fields[1].text = (njsRaw.Length == 1 ? njsRaw[0] : njsRaw[0] + "<size=80%>." + njsRaw[1]) + "<size=65%> NJS";

				if(rt < 1000) {
					fields[2].text = rt.ToString("0") + "<size=65%> MS";
				} else {
					fields[2].text = (rt/1000).ToString("0.#") + "<size=65%> S";
				}
			}
		}
	}
}
