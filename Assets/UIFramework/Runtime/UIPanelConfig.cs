using System;

namespace GameUI
{
    /// <summary>
    /// 单个界面的注册信息。
    /// </summary>
    public sealed class UIPanelConfig
    {
        public string Id { get; }
        public string Address { get; }
        public Type PanelType { get; }
        public UILayer Layer { get; }
        public bool Cache { get; }
        public bool Modal { get; }
        public UIPanelLayout Layout { get; }

        public UIPanelConfig(
            string id,
            string address,
            Type panelType,
            UILayer layer,
            bool cache,
            bool modal,
            UIPanelLayout layout)
        {
            Id = id;
            Address = address;
            PanelType = panelType;
            Layer = layer;
            Cache = cache;
            Modal = modal;
            Layout = layout;
        }
    }
}
