﻿using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System.IO;
using BepInEx.Logging;

using Random = UnityEngine.Random;
using TMPro;

namespace TrashItems
{
    [BepInPlugin("virtuacode.valheim.trashitems", "Trash Items Mod", "0.0.1")]

    class TrashItems : BaseUnityPlugin
    {
        public static ConfigEntry<bool> ConfirmDialog;
        public static ConfigEntry<KeyboardShortcut> TrashHotkey;
        public static ConfigEntry<SoundEffect> Sfx;
        public static ConfigEntry<Color> TrashColor;
        public static ConfigEntry<string> TrashLabel;
        public static bool _clickedTrash = false;
        public static bool _confirmed = false;
        public static InventoryGui _gui;
        public static Sprite trashSprite;
        public static Sprite bgSprite;
        public static GameObject dialog;
        public static AudioClip[] sounds = new AudioClip[3];
        public static Transform trash;
        public static AudioSource audio;
        public static TrashButton trashButton;

        public static ManualLogSource MyLogger;


        public enum SoundEffect
        {
            None,
            Sound1,
            Sound2,
            Sound3,
            Random
        }

        public static void Log(string msg)
        {
            MyLogger?.LogInfo(msg);
        }

        public static void LogErr(string msg)
        {
            MyLogger?.LogError(msg);
        }
        public static void LogWarn(string msg)
        {
            MyLogger?.LogWarning(msg);
        }

        private void Awake()
        {
            MyLogger = Logger;


            ConfirmDialog = Config.Bind<bool>("General", "ConfirmDialog", false, "Show confirm dialog");
            Sfx = Config.Bind<SoundEffect>("General", "SoundEffect", SoundEffect.Random,
                "Sound effect when trashing items");
            TrashHotkey = Config.Bind<KeyboardShortcut>("Input", "TrashHotkey", KeyboardShortcut.Deserialize("Delete"), "Hotkey for destroying items");
            TrashLabel = Config.Bind<string>("General", "TrashLabel", "Trash", "Label for the trash button");
            TrashColor = Config.Bind<Color>("General", "TrashColor", new Color(1f, 0.8482759f, 0), "Color for the trash label");

            TrashLabel.SettingChanged += (sender, args) => { if (trashButton != null) { trashButton.SetText(TrashLabel.Value); } };
            TrashColor.SettingChanged += (sender, args) => { if (trashButton != null) { trashButton.SetColor(TrashColor.Value); } };

            Log(nameof(TrashItems) + " Loaded!");
            
            trashSprite = LoadSprite("TrashItems.res.trash.png", new Rect(0, 0, 64, 64), new Vector2(32, 32));
            bgSprite = LoadSprite("TrashItems.res.trashmask.png", new Rect(0, 0, 96, 112), new Vector2(48, 56));

            sounds[0] = LoadAudioClip("TrashItems.res.trash1.wav");
            sounds[1] = LoadAudioClip("TrashItems.res.trash2.wav");
            sounds[2] = LoadAudioClip("TrashItems.res.trash3.wav");

            Harmony.CreateAndPatchAll(typeof(TrashItems));
        }


        public static AudioClip LoadAudioClip(string path)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream audioStream = asm.GetManifestResourceStream(path);

            using (MemoryStream mStream = new MemoryStream())
            {
                audioStream.CopyTo(mStream);
                return WavUtility.ToAudioClip(mStream.GetBuffer());
            }
        }

