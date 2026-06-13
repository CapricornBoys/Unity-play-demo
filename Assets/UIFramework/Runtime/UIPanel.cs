using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// UI 控制器基类。控制器由 UIManager 在预制体加载后动态挂载。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        public string PanelId { get; private set; }
        public bool IsOpen { get; private set; }

        protected CanvasGroup CanvasGroup { get; private set; }
        protected UIComponentRegistry UI { get; private set; }

        internal void Initialize(string panelId)
        {
            PanelId = panelId;
            CanvasGroup = GetComponent<CanvasGroup>();
            UI = new UIComponentRegistry(transform);
            OnInitialize();
        }

        internal void Show(object args)
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            IsOpen = true;
            OnOpen(args);
        }

        internal void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            OnClose();
            IsOpen = false;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Close()
        {
            UIManager.Instance.Close(this);
        }

        protected T GetUI<T>(string id) where T : Component
        {
            return UI.Get<T>(id);
        }

        protected bool TryGetUI<T>(string id, out T component) where T : Component
        {
            return UI.TryGet(id, out component);
        }

        protected T[] GetAllUI<T>(string id, bool includeInactive = true) where T : Component
        {
            return UI.GetAll<T>(id, includeInactive);
        }

        protected void RebuildUIRegistry()
        {
            UI.Rebuild();
        }

        /// <summary>
        /// 控制器动态挂载并完成组件注册后调用一次。
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnOpen(object args)
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}
