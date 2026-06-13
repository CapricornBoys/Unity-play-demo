using UnityEngine;

/// <summary>
/// 简单的第三人称角色控制器，用于演示草地踩踏交互效果。
/// 使用 WASD 键移动；需与 GrassInteractor 配合，
/// GrassInteractor 会自动将位置数据传递给草地 Shader。
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(GrassInteractor))]
public class GrassWalker : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("角色移动速度（单位/秒）。")]
    public float moveSpeed = 4f;

    [Tooltip("重力加速度（负值向下，单位：m/s²）。")]
    public float gravity   = -9.8f;

    [Tooltip("模型朝向插值速度：值越大，转身越快。")]
    public float rotateSpeed = 10f;

    [Header("视觉参数")]
    [Tooltip("（可选）角色模型的根节点 Transform，用于朝向移动方向的旋转。")]
    public Transform modelRoot;

    // CharacterController 组件引用
    private CharacterController _cc;
    // 竖直方向速度，用于模拟重力
    private float _vY;
    // 主摄像机引用，用于摄像机相对移动
    private Camera _cam;

    /// <summary>
    /// 初始化：缓存 CharacterController 与主摄像机引用。
    /// </summary>
    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _cam = Camera.main;
    }

    /// <summary>
    /// 每帧处理角色移动、重力模拟以及模型旋转。
    /// </summary>
    void Update()
    {
        // ---- 读取输入轴 ----
        float h = Input.GetAxis("Horizontal"); // 左右（A/D 或左右方向键）
        float v = Input.GetAxis("Vertical");   // 前后（W/S 或上下方向键）
        Vector3 moveDir = new Vector3(h, 0, v);

        // ---- 将移动方向转换为摄像机相对空间 ----
        // 确保移动方向始终以摄像机朝向为基准（上下视角不影响水平移动）
        if (_cam != null && moveDir.sqrMagnitude > 0.01f)
        {
            Vector3 camFwd   = _cam.transform.forward; camFwd.y = 0; camFwd.Normalize();
            Vector3 camRight = _cam.transform.right;   camRight.y = 0; camRight.Normalize();
            moveDir = camRight * h + camFwd * v;
        }

        // ---- 重力模拟 ----
        // 落地时重置竖直速度为微小负值（避免离地漂浮）
        if (_cc.isGrounded) _vY = -1f;
        else                _vY += gravity * Time.deltaTime; // 未落地则持续加速向下
        moveDir.y = _vY;

        // 应用移动
        _cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // ---- 旋转模型朝向移动方向 ----
        Vector3 flat = new Vector3(moveDir.x, 0, moveDir.z);
        if (flat.sqrMagnitude > 0.01f && modelRoot != null)
        {
            // 目标朝向：面向移动水平方向
            Quaternion target = Quaternion.LookRotation(flat);
            // 球面插值平滑旋转，避免瞬间转向
            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, target, rotateSpeed * Time.deltaTime);
        }
    }
}