        public static Sprite LoadSprite(string path, Rect size, Vector2 pivot, int units = 100)
        {

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream trashImg = asm.GetManifestResourceStream(path);

            Texture2D tex = new Texture2D((int) size.width, (int) size.height, TextureFormat.RGBA32, false, true);

            using (MemoryStream mStream = new MemoryStream())
            {
                trashImg.CopyTo(mStream);
                tex.LoadImage(mStream.ToArray());
                tex.Apply();
                return Sprite.Create(tex, size, pivot, units);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Show")]
        [HarmonyPostfix]
        public static void Show_Postfix(InventoryGui __instance)
        {
            Transform playerInventory = InventoryGui.instance.m_player.transform;
            trash = playerInventory.Find("Trash");

            if (trash != null)
                return;

            _gui = InventoryGui.instance;

            trash = Instantiate(playerInventory.Find("Armor"), playerInventory);
            trashButton = trash.gameObject.AddComponent<TrashButton>();

            var guiMixer = AudioMan.instance.m_masterMixer.FindMatchingGroups("GUI")[0];

            audio = trash.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.outputAudioMixerGroup = guiMixer;
            audio.bypassReverbZones = true;
        }

        public class TrashButton : MonoBehaviour
        {
            private Canvas canvas;
            private GraphicRaycaster raycaster;
            private RectTransform rectTransform;
            private GameObject buttonGo;

            void Awake()
            {
                if (InventoryGui.instance == null)
                    return;

                var playerInventory = InventoryGui.instance.m_player.transform;
                RectTransform rect = GetComponent<RectTransform>();
                rect.anchoredPosition -= new Vector2(0, 78);

                SetText(TrashLabel.Value);
                SetColor(TrashColor.Value);


                // Replace armor with trash icon
                Transform tArmor = transform.Find("armor_icon");
                if (!tArmor)
                {
                    LogErr("armor_icon not found!");
                }
                tArmor.GetComponent<Image>().sprite = trashSprite;

                transform.SetSiblingIndex(0);
                transform.gameObject.name = "Trash";

                buttonGo = new GameObject("ButtonCanvas");
                rectTransform = buttonGo.AddComponent<RectTransform>();
                rectTransform.transform.SetParent(transform.transform, true);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(70, 74);
                canvas = buttonGo.AddComponent<Canvas>();
                raycaster = buttonGo.AddComponent<GraphicRaycaster>(); 


                // Add trash ui button
                Button button = buttonGo.AddComponent<Button>();
                button.onClick.AddListener(new UnityAction(TrashItems.TrashItem));
                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0);

                // Add border background
                Transform frames = playerInventory.Find("selected_frame");
                GameObject newFrame = Instantiate(frames.GetChild(0).gameObject, transform);
                newFrame.GetComponent<Image>().sprite = bgSprite;
                newFrame.transform.SetAsFirstSibling();
                newFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(-8, 22);
                newFrame.GetComponent<RectTransform>().anchoredPosition = new Vector2(6, 7.5f);

                // Add inventory screen tab
                UIGroupHandler handler = gameObject.AddComponent<UIGroupHandler>();
                handler.m_groupPriority = 1;
                handler.m_enableWhenActiveAndGamepad = newFrame;
                _gui.m_uiGroups = _gui.m_uiGroups.AddToArray(handler);

                gameObject.AddComponent<TrashHandler>();
            }

            void Start()
            {
               StartCoroutine(DelayedOverrideSorting());
            }

            private IEnumerator DelayedOverrideSorting()
            {
                yield return null;

                if (canvas == null) yield break;

                canvas.overrideSorting = true;
                canvas.sortingOrder = 1;
            }

            public void SetText(string text)
            {
                Transform tText = transform.Find("ac_text");
                if (!tText)
                {
                    LogErr("ac_text not found!");
                    return;
                }
                tText.GetComponent<TMP_Text>().text = text;
            }

            public void SetColor(Color color)
            {
                Transform tText = transform.Find("ac_text");
                if (!tText)
                {
                    LogErr("ac_text not found!");
                    return;
                }
                tText.GetComponent<TMP_Text>().color = color;
            }
        }


        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        public static void Postfix()
        {
            OnCancel();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        public static void UpdateItemDrag_Postfix(InventoryGui __instance, ref GameObject ___m_dragGo, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount)
        {
            if (TrashHotkey.Value.IsDown())
            {
                _clickedTrash = true;
            }
                

            if (_clickedTrash && ___m_dragItem != null && ___m_dragInventory.ContainsItem(___m_dragItem))
            {
                if (ConfirmDialog.Value)
                {
                    if (_confirmed)
                    {
                        _confirmed = false;
                    }
                    else
                    {
                        ShowConfirmDialog(___m_dragItem, ___m_dragAmount);
                        _clickedTrash = false;
                        return;
                    }
                }

                if (___m_dragAmount == ___m_dragItem.m_stack)
                {
                    Player.m_localPlayer.RemoveEquipAction(___m_dragItem);
                    //Player.m_localPlayer.RemoveFromEquipQueue(___m_dragItem);
                    Player.m_localPlayer.UnequipItem(___m_dragItem, false);
                    ___m_dragInventory.RemoveItem(___m_dragItem);
                }
                else
                    ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);

                if (audio != null)
                {
                    switch (Sfx.Value)
                    {
                        case SoundEffect.None:
                            break;
                        case SoundEffect.Random:
                            audio.PlayOneShot(sounds[Random.Range(0, 3)]);
                            break;
                        case SoundEffect.Sound1:
                            audio.PlayOneShot(sounds[0]);
                           
                            break;
                        case SoundEffect.Sound2:
                            audio.PlayOneShot(sounds[1]);
                            break;
                        case SoundEffect.Sound3:
                            audio.PlayOneShot(sounds[2]);
                            break;
                    }
                }

                __instance.GetType().GetMethod("SetupDragItem", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(__instance, new object[] {null, null, 0});
                __instance.GetType().GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
            }

            _clickedTrash = false;
        }

        public static void ShowConfirmDialog(ItemDrop.ItemData item, int itemAmount)
        {
            if (InventoryGui.instance == null)
                return;

            if (dialog != null)
                return;

            dialog = Instantiate(InventoryGui.instance.m_splitPanel.gameObject, InventoryGui.instance.transform);

            var okButton = dialog.transform.Find("win_bkg/Button_ok").GetComponent<Button>();
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(new UnityAction(OnConfirm));
            okButton.GetComponentInChildren<TextMeshProUGUI>().text = "Trash";
            okButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 0.2f, 0.1f);

            var cancelButton = dialog.transform.Find("win_bkg/Button_cancel").GetComponent<Button>();
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(new UnityAction(OnCancel));

            dialog.transform.Find("win_bkg/Slider").gameObject.SetActive(false);

            var text = dialog.transform.Find("win_bkg/Text").GetComponent<TextMeshProUGUI>();
            text.text = Localization.instance.Localize(item.m_shared.m_name);

            var icon = dialog.transform.Find("win_bkg/Icon_bkg/Icon").GetComponent<Image>();
            icon.sprite = item.GetIcon();

            var amount = dialog.transform.Find("win_bkg/amount").GetComponent<TextMeshProUGUI>();

            amount.text = itemAmount + "/" + item.m_shared.m_maxStackSize;

            dialog.gameObject.SetActive(true);
        }

        public static void OnConfirm()
        {
            _confirmed = true;
            if (dialog != null)
            {
                Destroy(dialog);
                dialog = null;
            }

            TrashItem();
        }

        public static void OnCancel()
        {
            _confirmed = false;
            if (dialog != null)
            {
                Destroy(dialog);
                dialog = null;
            }
        }

        public static void TrashItem()
        {
            Log("Trash Items clicked!");

            if (_gui == null)
            {
                LogErr("_gui is null");
                return;
            }

            _clickedTrash = true;

            _gui.GetType().GetMethod("UpdateItemDrag", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_gui, new object[] { });
        }
    }

    public class TrashHandler : MonoBehaviour
    {
        private UIGroupHandler handler;
        private void Awake()
        {
            handler = this.GetComponent<UIGroupHandler>();
            handler.SetActive(false);
        }

        private void Update()
        {
            if (ZInput.GetButtonDown("JoyButtonA") && handler.IsActive)
            {
                TrashItems.TrashItem();
                // Switch back to inventory iab
                typeof(InventoryGui).GetMethod("SetActiveGroup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InventoryGui.instance, new object[] { 1, false });
            }
        }
    }
}
