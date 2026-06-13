using UnityEngine.UI;

namespace GameUI.Demo
{
    /// <summary>
    /// 设置界面控制器，由框架动态挂载。
    /// </summary>
    public sealed class UISettingsPanel : UIPanel
    {
        protected override void OnInitialize()
        {
            GetUI<Button>("CloseButton").onClick.AddListener(Close);
        }
    }
}
