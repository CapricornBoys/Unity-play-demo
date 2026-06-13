namespace GameUI
{
    /// <summary>
    /// UI 层级从低到高排列。
    /// 不同类型的界面放在固定层级中，避免手动维护 Sorting Order。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal = 100,
        Popup = 200,
        Tips = 300,
        Top = 400
    }
}
