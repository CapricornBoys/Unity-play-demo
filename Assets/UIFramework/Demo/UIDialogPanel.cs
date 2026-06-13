using UnityEngine.UI;

namespace GameUI.Demo
{
    /// <summary>
    /// 模态弹窗控制器，由框架动态挂载。
    /// </summary>
    public sealed class UIDialogPanel : UIPanel
    {
        private Text messageText;

        protected override void OnInitialize()
        {
            messageText = GetUI<Text>("Message");
            GetUI<Button>("ConfirmButton").onClick.AddListener(Close);
        }

        protected override void OnOpen(object args)
        {
            messageText.text = args as string ?? "No message.";
        }
    }
}
