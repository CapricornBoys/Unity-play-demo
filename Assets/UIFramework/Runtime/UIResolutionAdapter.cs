using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    /// <summary>
    /// 监听分辨率、屏幕方向和安全区变化，并更新 Canvas 缩放与 Safe Area。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIResolutionAdapter : MonoBehaviour
    {
        private CanvasScaler canvasScaler;
        private RectTransform safeAreaRoot;
        private Vector2 referenceResolution;
        private UIScaleMode scaleMode;
        private float matchValue;
        private bool useSafeArea;

        private int lastWidth;
        private int lastHeight;
        private Rect lastSafeArea;

        public void Initialize(
            CanvasScaler scaler,
            RectTransform safeRoot,
            Vector2 reference,
            UIScaleMode mode,
            float match,
            bool safeAreaEnabled)
        {
            canvasScaler = scaler;
            safeAreaRoot = safeRoot;
            referenceResolution = reference;
            scaleMode = mode;
            matchValue = Mathf.Clamp01(match);
            useSafeArea = safeAreaEnabled;
            Refresh(true);
        }

        public void Configure(
            Vector2 reference,
            UIScaleMode mode,
            float match,
            bool safeAreaEnabled)
        {
            referenceResolution = reference;
            scaleMode = mode;
            matchValue = Mathf.Clamp01(match);
            useSafeArea = safeAreaEnabled;
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (canvasScaler == null || safeAreaRoot == null)
            {
                return;
            }

            Rect safeArea = useSafeArea
                ? Screen.safeArea
                : new Rect(0f, 0f, Screen.width, Screen.height);

            if (!force
                && lastWidth == Screen.width
                && lastHeight == Screen.height
                && lastSafeArea == safeArea)
            {
                return;
            }

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = safeArea;

            ApplyCanvasScale();
            ApplySafeArea(safeArea);
        }

        private void ApplyCanvasScale()
        {
            canvasScaler.referenceResolution = referenceResolution;

            switch (scaleMode)
            {
                case UIScaleMode.FixedWidth:
                    canvasScaler.matchWidthOrHeight = 0f;
                    break;
                case UIScaleMode.FixedHeight:
                    canvasScaler.matchWidthOrHeight = 1f;
                    break;
                case UIScaleMode.Expand:
                    float screenAspect = Screen.height > 0
                        ? Screen.width / (float)Screen.height
                        : 1f;
                    float referenceAspect = referenceResolution.y > 0f
                        ? referenceResolution.x / referenceResolution.y
                        : 1f;

                    // 窄屏保持参考宽度，宽屏保持参考高度，确保设计区域不会被裁切。
                    canvasScaler.matchWidthOrHeight =
                        screenAspect < referenceAspect ? 0f : 1f;
                    break;
                default:
                    canvasScaler.matchWidthOrHeight = matchValue;
                    break;
            }
        }

        private void ApplySafeArea(Rect safeArea)
        {
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= width;
            anchorMin.y /= height;
            anchorMax.x /= width;
            anchorMax.y /= height;

            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }
    }
}
