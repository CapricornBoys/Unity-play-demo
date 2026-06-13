using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace GameUI
{
    /// <summary>
    /// UI 框架入口。
    /// Prefab 只保存视图结构，具体 UIPanel 控制器在加载完成后动态挂载。
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        private readonly Dictionary<string, UIPanelConfig> configs =
            new Dictionary<string, UIPanelConfig>();
        private readonly Dictionary<string, UIPanel> cachedPanels =
            new Dictionary<string, UIPanel>();
        private readonly Dictionary<string, Task<UIPanel>> loadingPanels =
            new Dictionary<string, Task<UIPanel>>();
        private readonly HashSet<UIPanel> addressablePanels =
            new HashSet<UIPanel>();
        private readonly Dictionary<UILayer, RectTransform> layerRoots =
            new Dictionary<UILayer, RectTransform>();
        private readonly Dictionary<UIPanel, GameObject> modalMasks =
            new Dictionary<UIPanel, GameObject>();
        private readonly List<UIPanel> openStack = new List<UIPanel>();

        private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        private UIScaleMode scaleMode = UIScaleMode.Expand;
        private float matchWidthOrHeight = 0.5f;
        private bool useSafeArea = true;
        private UIResolutionAdapter resolutionAdapter;

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<UIManager>();
                }

                if (instance == null)
                {
                    instance = new GameObject("[UIManager]").AddComponent<UIManager>();
                }

                return instance;
            }
        }

        public int OpenPanelCount => openStack.Count;

        /// <summary>
        /// 修改全局分辨率适配参数。可在打开首个界面前或运行时调用。
        /// </summary>
        public void ConfigureAdaptation(
            Vector2 reference,
            UIScaleMode mode = UIScaleMode.Expand,
            float match = 0.5f,
            bool safeAreaEnabled = true)
        {
            if (reference.x <= 0f || reference.y <= 0f)
            {
                throw new ArgumentException("UI 参考分辨率必须大于 0。", nameof(reference));
            }

            referenceResolution = reference;
            scaleMode = mode;
            matchWidthOrHeight = Mathf.Clamp01(match);
            useSafeArea = safeAreaEnabled;
            resolutionAdapter?.Configure(
                reference,
                scaleMode,
                matchWidthOrHeight,
                useSafeArea);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUIRoot();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseTop();
            }
        }

        /// <summary>
        /// 注册 Addressable 视图预制体及其运行时控制器类型。
        /// </summary>
        public void Register<TPanel>(
            string id,
            string address,
            UILayer layer = UILayer.Normal,
            bool cache = true,
            bool modal = false,
            UIPanelLayout layout = UIPanelLayout.FullScreen) where TPanel : UIPanel
        {
            RegisterInternal(new UIPanelConfig(
                id,
                address,
                typeof(TPanel),
                layer,
                cache,
                modal,
                layout));
        }

        public async Task<UIPanel> OpenAsync(string id, object args = null)
        {
            if (!configs.TryGetValue(id, out UIPanelConfig config))
            {
                Debug.LogError($"UI 未注册：{id}");
                return null;
            }

            UIPanel panel = await GetOrCreatePanelAsync(config);
            if (panel == null)
            {
                return null;
            }

            openStack.Remove(panel);
            panel.transform.SetAsLastSibling();

            if (config.Modal)
            {
                CreateOrMoveModalMask(panel);
                panel.transform.SetAsLastSibling();
            }

            openStack.Add(panel);
            panel.Show(args);
            return panel;
        }

        public async Task<T> OpenAsync<T>(string id, object args = null) where T : UIPanel
        {
            return await OpenAsync(id, args) as T;
        }

        public void Close(string id)
        {
            if (cachedPanels.TryGetValue(id, out UIPanel cached))
            {
                Close(cached);
                return;
            }

            for (int i = openStack.Count - 1; i >= 0; i--)
            {
                if (openStack[i].PanelId == id)
                {
                    Close(openStack[i]);
                    return;
                }
            }
        }

        public void Close(UIPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            openStack.Remove(panel);
            RemoveModalMask(panel);
            panel.Hide();

            if (configs.TryGetValue(panel.PanelId, out UIPanelConfig config) && !config.Cache)
            {
                ReleasePanelInstance(panel);
            }
        }

        public bool CloseTop()
        {
            if (openStack.Count == 0)
            {
                return false;
            }

            Close(openStack[openStack.Count - 1]);
            return true;
        }

        public void CloseAll()
        {
            while (openStack.Count > 0)
            {
                Close(openStack[openStack.Count - 1]);
            }
        }

        public bool IsOpen(string id)
        {
            for (int i = 0; i < openStack.Count; i++)
            {
                if (openStack[i].PanelId == id && openStack[i].IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Unload(string id)
        {
            if (!cachedPanels.TryGetValue(id, out UIPanel panel))
            {
                return false;
            }

            if (panel.IsOpen)
            {
                Close(panel);
            }

            cachedPanels.Remove(id);
            ReleasePanelInstance(panel);
            return true;
        }

        public void UnloadAllClosed()
        {
            List<string> ids = new List<string>(cachedPanels.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                if (!cachedPanels[ids[i]].IsOpen)
                {
                    Unload(ids[i]);
                }
            }
        }

        private void RegisterInternal(UIPanelConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                throw new ArgumentException("UI Id 不能为空。");
            }

            if (string.IsNullOrWhiteSpace(config.Address))
            {
                throw new ArgumentException($"UI {config.Id} 的 Addressables 地址不能为空。");
            }

            if (config.PanelType == null
                || config.PanelType.IsAbstract
                || !typeof(UIPanel).IsAssignableFrom(config.PanelType))
            {
                throw new ArgumentException($"UI {config.Id} 的控制器类型无效。");
            }

            configs[config.Id] = config;
        }

        private async Task<UIPanel> GetOrCreatePanelAsync(UIPanelConfig config)
        {
            if (config.Cache && cachedPanels.TryGetValue(config.Id, out UIPanel cached))
            {
                return cached;
            }

            if (loadingPanels.TryGetValue(config.Id, out Task<UIPanel> loadingTask))
            {
                return await loadingTask;
            }

            Task<UIPanel> createTask = CreatePanelAsync(config);
            loadingPanels.Add(config.Id, createTask);

            try
            {
                return await createTask;
            }
            finally
            {
                loadingPanels.Remove(config.Id);
            }
        }

        private async Task<UIPanel> CreatePanelAsync(UIPanelConfig config)
        {
            RectTransform parent = layerRoots[config.Layer];
            AsyncOperationHandle<GameObject> handle =
                Addressables.InstantiateAsync(config.Address, parent, false);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"UI Addressables 加载失败：{config.Address}");
                Addressables.Release(handle);
                return null;
            }

            GameObject instanceObject = handle.Result;
            ApplyPanelLayout(instanceObject.GetComponent<RectTransform>(), config.Layout);

            UIPanel prefabPanel = instanceObject.GetComponent<UIPanel>();
            if (prefabPanel != null)
            {
                Debug.LogError(
                    $"UI 预制体 {config.Address} 不应挂载 {prefabPanel.GetType().Name}。"
                    + "控制器由 UIManager 动态挂载。");
                Addressables.ReleaseInstance(instanceObject);
                return null;
            }

            // AddComponent 会根据 UIPanel 上的 RequireComponent 自动补齐 CanvasGroup。
            UIPanel panel = instanceObject.AddComponent(config.PanelType) as UIPanel;
            if (panel == null)
            {
                Debug.LogError($"UI {config.Id} 动态挂载 {config.PanelType.Name} 失败。");
                Addressables.ReleaseInstance(instanceObject);
                return null;
            }

            addressablePanels.Add(panel);
            panel.Initialize(config.Id);

            if (config.Cache)
            {
                cachedPanels[config.Id] = panel;
            }

            return panel;
        }

        private static void ApplyPanelLayout(
            RectTransform rect,
            UIPanelLayout layout)
        {
            if (rect == null)
            {
                return;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            if (layout == UIPanelLayout.FullScreen)
            {
                StretchToParent(rect);
                return;
            }

            // 弹窗保留 Prefab 设计尺寸，但无论原始锚点如何都在父层居中。
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void ReleasePanelInstance(UIPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            if (addressablePanels.Remove(panel))
            {
                Addressables.ReleaseInstance(panel.gameObject);
            }
            else
            {
                Destroy(panel.gameObject);
            }
        }

        private void BuildUIRoot()
        {
            GameObject canvasObject = new GameObject(
                "[UIRoot]",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100f;

            GameObject safeAreaObject = new GameObject("[SafeArea]", typeof(RectTransform));
            RectTransform safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
            safeAreaRect.SetParent(canvasObject.transform, false);
            StretchToParent(safeAreaRect);

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerObject = new GameObject(layer.ToString(), typeof(RectTransform));
                RectTransform rect = layerObject.GetComponent<RectTransform>();

                // 背景铺满完整屏幕，交互界面放入安全区。
                Transform parent = layer == UILayer.Background
                    ? canvasObject.transform
                    : safeAreaRect;
                rect.SetParent(parent, false);
                StretchToParent(rect);
                layerRoots.Add(layer, rect);
            }

            // Safe Area 必须位于背景层之后，保证其中的交互界面最后绘制。
            safeAreaRect.SetAsLastSibling();

            resolutionAdapter = canvasObject.AddComponent<UIResolutionAdapter>();
            resolutionAdapter.Initialize(
                scaler,
                safeAreaRect,
                referenceResolution,
                scaleMode,
                matchWidthOrHeight,
                useSafeArea);

            EnsureEventSystem();
        }

        private void CreateOrMoveModalMask(UIPanel panel)
        {
            if (!modalMasks.TryGetValue(panel, out GameObject mask))
            {
                mask = new GameObject("ModalMask", typeof(RectTransform), typeof(Image));
                mask.transform.SetParent(panel.transform.parent, false);
                StretchToParent(mask.GetComponent<RectTransform>());

                Image image = mask.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.65f);
                image.raycastTarget = true;
                modalMasks.Add(panel, mask);
            }

            mask.SetActive(true);
            mask.transform.SetAsLastSibling();
        }

        private void RemoveModalMask(UIPanel panel)
        {
            if (!modalMasks.TryGetValue(panel, out GameObject mask))
            {
                return;
            }

            modalMasks.Remove(panel);
            Destroy(mask);
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject(
                "[EventSystem]",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
