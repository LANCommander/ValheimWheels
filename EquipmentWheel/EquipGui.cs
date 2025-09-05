using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EquipmentWheel
{
    public class EquipGui : MonoBehaviour
    {
        private EquipWheelUI ui;

        public AssetBundle Assets;

        public static bool Visible = false;
        public int ToggleVisible = 0;
        public static bool ToggleDownWasPressed = false;


        void Awake()
        {
            LoadAssets();
            GameObject uiPrefab = Assets.LoadAsset<GameObject>("assets/selectionwheel/selectionwheel.prefab");
            var rect = gameObject.AddComponent<RectTransform>();

            var go = Instantiate<GameObject>(uiPrefab, new Vector3(0, 0, 0), transform.rotation, rect);
            ui = (go.AddComponent<EquipWheelUI>());
            go.SetActive(false);

            Visible = false;
            Assets.Unload(false);
        }

        void Start()
        {
            var rect = gameObject.GetComponent<RectTransform>();

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0, 0);

            if (Hud.instance == null)
                return;


            EquipWheel.TryHideHotkeyBar();
        }

        private void LoadAssets()
        {
            Assembly asm = Assembly.GetAssembly(typeof(EquipWheel));
            Stream wheelAssets = asm.GetManifestResourceStream("EquipmentWheel.res.selectionwheel");

            using (MemoryStream mStream = new MemoryStream())
            {
                wheelAssets.CopyTo(mStream);
                Assets = AssetBundle.LoadFromMemory(mStream.ToArray());
            }
        }

        public void Hide()
        {
            ui.gameObject.SetActive(false);
            Visible = false;
            ToggleVisible = 0;
        }

        private void Update()
        {
            if (EquipWheel.ToggleMenu.Value)
                HandleToggleEnabled();
            else
                HandleToggleDisabled();
        }

        private void ReduceJoyStickIgnoreTime()
        {
            if (EquipWheel.JoyStickIgnoreTime > 0)
                EquipWheel.JoyStickIgnoreTime -= Time.deltaTime;
        }

        private void HandlePressedHovering()
        {
            if (WheelManager.PressedOnHovering)
            {
                if (ZInput.GetButtonUp("JoyUse"))
                {
                    WheelManager.PressedOnHovering = false;
                    WheelManager.HoverTextVisible = false;
                    return;
                }
            }

            if (EquipWheel.IsShortcutDown && EquipWheel.IsUsingUseButton())
            {
                WheelManager.PressedOnHovering = WheelManager.PressedOnHovering || WheelManager.HoverTextVisible;
            }
        }

        private void HandleToggleEnabled()
        {

            ReduceJoyStickIgnoreTime();
            HandlePressedHovering();

            if (!EquipWheel.CanOpenMenu)
            {
                Hide();
                return;
            }

            if (ToggleDownWasPressed)
            {
                if (EquipWheel.IsShortcutUp)
                {
                    Hide();
                    ToggleDownWasPressed = false;
                    return;
                }

                return;
            }

            if (EquipWheel.IsShortcutDown && ToggleVisible < 2)
                ToggleVisible++;

            var toggleDown = EquipWheel.IsShortcutDown && ToggleVisible == 2;

            if (EquipWheel.TriggerOnRelease.Value && toggleDown)
            {
                if (WheelManager.IsActive(EquipWheel.Instance))
                {
                    UseCurrentItem();
                    ToggleDownWasPressed = true;
                    return;
                }
            }

            if (ToggleVisible > 0 && !toggleDown)
            {
                if (!Visible)
                {
                    ui.gameObject.SetActive(true);
                    Visible = true;
                    WheelManager.Activate(EquipWheel.Instance);
                }


                if (EquipWheel.TriggerOnClick.Value && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0)))
                {
                    UseCurrentItem(true);
                }

                return;
            }
            Hide();
        }

        private void UseCurrentItem(bool flash = false)
        {
            if (ui.CurrentItem != null)
            {
                if (flash)
                    ui.Flash();

                Player localPlayer = Player.m_localPlayer;
                localPlayer.UseItem(null, ui.CurrentItem, false);
                EquipWheel.JoyStickIgnoreTime = EquipWheel.IgnoreJoyStickDuration.Value / 1000f;
            }
        }

        private void HandleToggleDisabled()
        {

            ReduceJoyStickIgnoreTime();
            HandlePressedHovering();

            if (!EquipWheel.CanOpenMenu)
            {
                Hide();
                return;
            }

            if (EquipWheel.TriggerOnRelease.Value && EquipWheel.IsShortcutUp)
            {
                UseCurrentItem();
                Hide();
                return;
            }

            if ((EquipWheel.IsShortcutPressed && ZInput.IsGamepadActive())
                || (EquipWheel.IsShortcutPressed && (EquipWheel.BestMatchPressed
                || EquipWheel.BestMatchDown)))
            {


                if (!Visible)
                {
                    ui.gameObject.SetActive(true);
                    Visible = true;
                    WheelManager.Activate(EquipWheel.Instance);
                }


                if (EquipWheel.TriggerOnClick.Value && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0)))
                {
                    UseCurrentItem(true);
                }

                return;
            }

            Hide();
        }

    }
}
