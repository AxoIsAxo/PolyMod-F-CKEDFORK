using Cpp2IL.Core.Extensions;
using HarmonyLib;
using PolyMod.Loaders;
using TMPro;
using Unity.Services.Core.Internal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolyMod.Managers
{
    internal static class VisualManager
    {
        private const string HEADER_PREFIX = "<align=\"center\"><size=150%><b>";
        private const string HEADER_POSTFIX = "</b></size><align=\"left\">";

        private static Dictionary<int, int> basicPopupWidths = new();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SplashController), nameof(SplashController.LoadAndPlayClip))]
        private static bool SplashController_LoadAndPlayClip(SplashController __instance)
        {
            string name = "intro.mp4";
            string path = Path.Combine(Application.persistentDataPath, name);
            File.WriteAllBytesAsync(path, Plugin.GetResource(name).ReadBytes());
            __instance.lastPlayTime = Time.realtimeSinceStartup;
            __instance.videoPlayer.url = path;
            __instance.videoPlayer.Play();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BasicPopup), nameof(BasicPopup.Update))]
        private static void BasicPopup_Update(BasicPopup __instance)
        {
            int id = __instance.GetInstanceID();
            if (basicPopupWidths.ContainsKey(id))
                __instance.rectTransform.SetWidth(basicPopupWidths[id]);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PopupBase), nameof(PopupBase.Hide))]
        private static void PopupBase_Hide(PopupBase __instance)
        {
            basicPopupWidths.Remove(__instance.GetInstanceID());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PopupButtonContainer), nameof(PopupButtonContainer.SetButtonData))]
        private static void PopupButtonContainer_SetButtonData(PopupButtonContainer __instance)
        {
            int num = __instance.buttons.Length;
            for (int i = 0; i < num; i++)
            {
                UITextButton uitextButton = __instance.buttons[i];
                Vector2 vector = new((num == 1) ? 0.5f : (i / (num - 1.0f)), 0.5f);
                uitextButton.rectTransform.anchorMin = vector;
                uitextButton.rectTransform.anchorMax = vector;
                uitextButton.rectTransform.pivot = vector;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartScreen), nameof(StartScreen.Start))]
        private static void StartScreen_Start()
        {
            GameObject originalText = GameObject.Find("SettingsButton/DescriptionText");
            GameObject text = GameObject.Instantiate(originalText, originalText.transform.parent.parent.parent);
            text.name = "PolyModVersion";
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchoredPosition = new(265, 40);
            rect.sizeDelta = new(500, rect.sizeDelta.y);
            rect.anchorMax = new(0, 0);
            rect.anchorMin = new(0, 0);
            text.GetComponent<TextMeshProUGUI>().fontSize = 18;
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
            text.GetComponent<TMPLocalizer>().Text = $"PolyMod {(Plugin.DEV ? "Dev" : Plugin.VERSION)}";
            text.AddComponent<LayoutElement>().ignoreLayout = true;

            GameObject originalButton = GameObject.Find("StartScreen/NewsButton");
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.gameObject.name = "PolyModHubButton";
            button.transform.position = originalButton.transform.position - new Vector3(90, 0, 0);
            button.active = true;

            Transform descriptionText = button.transform.Find("DescriptionText");
            descriptionText.gameObject.SetActive(true);
            descriptionText.GetComponentInChildren<TMPLocalizer>().Key = "polymod.hub";

            UIRoundButton buttonObject = button.GetComponent<UIRoundButton>();
            buttonObject.bg.sprite = SpritesLoader.BuildSprite(Plugin.GetResource("polymod_icon.png").ReadBytes());
            buttonObject.bg.transform.localScale = new Vector3(1.2f, 1.2f, 0);
            buttonObject.bg.color = Color.white;

            buttonObject.outline.gameObject.SetActive(false);
            buttonObject.iconContainer.gameObject.SetActive(false);
            buttonObject.OnClicked += (UIButtonBase.ButtonAction)PolyModHubButtonClicked;

            static void PolyModHubButtonClicked(int buttonId, BaseEventData eventData)
            {
                BasicPopup popup = PopupManager.GetBasicPopup();
                popup.Header = Localization.Get("polymod.hub");
                popup.Description = Localization.Get("polymod.hub.header", new Il2CppSystem.Object[] {
                    HEADER_PREFIX,
                    HEADER_POSTFIX
                }) + "\n\n";
                foreach (var mod in ModManager.mods.Values)
                {
                    popup.Description += Localization.Get("polymod.hub.mod", new Il2CppSystem.Object[] {
                        mod.name,
                        Localization.Get("polymod.hub.mod.status."
                            + Enum.GetName(typeof(ModManager.Mod.Status), mod.status)!.ToLower()),
                        string.Join(", ", mod.authors),
                        mod.version.ToString()
                    });
                    popup.Description += "\n\n";
                }
                popup.Description += Localization.Get("polymod.hub.footer", new Il2CppSystem.Object[] {
                    HEADER_PREFIX,
                    HEADER_POSTFIX
                });
                List<PopupBase.PopupButtonData> popupButtons = new()
                {
                    new("buttons.back"),
                    new(
                        "polymod.hub.discord",
                        callback: (UIButtonBase.ButtonAction)((int _, BaseEventData _) =>
                            NativeHelpers.OpenURL("https://discord.gg/eWPdhWtfVy", false))
                    )
                };
                if (Plugin.config.debug)
                    popupButtons.Add(new(
                        "polymod.hub.dump",
                        callback: (UIButtonBase.ButtonAction)((int _, BaseEventData _) => {
                            //TODO: fix dump
                            Directory.CreateDirectory(Plugin.DUMPED_DATA_PATH);
                            foreach (int version in PolytopiaDataManager.gameLogicDatas.Keys)
                            {
                                File.WriteAllTextAsync(
                                    Path.Combine(Plugin.DUMPED_DATA_PATH, $"gameLogicData{version}.json"),
                                    PolytopiaDataManager.provider.LoadGameLogicData(version)
                                );
                            }
                            File.WriteAllTextAsync(
                                Path.Combine(Plugin.DUMPED_DATA_PATH, $"avatarData.json"),
                                PolytopiaDataManager.provider.LoadAvatarData(1337)
                            );
                            NotificationManager.Notify(Localization.Get("polymod.hub.dumped"));
                        }),
                        closesPopup: false
                    ));
                popup.buttonData = popupButtons.ToArray();
                popup.ShowSetWidth(1000);
            }
        }

        internal static void ShowSetWidth(this BasicPopup self, int width)
        {
            basicPopupWidths.Add(self.GetInstanceID(), width);
            self.Show();
        }

        internal static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(VisualManager));
        }
    }
}