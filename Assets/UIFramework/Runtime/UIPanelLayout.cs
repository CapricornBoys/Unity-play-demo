namespace GameUI
{
    /// <summary>
    /// 界面根节点在所属 UI 层中的布局方式。
    /// </summary>
    public enum UIPanelLayout
    {
        /// <summary>
        /// 根节点铺满所属层，适用于主界面、背包和设置页。
        /// </summary>
        FullScreen = 0,

        /// <summary>
        /// 保留 Prefab 中的设计尺寸并居中，适用于弹窗。
        /// </summary>
        ContentSize = 1
    }
}
