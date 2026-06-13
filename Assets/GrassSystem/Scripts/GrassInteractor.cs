using UnityEngine;

/// <summary>
/// 草地交互器组件。
/// 挂载到任何需要与草地发生交互（踩踏/压弯）的对象上。
/// 启用时自动向 GrassInteractionManager 注册，禁用时自动注销。
/// </summary>
public class GrassInteractor : MonoBehaviour
{
    [Tooltip("该交互器对周围草叶的影响半径（单位：Unity 世界单位）。")]
    public float radius = 1.2f;

    [Tooltip("若为 true，在 Scene 视图中以半透明球体可视化影响范围。")]
    public bool  showGizmo = true;

    /// <summary>
    /// 对象启用时，向 GrassInteractionManager 注册自身，
    /// 使 Manager 能够在每帧将位置传递给草地 Shader。
    /// </summary>
    void OnEnable()  => GrassInteractionManager.Instance?.Register(this);

    /// <summary>
    /// 对象禁用时，从 GrassInteractionManager 注销自身，
    /// 避免 Manager 继续引用已失效的交互器。
    /// </summary>
    void OnDisable() => GrassInteractionManager.Instance?.Unregister(this);

    /// <summary>
    /// 在 Scene 视图中绘制调试用 Gizmo，
    /// 用实心半透明球（内部）+ 线框球（外轮廓）展示影响半径。
    /// 仅在选中该对象时显示。
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        // 半透明实心球：直观表示影响体积范围
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        // 不透明线框球：清晰标出半径边界
        Gizmos.color = new Color(0, 1, 0, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
