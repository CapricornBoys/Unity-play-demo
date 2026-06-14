using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Editor
{
    /// <summary>
    /// 生成纯视图测试预制体并自动配置 Addressables。
    /// 预制体不挂载任何 UIPanel 派生脚本。
    /// </summary>
    public static class UIFrameworkDemoPrefabBuilder
    {
        private const string PrefabFolder = "Assets/UIFramework/Demo/Prefabs";
        private const string Match3PrefabPath = PrefabFolder + "/Match3Panel.prefab";
        private const string MergeTwoPrefabPath =
            PrefabFolder + "/MergeTwoPanel.prefab";

        /// <summary>
        /// 新增示例资源尚未生成时，在脚本编译完成后自动补建一次。
        /// 已存在资源时不会重复执行，避免每次进入 Unity 都改写 Prefab。
        /// </summary>
        [DidReloadScripts]
        private static void RebuildWhenMatch3PrefabIsMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Match3PrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(MergeTwoPrefabPath) == null)
            {
                Rebuild();
            }
        }

        [MenuItem("Tools/UI Framework/Rebuild Demo Prefabs")]
        public static void Rebuild()
        {
            EnsureFolder(PrefabFolder);
            BuildMainPanel();
            BuildSettingsPanel();
            BuildDialogPanel();
            BuildInventoryPanel();
            BuildMatch3Panel();
            BuildMergeTwoPanel();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Pure-view UI prefabs rebuilt and marked as Addressable.");
        }

        private static void BuildMainPanel()
        {
            GameObject root = CreatePanelRoot(
                "MainPanel",
                new Vector2(900f, 650f),
                new Color(0.05f, 0.08f, 0.14f, 1f),
                true);

            CreateText(root.transform, "Title", "UI Framework Demo", 46,
                new Vector2(0f, 280f), new Vector2(700f, 90f));
            CreateButton(root.transform, "Match3Button", "Open Match 3",
                new Vector2(0f, 190f));
            CreateButton(root.transform, "MergeTwoButton", "Open Merge Two",
                new Vector2(0f, 100f));
            CreateButton(root.transform, "InventoryButton", "Open Inventory",
                new Vector2(0f, 10f));
            CreateButton(root.transform, "SettingsButton", "Open Settings",
                new Vector2(0f, -80f));
            CreateButton(root.transform, "DialogButton", "Open Dialog",
                new Vector2(0f, -170f));
            CreateText(root.transform, "Hint", "ESC: close top panel", 24,
                new Vector2(0f, -275f), new Vector2(600f, 60f));

            SaveAddressablePrefab(root, "MainPanel.prefab", "UI/MainPanel");
        }

        private static void BuildSettingsPanel()
        {
            GameObject root = CreatePanelRoot(
                "SettingsPanel",
                new Vector2(620f, 480f),
                new Color(0.12f, 0.16f, 0.23f, 1f),
                true);

            CreateText(root.transform, "Title", "Settings", 42,
                new Vector2(0f, 150f), new Vector2(500f, 80f));
            CreateText(root.transform, "Description", "This panel is cached.", 26,
                new Vector2(0f, 40f), new Vector2(500f, 60f));
            CreateButton(root.transform, "CloseButton", "Close",
                new Vector2(0f, -110f));

            SaveAddressablePrefab(root, "SettingsPanel.prefab", "UI/SettingsPanel");
        }

        private static void BuildDialogPanel()
        {
            GameObject root = CreatePanelRoot(
                "DialogPanel",
                new Vector2(680f, 420f),
                new Color(0.16f, 0.19f, 0.27f, 1f),
                false);

            CreateText(root.transform, "Title", "Dialog", 40,
                new Vector2(0f, 115f), new Vector2(500f, 70f));
            CreateText(root.transform, "Message", string.Empty, 24,
                new Vector2(0f, 20f), new Vector2(580f, 100f));
            CreateButton(root.transform, "ConfirmButton", "Confirm",
                new Vector2(0f, -115f));

            SaveAddressablePrefab(root, "DialogPanel.prefab", "UI/DialogPanel");
        }

        private static void BuildInventoryPanel()
        {
            GameObject root = CreatePanelRoot(
                "InventoryPanel",
                Vector2.zero,
                new Color(0.035f, 0.05f, 0.08f, 1f),
                true);

            CreateTextAnchored(
                root.transform,
                "InventoryTitle",
                "INVENTORY",
                44,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(40f, -92f),
                new Vector2(500f, -18f),
                TextAnchor.MiddleLeft);

            CreateAnchoredButton(
                root.transform,
                "CloseButton",
                "Close",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-240f, -88f),
                new Vector2(-40f, -24f));

            ScrollRect scrollRect = CreateInventoryScroll(root.transform);
            CreateDetailPanel(root.transform);

            // ScrollRect 引用在创建时已经完成，保留局部变量避免构建过程误删组件。
            EditorUtility.SetDirty(scrollRect);
            SaveAddressablePrefab(root, "InventoryPanel.prefab", "UI/InventoryPanel");
        }

        private static void BuildMatch3Panel()
        {
            GameObject root = CreatePanelRoot(
                "Match3Panel",
                Vector2.zero,
                new Color(0.025f, 0.035f, 0.065f, 1f),
                true);

            CreateTextAnchored(
                root.transform,
                "Match3Title",
                "MATCH 3",
                46,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -94f),
                new Vector2(430f, -20f),
                TextAnchor.MiddleLeft);

            CreateAnchoredButton(
                root.transform,
                "RestartButton",
                "Restart",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-450f, -88f),
                new Vector2(-250f, -24f));

            CreateAnchoredButton(
                root.transform,
                "CloseButton",
                "Close",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-230f, -88f),
                new Vector2(-30f, -24f));

            GameObject boardFrameObject = new GameObject(
                "BoardFrame",
                typeof(RectTransform),
                typeof(Image));
            RectTransform boardFrame = boardFrameObject.GetComponent<RectTransform>();
            boardFrame.SetParent(root.transform, false);
            boardFrame.anchorMin = new Vector2(0.5f, 0.5f);
            boardFrame.anchorMax = new Vector2(0.5f, 0.5f);
            boardFrame.pivot = new Vector2(0.5f, 0.5f);
            boardFrame.anchoredPosition = new Vector2(-220f, -20f);
            boardFrame.sizeDelta = new Vector2(850f, 850f);
            boardFrameObject.GetComponent<Image>().color =
                new Color(0.06f, 0.08f, 0.13f, 1f);

            GameObject boardRootObject = new GameObject(
                "BoardRoot",
                typeof(RectTransform));
            RectTransform boardRoot = boardRootObject.GetComponent<RectTransform>();
            boardRoot.SetParent(boardFrame, false);
            boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.pivot = new Vector2(0f, 1f);
            boardRoot.anchoredPosition = new Vector2(-396f, 396f);
            boardRoot.sizeDelta = new Vector2(792f, 792f);

            CreateMatch3TileTemplate(boardRoot);
            CreateMatch3Hud(root.transform);
            SaveAddressablePrefab(root, "Match3Panel.prefab", "UI/Match3Panel");
        }

        private static void BuildMergeTwoPanel()
        {
            GameObject root = CreatePanelRoot(
                "MergeTwoPanel",
                Vector2.zero,
                new Color(0.025f, 0.035f, 0.065f, 1f),
                true);

            CreateTextAnchored(
                root.transform,
                "MergeTwoTitle",
                "MERGE TWO",
                46,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -94f),
                new Vector2(430f, -20f),
                TextAnchor.MiddleLeft);

            CreateAnchoredButton(
                root.transform,
                "RestartButton",
                "Restart",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-450f, -88f),
                new Vector2(-250f, -24f));

            CreateAnchoredButton(
                root.transform,
                "CloseButton",
                "Close",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-230f, -88f),
                new Vector2(-30f, -24f));

            GameObject boardFrameObject = new GameObject(
                "BoardFrame",
                typeof(RectTransform),
                typeof(Image));
            RectTransform boardFrame = boardFrameObject.GetComponent<RectTransform>();
            boardFrame.SetParent(root.transform, false);
            boardFrame.anchorMin = new Vector2(0.5f, 0.5f);
            boardFrame.anchorMax = new Vector2(0.5f, 0.5f);
            boardFrame.pivot = new Vector2(0.5f, 0.5f);
            boardFrame.anchoredPosition = new Vector2(-220f, -20f);
            boardFrame.sizeDelta = new Vector2(850f, 850f);
            boardFrameObject.GetComponent<Image>().color =
                new Color(0.06f, 0.08f, 0.13f, 1f);

            GameObject boardRootObject = new GameObject(
                "BoardRoot",
                typeof(RectTransform));
            RectTransform boardRoot = boardRootObject.GetComponent<RectTransform>();
            boardRoot.SetParent(boardFrame, false);
            boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.pivot = new Vector2(0f, 1f);
            boardRoot.anchoredPosition = new Vector2(-366f, 366f);
            boardRoot.sizeDelta = new Vector2(732f, 732f);

            CreateMatch3TileTemplate(boardRoot);
            CreateMergeTwoHud(root.transform);
            SaveAddressablePrefab(
                root,
                "MergeTwoPanel.prefab",
                "UI/MergeTwoPanel");
        }

        private static void CreateMatch3TileTemplate(RectTransform boardRoot)
        {
            GameObject tileObject = new GameObject(
                "TileTemplate",
                typeof(RectTransform),
                typeof(Image));
            RectTransform tile = tileObject.GetComponent<RectTransform>();
            tile.SetParent(boardRoot, false);
            tile.anchorMin = new Vector2(0f, 1f);
            tile.anchorMax = new Vector2(0f, 1f);
            tile.pivot = new Vector2(0f, 1f);
            tile.sizeDelta = new Vector2(92f, 92f);
            tileObject.GetComponent<Image>().color =
                new Color(0.12f, 0.15f, 0.22f, 1f);

            GameObject gemObject = new GameObject(
                "Gem",
                typeof(RectTransform),
                typeof(Image));
            RectTransform gem = gemObject.GetComponent<RectTransform>();
            gem.SetParent(tile, false);
            gem.anchorMin = new Vector2(0.14f, 0.14f);
            gem.anchorMax = new Vector2(0.86f, 0.86f);
            gem.offsetMin = Vector2.zero;
            gem.offsetMax = Vector2.zero;
            gemObject.GetComponent<Image>().color = Color.white;
            tileObject.SetActive(false);
        }

        private static void CreateMatch3Hud(Transform parent)
        {
            GameObject hudObject = new GameObject(
                "GameHud",
                typeof(RectTransform),
                typeof(Image));
            RectTransform hud = hudObject.GetComponent<RectTransform>();
            hud.SetParent(parent, false);
            hud.anchorMin = new Vector2(0.72f, 0.18f);
            hud.anchorMax = new Vector2(0.96f, 0.78f);
            hud.offsetMin = Vector2.zero;
            hud.offsetMax = Vector2.zero;
            hudObject.GetComponent<Image>().color =
                new Color(0.07f, 0.1f, 0.16f, 1f);

            CreateTextAnchored(
                hud,
                "ScoreText",
                "Score: 0",
                36,
                new Vector2(0.08f, 0.74f),
                new Vector2(0.92f, 0.9f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                hud,
                "MovesText",
                "Moves: 0",
                30,
                new Vector2(0.08f, 0.58f),
                new Vector2(0.92f, 0.72f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                hud,
                "StatusText",
                "Match three or more tiles",
                24,
                new Vector2(0.08f, 0.18f),
                new Vector2(0.92f, 0.52f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);
        }

        private static void CreateMergeTwoHud(Transform parent)
        {
            GameObject hudObject = new GameObject(
                "GameHud",
                typeof(RectTransform),
                typeof(Image));
            RectTransform hud = hudObject.GetComponent<RectTransform>();
            hud.SetParent(parent, false);
            hud.anchorMin = new Vector2(0.72f, 0.18f);
            hud.anchorMax = new Vector2(0.96f, 0.78f);
            hud.offsetMin = Vector2.zero;
            hud.offsetMax = Vector2.zero;
            hudObject.GetComponent<Image>().color =
                new Color(0.07f, 0.1f, 0.16f, 1f);

            CreateTextAnchored(
                hud,
                "ScoreText",
                "Score: 0",
                36,
                new Vector2(0.08f, 0.74f),
                new Vector2(0.92f, 0.9f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                hud,
                "MovesText",
                "Moves: 0",
                30,
                new Vector2(0.08f, 0.58f),
                new Vector2(0.92f, 0.72f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                hud,
                "StatusText",
                "Merge two equal adjacent tiles",
                24,
                new Vector2(0.08f, 0.18f),
                new Vector2(0.92f, 0.52f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);
        }

        private static ScrollRect CreateInventoryScroll(Transform parent)
        {
            GameObject scrollObject = new GameObject(
                "InventoryScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.SetParent(parent, false);
            SetAnchors(
                scrollRectTransform,
                new Vector2(0.035f, 0.08f),
                new Vector2(0.7f, 0.86f),
                Vector2.zero,
                Vector2.zero);
            scrollObject.GetComponent<Image>().color =
                new Color(0.07f, 0.09f, 0.13f, 1f);

            GameObject viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollObject.transform, false);
            Stretch(viewport, new Vector2(18f, 18f), new Vector2(-18f, -18f));
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            CreateSlotTemplate(content);

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 42f;
            return scrollRect;
        }

        private static void CreateSlotTemplate(RectTransform content)
        {
            GameObject slotObject = new GameObject(
                "SlotTemplate",
                typeof(RectTransform),
                typeof(Image));
            RectTransform slot = slotObject.GetComponent<RectTransform>();
            slot.SetParent(content, false);
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.sizeDelta = new Vector2(150f, 150f);
            slotObject.GetComponent<Image>().color =
                new Color(0.16f, 0.2f, 0.28f, 1f);

            GameObject iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image));
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.SetParent(slot, false);
            icon.anchorMin = new Vector2(0.18f, 0.28f);
            icon.anchorMax = new Vector2(0.82f, 0.92f);
            icon.offsetMin = Vector2.zero;
            icon.offsetMax = Vector2.zero;
            iconObject.GetComponent<Image>().color = Color.white;

            CreateTextAnchored(
                slot,
                "Name",
                string.Empty,
                17,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.28f),
                new Vector2(6f, 4f),
                new Vector2(-6f, 0f),
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                slot,
                "Count",
                string.Empty,
                18,
                new Vector2(0.58f, 0.7f),
                new Vector2(0.96f, 0.96f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.UpperRight);

            slotObject.SetActive(false);
        }

        private static void CreateDetailPanel(Transform parent)
        {
            GameObject detailObject = new GameObject(
                "DetailPanel",
                typeof(RectTransform),
                typeof(Image));
            RectTransform detail = detailObject.GetComponent<RectTransform>();
            detail.SetParent(parent, false);
            SetAnchors(
                detail,
                new Vector2(0.73f, 0.12f),
                new Vector2(0.965f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            detailObject.GetComponent<Image>().color =
                new Color(0.09f, 0.12f, 0.18f, 1f);

            GameObject iconObject = new GameObject(
                "DetailIcon",
                typeof(RectTransform),
                typeof(Image));
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.SetParent(detail, false);
            icon.anchorMin = new Vector2(0.5f, 1f);
            icon.anchorMax = new Vector2(0.5f, 1f);
            icon.pivot = new Vector2(0.5f, 1f);
            icon.anchoredPosition = new Vector2(0f, -48f);
            icon.sizeDelta = new Vector2(190f, 190f);

            CreateTextAnchored(
                detail,
                "DetailName",
                "Select an item",
                32,
                new Vector2(0.08f, 0.54f),
                new Vector2(0.92f, 0.68f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                detail,
                "DetailCount",
                string.Empty,
                24,
                new Vector2(0.08f, 0.45f),
                new Vector2(0.92f, 0.54f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);

            CreateTextAnchored(
                detail,
                "DetailDescription",
                "Click to view details. Long press and drag to exchange positions.",
                22,
                new Vector2(0.08f, 0.08f),
                new Vector2(0.92f, 0.43f),
                Vector2.zero,
                Vector2.zero,
                TextAnchor.UpperLeft);
        }

        private static GameObject CreatePanelRoot(
            string name,
            Vector2 size,
            Color color,
            bool fullScreen)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            if (fullScreen)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
            }
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static void CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280f, 70f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.45f, 0.78f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(
                buttonObject.transform,
                "Label",
                label,
                28,
                Vector2.zero,
                rect.sizeDelta);
            text.raycastTarget = false;
        }

        private static Button CreateAnchoredButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax, offsetMin, offsetMax);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.65f, 0.2f, 0.22f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateTextAnchored(
                buttonObject.transform,
                "Label",
                label,
                24,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            return button;
        }

        private static Text CreateTextAnchored(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax, offsetMin, offsetMax);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
        }

        private static void SaveAddressablePrefab(
            GameObject root,
            string fileName,
            string address)
        {
            string path = $"{PrefabFolder}/{fileName}";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(true);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetEntry entry =
                settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
