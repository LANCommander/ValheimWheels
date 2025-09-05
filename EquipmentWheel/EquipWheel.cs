using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace EquipmentWheel
{
    [BepInPlugin("virtuacode.valheim.equipwheel", "Equip Wheel Mod", "0.0.1")]
    public class EquipWheel : BaseUnityPlugin, WheelManager.IWheel
    {
        private static Harmony _harmony;
        
        public static ManualLogSource MyLogger = BepInEx.Logging.Logger.CreateLogSource(Assembly.GetExecutingAssembly().GetName().Name);

        public static ConfigEntry<KeyboardShortcut> Hotkey;
        public static ConfigEntry<WheelManager.DPadButton> HotkeyDPad;
        public static ConfigEntry<bool> UseSitButton;

        public static ConfigEntry<bool> EquipWhileRunning;
        public static ConfigEntry<bool> AutoEquipShield;
        public static ConfigEntry<bool> HideHotkeyBar;
        public static ConfigEntry<int> IgnoreJoyStickDuration;
        public static ConfigEntry<bool> UseRarityColoring;

        public static ConfigEntry<bool> TriggerOnRelease;
        public static ConfigEntry<bool> TriggerOnClick;
        public static ConfigEntry<bool> ToggleMenu;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<Color> HighlightColor;
        public static ConfigEntry<float> GuiScale;

        public static ConfigEntry<int> InventoryRow;
        public static ConfigEntry<bool> ItemFiltering;
        public static ConfigEntry<ItemDrop.ItemData.ItemType>[] ItemTypes;
        public static ConfigEntry<string> ItemRegex;
        public static ConfigEntry<string> ItemRegexIgnore;
        public static ConfigEntry<bool> ItemRegexCaseSensitive;


        private static EquipWheel instance;
        public static KeyCode ReplacedKey = KeyCode.None;
        public static List<string> ReplacedButtons = new List<string>();
        public static float JoyStickIgnoreTime = 0;
        public static EquipGui Gui;



        public static Color GetHighlightColor => HighlightColor.Value;

        public static EquipWheel Instance
        {
            get
            {
                return instance;
            }
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

        private static bool TakeInput(bool look = false)
        {
            return !GameCamera.InFreeFly() && 
                ((!Chat.instance || !Chat.instance.HasFocus()) 
                && !Menu.IsVisible() && !global::Console.IsVisible() 
                && !TextInput.IsVisible() 
                && !Minimap.InTextInput() 
                && (!ZInput.IsGamepadActive() || !Minimap.IsOpen()) 
                //&& (!ZInput.IsGamepadActive() || !InventoryGui.IsVisible()) 
                && (!ZInput.IsGamepadActive() || !StoreGui.IsVisible()) 
                && (!ZInput.IsGamepadActive() || !Hud.IsPieceSelectionVisible())) 
                && (!PlayerCustomizaton.IsBarberGuiVisible() || look) 
                && (!PlayerCustomizaton.BarberBlocksLook() || !look);
        }
      
        private static bool InInventoryEtc()
        {
            return Minimap.IsOpen() || StoreGui.IsVisible() || Hud.IsPieceSelectionVisible();
        }

        public static bool CanOpenMenu
        {
            get
            {
                Player localPlayer = Player.m_localPlayer;

                bool canOpenMenu = !(localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
                    && !(!TakeInput(true) || InInventoryEtc()) 
                    && (!WheelManager.InventoryVisible) 
                    && !(IsUsingUseButton() && WheelManager.PressedOnHovering);
                return canOpenMenu;
            }
        }

        public static bool IsDedicated()
        {
            var method = typeof(ZNet).GetMethod(nameof(ZNet.IsDedicated), BindingFlags.Public | BindingFlags.Instance);
            var openDelegate = (Func<ZNet, bool>)Delegate.CreateDelegate
                (typeof(Func<ZNet, bool>), method);
            return openDelegate(null);
        }

        public static bool IsUsingUseButton()
        {
            return ZInput.IsGamepadActive() && HotkeyDPad.Value == WheelManager.DPadButton.None && !UseSitButton.Value;
        }

        public void Awake()
        {
            instance = this;

            /* General */
            ModEnabled = Config.Bind(
                "General",
                "ModEnabled",
                true,
                "Enable mod when value is true");

            /* Input */
            Hotkey = Config.Bind(
                "Input",
                "Hotkey",
                KeyboardShortcut.Deserialize("G"),
                "Hotkey for opening equip wheel menu");

            HotkeyDPad = Config.Bind(
                "Input",
                "HotkeyDPad",
                WheelManager.DPadButton.None, 
                "Hotkey on the D-Pad (None, Left, Right or LeftOrRight)");

            UseSitButton = Config.Bind(
                "Input", 
                "UseSitButton", 
                false, 
                "When enabled use the sit button as hotkey (HotkeyDPad has to be set to None)");

            TriggerOnRelease = Config.Bind(
                "Input", 
                "TriggerOnRelease", 
                true,
                "Releasing the Hotkey will equip/use the selected item");

            TriggerOnClick = Config.Bind(
                "Input", 
                "TriggerOnClick", 
                false,
                "Click with left mouse button will equip/use the selected item");

            ToggleMenu = Config.Bind(
                "Input", 
                "ToggleMenu",
                false,
                "When enabled the equip wheel will toggle between hidden/visible when the hotkey was pressed");

            /* Appereance */
            HighlightColor = Config.Bind(
                "Appereance", 
                "HighlightColor", 
                new Color(0.414f, 0.734f, 1f),
                "Color of the highlighted selection");

            GuiScale = Config.Bind(
                "Appereance", 
                "GuiScale", 
                0.5f, 
                "Scale factor of the user interface");
            
            HideHotkeyBar = Config.Bind(
                "Appereance", 
                "HideHotkeyBar", 
                false, 
                "Hides the top-left Hotkey Bar");

            /* Misc */
            EquipWhileRunning = Config.Bind(
                "Misc", 
                "EquipWhileRunning", 
                true, 
                "Allow to equip weapons while running");

            AutoEquipShield = Config.Bind(
                "Misc", 
                "AutoEquipShield", true,
                "Enable auto equip of shield when one-handed weapon was equipped");

            HideHotkeyBar.SettingChanged += (e, args) =>
            {
                if (Hud.instance == null)
                    return;

                HotkeyBar hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").GetComponent<HotkeyBar>();

                if (hotKeyBar == null)
                    return;

                hotKeyBar.gameObject.SetActive(!HideHotkeyBar.Value);
            };
            
            IgnoreJoyStickDuration = Config.Bind(
                "Input", 
                "IgnoreJoyStickDuration", 
                500,
                new ConfigDescription(
                    "Duration in milliseconds for ignoring left joystick input after button release",
                    new AcceptableValueRange<int>(0, 2000)
                ));

            InventoryRow = Config.Bind(
                "Misc", 
                "InventoryRow", 
                1,
                new ConfigDescription(
                    "Row of the inventory that should be used for the equip wheel",
                    new AcceptableValueRange<int>(1, 4)
                ));

            ItemFiltering = Config.Bind(
                "Misc", 
                "ItemFiltering", 
                false,
                "Will scan the whole inventory for items of the specified item types and Regex and show them in the equip wheel");

            ItemTypes = new ConfigEntry<ItemDrop.ItemData.ItemType>[6];

            ItemTypes[0] = Config.Bind(
                "Misc", 
                "ItemType1", 
                ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemTypes[1] = Config.Bind(
                "Misc", 
                "ItemType2", ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemTypes[2] = Config.Bind(
                "Misc", 
                "ItemType3", 
                ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemTypes[3] = Config.Bind(
                "Misc", 
                "ItemType4", 
                ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemTypes[4] = Config.Bind(
                "Misc", 
                "ItemType5", 
                ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemTypes[5] = Config.Bind(
                "Misc", 
                "ItemType6", 
                ItemDrop.ItemData.ItemType.None,
                "Item type used for filtering items");

            ItemRegex = Config.Bind(
                "Misc", 
                "ItemRegex", 
                "",
                "Regex used for filtering items");

            ItemRegexIgnore = Config.Bind(
                "Misc", 
                "ItemRegexIgnore", 
                "",
                "Regex used for ignoring items");

            ItemRegexCaseSensitive = Config.Bind(
                "Misc", 
                "ItemRegexCaseSensitive", 
                false,
                "When enabled the Regex will be case-sensitive");

            if (!ModEnabled.Value)
            {
                LogWarn("Mod not loaded because it was disabled via config.");
                return;
            }

            if (IsDedicated())
            {
                LogWarn("Mod not loaded because game instance is a dedicated server.");
                return;
            }

            _harmony = Harmony.CreateAndPatchAll(typeof(Patcher));

            WheelManager.AddWheel(this);
            
            try
            {
                EpicLootWrapper.CreateInstance();

                UseRarityColoring = Config.Bind(
                    "Appereance", 
                    "UseRarityColoring", 
                    true, 
                    "When enabled, the highlight color will be set to the rarity color of the selected item.");

                Log("Epicloot Mod installed. Applied compatibility patch.");
            }
            catch (Exception e)
            {
                LogErr(e.Message);
                LogErr("Failed to initialize EpicLootWrapper. Probably a compatibility issue. Please inform the mod creator of EquipmentWheel about this issue! (https://www.nexusmods.com/valheim/mods/536)");
            }

            Log(this.GetType().Namespace + " Loaded!");
        }

        void OnDestroy()
        {
            WheelManager.RemoveWheel(this);
            _harmony?.UnpatchAll();
        }

        public static bool BestMatchPressed
        {
            get
            {
                if (Instance == null)
                    return false;

                return WheelManager.BestMatchPressed(Instance);
            }
        }

        public static void TryHideHotkeyBar()
        {
            HotkeyBar hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").GetComponent<HotkeyBar>();

            if (hotKeyBar == null)
                return;

            hotKeyBar.gameObject.SetActive(!HideHotkeyBar.Value);
        }

        public static bool BestMatchDown
        {
            get
            {
                if (Instance == null)
                    return false;

                return WheelManager.BestMatchDown(Instance);
            }
        }


        public static bool IsShortcutDown
        {
            get
            {
                if (ZInput.IsGamepadActive())
                {
                    switch (HotkeyDPad.Value)
                    {
                        case WheelManager.DPadButton.None:
                            if (UseSitButton.Value)
                                return ZInput.GetButtonDown("JoySit");
                            else
                                return ZInput.GetButtonDown("JoyUse");

                        case WheelManager.DPadButton.Left:
                            return ZInput.GetButtonDown("JoyHotbarLeft");

                        case WheelManager.DPadButton.Right:
                            return ZInput.GetButtonDown("JoyHotbarRight");

                        case WheelManager.DPadButton.LeftOrRight:
                             return ZInput.GetButtonDown("JoyHotbarRight") || ZInput.GetButtonDown("JoyHotbarLeft");


                        default:
                            return ZInput.GetButtonDown("JoyHotbarRight") || ZInput.GetButtonDown("JoyHotbarLeft");
                    }
                }
                else
                {
                    var shortcut = Hotkey.Value;
                    var mainKey = shortcut.MainKey;
                    var modifierKeys = shortcut.Modifiers.ToArray();
                    return Input.GetKeyDown(mainKey) || modifierKeys.Any(Input.GetKeyDown);
                }
            }
        }

        public static bool IsShortcutUp
        {
            get
            {
                if (ZInput.IsGamepadActive())
                {
                    switch (HotkeyDPad.Value)
                    {
                        case WheelManager.DPadButton.None:
                            if (UseSitButton.Value)
                                return ZInput.GetButtonUp("JoySit");
                            else
                                return ZInput.GetButtonUp("JoyUse");

                        case WheelManager.DPadButton.Left:
                            return ZInput.GetButtonUp("JoyHotbarLeft");

                        case WheelManager.DPadButton.Right:
                            return ZInput.GetButtonUp("JoyHotbarRight");

                        case WheelManager.DPadButton.LeftOrRight:
                            return ZInput.GetButtonUp("JoyHotbarRight") || ZInput.GetButtonUp("JoyHotbarLeft");

                        default:
                            return ZInput.GetButtonUp("JoyHotbarRight") || ZInput.GetButtonUp("JoyHotbarLeft");
                    }
                }
                else
                {
                    var shortcut = Hotkey.Value;
                    var mainKey = shortcut.MainKey;
                    var modifierKeys = shortcut.Modifiers.ToArray();
                    return Input.GetKeyUp(mainKey) || modifierKeys.Any(Input.GetKeyUp);
                }

            }
        
        }

        public static bool IsShortcutPressed
        {
            get
            {

                if (ZInput.IsGamepadActive())
                {
                    switch (HotkeyDPad.Value)
                    {
                        case WheelManager.DPadButton.None:
                            if (UseSitButton.Value)
                                return ZInput.GetButton("JoySit");
                            else
                                return ZInput.GetButton("JoyUse");


                        case WheelManager.DPadButton.Left:
                            return ZInput.GetButton("JoyHotbarLeft");

                        case WheelManager.DPadButton.Right:
                            return ZInput.GetButton("JoyHotbarRight");

                        case WheelManager.DPadButton.LeftOrRight:
                            return ZInput.GetButton("JoyHotbarRight") || ZInput.GetButton("JoyHotbarLeft");

                        default:
                            return ZInput.GetButton("JoyHotbarRight") || ZInput.GetButton("JoyHotbarLeft");
                    }
                }
                else
                {
                    var shortcut = Hotkey.Value;
                    var mainKey = shortcut.MainKey;
                    var modifierKeys = shortcut.Modifiers.ToArray();
                    return Input.GetKey(mainKey) || modifierKeys.Any(Input.GetKey);
                }

            }
        }

        public static void ParseNames(string value, ref string[] arr)
        {
            if (ObjectDB.instance == null)
                return;

            var names = ParseTokens(value);
            var ids = new List<string>();

            foreach (var name in names)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(name);
                if (prefab != null)
                {
                    var item = prefab.GetComponent<ItemDrop>();
                    ids.Add(item.m_itemData.m_shared.m_name);
                }
            }

            arr = ids.Distinct().ToArray();
        }

        public static string[] ParseTokens(string value)
        {
            char[] delimiterChars = { ' ', ',', '.', ':', '\t' };
            return value.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
        }

        public int GetKeyCount(bool pressed = false)
        {
            var matches = 0;
            var hotkey = Hotkey.Value;

            var main = hotkey.MainKey;
            var mods = hotkey.Modifiers.ToArray();

            if (main == KeyCode.None)
                return matches;

            if (pressed ? Input.GetKey(main) : Input.GetKeyDown(main))
                matches++;

            matches += mods.Count(Input.GetKey);

            return matches;
        }

        public int GetKeyCountDown()
        {
            return GetKeyCount();
        }

        public int GetKeyCountPressed()
        {
            return GetKeyCount(true);
        }

        public bool IsVisible()
        {
            return EquipGui.visible;
        }

        public void Hide()
        {
            if (Gui == null)
                return;

            Gui.Hide();
        }

        public string GetName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        float WheelManager.IWheel.JoyStickIgnoreTime()
        {
            return JoyStickIgnoreTime;
        }
    }
}
