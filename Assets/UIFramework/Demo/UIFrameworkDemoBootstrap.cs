namespace GameUI.Demo
{
    /// <summary>
    /// 演示入口：通过 Addressables 地址注册三个预制体界面。
    /// </summary>
    public sealed class UIFrameworkDemoBootstrap : UnityEngine.MonoBehaviour
    {
        private async void Start()
        {
            UIManager manager = UIManager.Instance;

            manager.Register<UIMainPanel>(
                "Main",
                "UI/MainPanel",
                UILayer.Normal);

            manager.Register<GameUI.Match3.Match3Panel>(
                "Match3",
                "UI/Match3Panel",
                UILayer.Normal,
                cache: true,
                layout: UIPanelLayout.FullScreen);

            manager.Register<GameUI.MergeTwo.MergeTwoPanel>(
                "MergeTwo",
                "UI/MergeTwoPanel",
                UILayer.Normal,
                cache: true,
                layout: UIPanelLayout.FullScreen);

            manager.Register<GameUI.Inventory.InventoryPanel>(
                "Inventory",
                "UI/InventoryPanel",
                UILayer.Normal,
                cache: true,
                layout: UIPanelLayout.FullScreen);

            manager.Register<UISettingsPanel>(
                "Settings",
                "UI/SettingsPanel",
                UILayer.Normal,
                cache: true);

            manager.Register<UIDialogPanel>(
                "Dialog",
                "UI/DialogPanel",
                UILayer.Popup,
                cache: false,
                modal: true,
                layout: UIPanelLayout.ContentSize);

            await manager.OpenAsync("Main");
        }
    }
}
