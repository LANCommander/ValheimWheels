using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace EquipmentWheel
{
    public class EquipWheelUI : MonoBehaviour
    {
        /* Contants */
        public readonly float ANGLE_STEP = 360f / 8f;
        public readonly float ITEM_DISTANCE = 295f;
        public readonly float ITEM_SCALE = 2f;
        public readonly float INNER_DIAMETER = 340f;
        public readonly float LENGTH_THRESHOLD = 0.1f;

        private GameObject cursor;
        private Text text;
        private GameObject highlight;
        private readonly ItemDrop.ItemData[] items = new ItemDrop.ItemData[8];
        private HotkeyBar hotKeyBar;
        private Transform itemsRoot;
        private bool addedListener = false;

        public class ElementData
        {
            public bool m_used;
            public GameObject m_go;
            public Image m_icon;
            public GuiBar m_durability;
            public TextMeshProUGUI m_amount;
            public GameObject m_equiped;
            public GameObject m_queued;
            public GameObject m_selection;
        }

        /* Definitions from original Valheim code. (They weren't public)*/
        private readonly List<ElementData> m_elements = new List<ElementData>();
        private GameObject m_elementPrefab;

        private int previous = -1;
        public int Current
        {
            get
            {
                if ((!ZInput.IsGamepadActive() && MouseInCenter) || JoyStickInCenter || (ZInput.IsGamepadActive() && Lenght < LENGTH_THRESHOLD))
                    return -1;

                int index = Mod((int)Mathf.Round((-Angle) / ANGLE_STEP), 8);

                if (index >= items.Length)
                    return -1;

                return index;
            }
        }

        public bool JoyStickInCenter
        {
            get
            {
                if (EquipWheel.HotkeyDPad.Value == WheelManager.DPadButton.None)
                {
                    var x = ZInput.GetJoyLeftStickX();
                    var y = ZInput.GetJoyLeftStickY();
                    return ZInput.IsGamepadActive() && x == 0 && y == 0;
                }
                else
                {
                    var x = ZInput.GetJoyRightStickX();
                    var y = ZInput.GetJoyRightStickY();
                    return ZInput.IsGamepadActive() && x == 0 && y == 0;
                }
            }
        }

        public ItemDrop.ItemData CurrentItem
        {
            get
            {
                if (Current < 0)
                    return null;

                return items[Current];
            }
        }

        public bool MouseInCenter
        {
            get
            {
                float radius = INNER_DIAMETER / 2 * gameObject.transform.lossyScale.x;
                var dir = Input.mousePosition - cursor.transform.position;
                return dir.magnitude <= radius;
            }
        }

        public double Lenght
        {
            get
            {
                if (EquipWheel.HotkeyDPad.Value == WheelManager.DPadButton.None)
                {

                    var x = ZInput.GetJoyLeftStickX();
                    var y = ZInput.GetJoyLeftStickY();

                    return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

                }
                else
                {
                    var x = ZInput.GetJoyRightStickX();
                    var y = ZInput.GetJoyRightStickY();

                    return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
                }
            }
        }

        public float Angle
        {
            get
            {
                if (ZInput.IsGamepadActive())
                {

                    if (EquipWheel.HotkeyDPad.Value == WheelManager.DPadButton.None)
                    {

                        var x = ZInput.GetJoyLeftStickX();
                        var y = -ZInput.GetJoyLeftStickY();

                        if (x != 0 || y != 0)
                            return Mathf.Atan2(y, x) * Mathf.Rad2Deg - 90;
                    }
                    else
                    {
                        var x = ZInput.GetJoyRightStickX();
                        var y = -ZInput.GetJoyRightStickY();

                        if (x != 0 || y != 0)
                            return Mathf.Atan2(y, x) * Mathf.Rad2Deg - 90;
                    }
                }


                var dir = Input.mousePosition - cursor.transform.position;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
                return angle;
            }
        }

        public IEnumerator FlashCoroutine(float aTime)
        {
            var color = EquipWheel.GetHighlightColor;

            for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / aTime)
            {
                highlight.GetComponent<Image>().color = Color.Lerp(Color.white, color, t);
                yield return null;
            }
        }

        private int Mod(int a, int b)
        {
            return (a % b + b) % b;
        }

        public void Flash()
        {
            StartCoroutine(FlashCoroutine(0.4f));
        }

        private void Awake()
        {
            cursor = transform.Find("Cursor").gameObject;
            highlight = transform.Find("Highlight").gameObject;

            hotKeyBar = Hud.instance.transform.Find("hudroot/HotKeyBar").gameObject.GetComponent<HotkeyBar>();

            m_elementPrefab = hotKeyBar.m_elementPrefab;

            itemsRoot = transform.Find("Items");

            var mat = highlight.GetComponent<Image>().material;
            mat.SetFloat("_Degree", ANGLE_STEP);

            var textGo = new GameObject("Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(transform);
            textRect.sizeDelta = new Vector2(1000, 100);
            textRect.anchoredPosition = new Vector2(0, 450);

            text = textGo.AddComponent<Text>();
            text.color = EquipWheel.GetHighlightColor;

            EquipWheel.HighlightColor.SettingChanged += (sender, args) => text.color = EquipWheel.GetHighlightColor;

            text.alignment = TextAnchor.UpperCenter;
            text.fontSize = 60;
            text.supportRichText = true;

            foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font.name == "AveriaSerifLibre-Bold")
                {
                    text.font = font;
                    break;
                }
            }

            var outline = textGo.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1, -1);
            outline.effectColor = Color.black;

            textGo.SetActive(false);
        }

        private void Start()
        {
            if (!addedListener)
            {
                var player = Player.m_localPlayer;
                var inventory = player.GetInventory();

                inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnInventoryChanged));
                addedListener = true;
            }
        }

        public void OnInventoryChanged()
        {
            if (!EquipGui.Visible)
            {
                return;
            }


            UpdateItems();
            UpdateIcons(Player.m_localPlayer, true);
        }

        void UpdateItems()
        {
            if (Player.m_localPlayer == null)
                return;

            var inventory = Player.m_localPlayer.GetInventory();

            if (inventory == null)
                return;

            if (EquipWheel.ItemFiltering.Value)
            {
                var namedItems = new List<ItemDrop.ItemData>();
                var filteredItems = new List<ItemDrop.ItemData>();


                foreach (var item in inventory.GetAllItems())
                {

                    if (EquipWheel.ItemRegexIgnore.Value.Length > 0)
                    {
                        var regexPattern = new Regex(EquipWheel.ItemRegexIgnore.Value, EquipWheel.ItemRegexCaseSensitive.Value ? RegexOptions.None : RegexOptions.IgnoreCase);

                        string itemName;

                        if (EpicLootWrapper.Instance != null)
                        {
                            var itemColor = EpicLootWrapper.Instance.GetItemColor(item);
                            itemName = Localization.instance.Localize(EpicLootWrapper.Instance.GetItemName(item, itemColor));
                        }
                        else
                        {
                            itemName = Localization.instance.Localize(item.m_shared.m_name);
                        }

                        if (regexPattern.IsMatch(itemName))
                        {
                            continue;
                        }
                    }

                    if (EquipWheel.ItemRegex.Value.Length > 0)
                    {
                        var regexPattern = new Regex(EquipWheel.ItemRegex.Value, EquipWheel.ItemRegexCaseSensitive.Value ? RegexOptions.None : RegexOptions.IgnoreCase);

                        string itemName;

                        if (EpicLootWrapper.Instance != null)
                        {
                            var itemColor = EpicLootWrapper.Instance.GetItemColor(item);
                            itemName = Localization.instance.Localize(EpicLootWrapper.Instance.GetItemName(item, itemColor));
                        }
                        else
                        {
                            itemName = Localization.instance.Localize(item.m_shared.m_name);
                        }

                        if (regexPattern.IsMatch(itemName))
                        {
                            namedItems.Add(item);
                            continue;
                        }
                    }

                    var type = item.m_shared.m_itemType;

                    foreach (var itemType in EquipWheel.ItemTypes)
                        if (type == itemType.Value)
                            filteredItems.Add(item);
                }

                filteredItems
                    .Sort((a, b) => Array.IndexOf(EquipWheel.ItemTypes, a.m_shared.m_itemType)
                    .CompareTo(Array.IndexOf(EquipWheel.ItemTypes, b.m_shared.m_itemType)));

                for (int index = 0; index < 8; index++)
                {
                    items[index] = null;

                    if (namedItems.Count > index)
                    {
                        items[index] = namedItems[index];
                        continue;
                    }

                    if (filteredItems.Count > index - namedItems.Count)
                        items[index] = filteredItems[index - namedItems.Count];
                }

                return;
            }


            for (int index = 0; index < 8; index++)
            {
                items[index] = null;
                var item = inventory.GetItemAt(index, EquipWheel.InventoryRow.Value - 1);

                if (item != null)
                    items[index] = item;
            }
        }

        void OnEnable()
        {
            highlight.GetComponent<Image>().color = EquipWheel.GetHighlightColor;

            var scale = EquipWheel.GuiScale.Value;

            GetComponent<RectTransform>().localScale = new Vector3(scale, scale, scale);

            EquipWheel.JoyStickIgnoreTime = 0;

            UpdateItems();
            UpdateIcons(Player.m_localPlayer, true);
            Update();
        }

        void OnDisable()
        {
            highlight.SetActive(false);
            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                image.color = new Color(0, 0, 0, 0.5f);

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            if (Current != previous)
            {
                if (CurrentItem != null)
                {
                    InventoryGui.instance.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
                    text.gameObject.SetActive(true);

                    if (EpicLootWrapper.Instance != null)
                    {
                        var itemColor = EpicLootWrapper.Instance.GetItemColor(CurrentItem);

                        if (itemColor.Equals(Color.white) || !EquipWheel.UseRarityColoring.Value)
                            itemColor = EquipWheel.GetHighlightColor;


                        text.text = Localization.instance.Localize(EpicLootWrapper.Instance.GetItemName(CurrentItem, itemColor));
                    }
                    else
                    {
                        text.text = Localization.instance.Localize(CurrentItem.m_shared.m_name);
                    }
                }
                else
                {
                    text.gameObject.SetActive(false);
                }
                previous = Current;
            }

            highlight.SetActive(CurrentItem != null);


            var color = CurrentItem == null ? new Color(0, 0, 0, 0.5f) : EquipWheel.GetHighlightColor;

            if (CurrentItem != null && EpicLootWrapper.Instance != null)
            {
                var itemColor = EpicLootWrapper.Instance.GetItemColor(CurrentItem);

                if (!itemColor.Equals(Color.white) && EquipWheel.UseRarityColoring.Value)
                {
                    color = itemColor;
                    highlight.GetComponent<Image>().color = color;
                }
                else
                {
                    highlight.GetComponent<Image>().color = EquipWheel.GetHighlightColor;
                }
            }

            var images = cursor.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {

                image.color = color;

                if (image.gameObject.name == "Image")
                {
                    image.gameObject.SetActive(CurrentItem != null);
                }
            }

            cursor.transform.rotation = Quaternion.AngleAxis(Angle, Vector3.forward);
            var highlightAngle = Current * ANGLE_STEP;
            highlight.transform.rotation = Quaternion.AngleAxis(-highlightAngle, Vector3.forward);

            UpdateIcons(Player.m_localPlayer);
        }

        private int CountItems(ItemDrop.ItemData[] items)
        {
            int count = 0;

            foreach (var item in items)
            {
                if (item != null)
                    count++;
            }

            return count;
        }


        /* Modified UpdateIcons Function from HotkeyBar class */
        private void UpdateIcons(Player player, bool forceUpdate = false)
        {
            if (!player || player.IsDead())
            {
                foreach (ElementData elementData in this.m_elements)
                {
                    UnityEngine.Object.Destroy(elementData.m_go);
                }
                this.m_elements.Clear();
                return;
            }

            if (this.m_elements.Count != CountItems(items) || forceUpdate)
            {
                foreach (ElementData elementData2 in this.m_elements)
                {
                    UnityEngine.Object.Destroy(elementData2.m_go);
                }
                this.m_elements.Clear();
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                        continue;

                    ElementData elementData3 = new ElementData();
                    elementData3.m_go = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, itemsRoot);
                    elementData3.m_go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

                    var x = Mathf.Sin(i * ANGLE_STEP * Mathf.Deg2Rad) * ITEM_DISTANCE;
                    var y = Mathf.Cos(i * ANGLE_STEP * Mathf.Deg2Rad) * ITEM_DISTANCE;

                    elementData3.m_go.transform.localScale = new Vector3(ITEM_SCALE, ITEM_SCALE, ITEM_SCALE);
                    elementData3.m_go.transform.localPosition = new Vector3(x, y, 0f);
                    elementData3.m_go.transform.Find("binding").GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                    elementData3.m_icon = elementData3.m_go.transform.transform.Find("icon").GetComponent<Image>();
                    elementData3.m_durability = elementData3.m_go.transform.Find("durability").GetComponent<GuiBar>();
                    elementData3.m_amount = elementData3.m_go.transform.Find("amount").GetComponent<TextMeshProUGUI>();
                    elementData3.m_equiped = elementData3.m_go.transform.Find("equiped").gameObject;
                    elementData3.m_queued = elementData3.m_go.transform.Find("queued").gameObject;
                    elementData3.m_selection = elementData3.m_go.transform.Find("selected").gameObject;
                    elementData3.m_selection.SetActive(false);

                    if (EquipWheel.InventoryRow.Value > 1 || EquipWheel.ItemFiltering.Value)
                    {
                        elementData3.m_go.transform.Find("binding").GetComponent<TextMeshProUGUI>().enabled = false;
                    }

                    this.m_elements.Add(elementData3);
                }
            }
            foreach (ElementData elementData4 in this.m_elements)
            {
                elementData4.m_used = false;
            }

            int elem_index = 0;
            for (int j = 0; j < this.items.Length; j++)
            {
                if (this.items[j] == null)
                    continue;

                ItemDrop.ItemData itemData2 = this.items[j];
                ElementData elementData5 = this.m_elements[elem_index];
                elementData5.m_used = true;
                elementData5.m_icon.gameObject.SetActive(true);
                elementData5.m_icon.sprite = itemData2.GetIcon();
                elementData5.m_durability.gameObject.SetActive(itemData2.m_shared.m_useDurability);
                if (itemData2.m_shared.m_useDurability)
                {
                    if (itemData2.m_durability <= 0f)
                    {
                        elementData5.m_durability.SetValue(1f);
                        elementData5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
                    }
                    else
                    {
                        elementData5.m_durability.SetValue(itemData2.GetDurabilityPercentage());
                        elementData5.m_durability.ResetColor();
                    }
                }
                elementData5.m_equiped.SetActive(itemData2.m_equipped);
                //elementData5.m_queued.SetActive(player.IsItemQueued(itemData2))
                elementData5.m_queued.SetActive(player.IsEquipActionQueued(itemData2));
                if (itemData2.m_shared.m_maxStackSize > 1)
                {
                    elementData5.m_amount.gameObject.SetActive(true);
                    elementData5.m_amount.text = itemData2.m_stack.ToString() + "/" + itemData2.m_shared.m_maxStackSize.ToString();
                }
                else
                {
                    elementData5.m_amount.gameObject.SetActive(false);
                }

                if (EpicLootWrapper.Instance != null)
                    EpicLootWrapper.Instance.ModifyElement(elementData5, itemData2);

                elem_index++;
            }
            for (int k = 0; k < this.m_elements.Count; k++)
            {
                ElementData elementData6 = this.m_elements[k];
                if (!elementData6.m_used)
                {
                    elementData6.m_icon.gameObject.SetActive(false);
                    elementData6.m_durability.gameObject.SetActive(false);
                    elementData6.m_equiped.SetActive(false);
                    elementData6.m_queued.SetActive(false);
                    elementData6.m_amount.gameObject.SetActive(false);
                }
            }
        }
    }
}
