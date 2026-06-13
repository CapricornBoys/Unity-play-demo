using UnityEngine.UI;

namespace GameUI.Demo
{
    /// <summary>
    /// 主界面控制器，由 UIManager 在 MainPanel 预制体加载后动态挂载。
    /// </summary>
    public sealed class UIMainPanel : UIPanel
    {
        protected override void OnInitialize()
        {
            GetUI<Button>("Match3Button").onClick.AddListener(OpenMatch3);
            GetUI<Button>("InventoryButton").onClick.AddListener(OpenInventory);
            GetUI<Button>("SettingsButton").onClick.AddListener(OpenSettings);
            GetUI<Button>("DialogButton").onClick.AddListener(OpenDialog);
        }

        private async void OpenMatch3()
        {
            await UIManager.Instance.OpenAsync("Match3");
        }

        private async void OpenInventory()
        {
            await UIManager.Instance.OpenAsync("Inventory");
        }

        private async void OpenSettings()
        {
            await UIManager.Instance.OpenAsync("Settings");
        }

        private async void OpenDialog()
        {
            await UIManager.Instance.OpenAsync(
                "Dialog",
                "This message is passed through Open args.");
        }
    }
}
