using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace EquipmentWheel
{
    public class EpicLootWrapper
    {
        private Assembly _epicLootAssembly;
        private Type _itemBackgroundHelper;
        private Type _epicLoot;
        private Type _itemDataExtensions;

        private MethodInfo _createAndGetMagicItemBackgroundImage;
        private MethodInfo _getMagicItemBackgroundSprite;
        private MethodInfo _useMagicBackground;
        private MethodInfo _getRarityColor;
        private MethodInfo _getDecoratedName;
        
        public static EpicLootWrapper Instance;


        public EpicLootWrapper(PluginInfo pluginInfo)
        {
            _epicLootAssembly = pluginInfo.Instance.GetType().Assembly;

            if (_epicLootAssembly == null)
                throw new Exception("Assembly for EpicLoot cannot be resolved");

            foreach (var type in _epicLootAssembly.GetExportedTypes())
                switch (type.FullName)
                {
                    case "EpicLoot.ItemBackgroundHelper":
                        _itemBackgroundHelper = type;
                        break;

                    case "EpicLoot.EpicLoot":
                        _epicLoot = type;
                        break;

                    case "EpicLoot.ItemDataExtensions":
                        _itemDataExtensions = type;
                        break;
                }

            if (_itemBackgroundHelper == null)
                throw new Exception("Type EpicLoot.ItemBackgroundHelper cannot be resolved");

            if (_epicLoot == null)
                throw new Exception("Type EpicLoot.EpicLoot cannot be resolved");

            if (_itemDataExtensions == null)
                throw new Exception("Type EpicLoot.ItemDataExtensions cannot be resolved");

            _createAndGetMagicItemBackgroundImage = _itemBackgroundHelper.GetMethod("CreateAndGetMagicItemBackgroundImage",
                new Type[] { typeof(GameObject), typeof(GameObject), typeof(bool) });

            if (_createAndGetMagicItemBackgroundImage == null)
                throw new Exception("Method CreateAndGetMagicItemBackgroundImage cannot be resolved");

            _getMagicItemBackgroundSprite = _epicLoot.GetMethod("GetMagicItemBgSprite", new Type[] { });

            if (_getMagicItemBackgroundSprite == null)
                throw new Exception("Method GetMagicItemBgSprite cannot be resolved");

            _useMagicBackground = _itemDataExtensions.GetMethod("UseMagicBackground", new Type[] { typeof(ItemDrop.ItemData) });

            if (_useMagicBackground == null)
                throw new Exception("Method UseMagicBackground cannot be resolved");

            _getRarityColor = _itemDataExtensions.GetMethod("GetRarityColor", new Type[] { typeof(ItemDrop.ItemData) });

            if (_getRarityColor == null)
                throw new Exception("Method GetRarityColor cannot be resolved");

            _getDecoratedName = _itemDataExtensions.GetMethod("GetDecoratedName", new Type[] { typeof(ItemDrop.ItemData), typeof(string) });

            if (_getDecoratedName == null)
                throw new Exception("Method GetDecoratedName cannot be resolved");
        }

        public static EpicLootWrapper CreateInstance()
        {
            PluginInfo pluginInfo;

            try
            {
                pluginInfo = Chainloader.PluginInfos["randyknapp.mods.epicloot"];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

            Instance = new EpicLootWrapper(pluginInfo);

            return Instance;
        }

        public string GetItemName(ItemDrop.ItemData item, Color color)
        {
            return (string)_getDecoratedName.Invoke(null, new object[] { item, "#" + ColorUtility.ToHtmlStringRGB(color) });
        }

        public string GetItemName(ItemDrop.ItemData item)
        {
            return GetItemName(item, EquipWheel.GetHighlightColor);
        }

        public Color GetItemColor(ItemDrop.ItemData item)
        {
            return (Color)_getRarityColor.Invoke(null, new object[] { item });
        }

        public void ModifyElement(EquipWheelUI.ElementData element, ItemDrop.ItemData item)
        {
            if (element == null || item == null)
                return;

            var magicItemTransform = element.m_go.transform.Find("magicItem");
            if (magicItemTransform != null)
            {
                var mi = magicItemTransform.GetComponent<Image>();
                if (mi != null)
                {
                    mi.enabled = false;
                }
            }

            var setItemTransform = element.m_go.transform.Find("setItem");
            if (setItemTransform != null)
            {
                var setItem = setItemTransform.GetComponent<Image>();
                if (setItem != null)
                {
                    setItem.enabled = false;
                }
            }

            var magicItem = (Image)_createAndGetMagicItemBackgroundImage.Invoke(null, new object[] { element.m_go, element.m_equiped.gameObject, true });

            if ((bool)_useMagicBackground.Invoke(null, new object[] { item }))
            {
                magicItem.enabled = true;
                magicItem.sprite = (Sprite)_getMagicItemBackgroundSprite.Invoke(null, new object[] { });
                magicItem.color = (Color)_getRarityColor.Invoke(null, new object[] { item });
            }

            var setItem2 = element.m_go.transform.Find("setItem");
            if (setItem2 != null && !string.IsNullOrEmpty(item.m_shared.m_setName))
            {
                var img = setItem2.GetComponent<Image>();
                img.enabled = true;
            }
        }
    }
}
