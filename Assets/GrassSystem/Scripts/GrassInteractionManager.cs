using UnityEngine;

/// <summary>
/// 草地交互管理器（单例）。
/// 负责管理所有草地交互器（GrassInteractor），每帧将交互器位置
/// 以及轨迹贴图（Trail RT）数据通过 sharedMaterial 传递给草地渲染器，
/// 供自定义草地 Shader 读取并实现压弯、变色等交互效果。
/// </summary>
public class GrassInteractionManager : MonoBehaviour
{
    /// <summary>单例实例，供 GrassInteractor 注册/注销时快速访问。</summary>
    public static GrassInteractionManager Instance { get; private set; }

    [Header("引用")]
    [Tooltip("接收交互器数据的草地渲染器数组（支持多块草地）。")]
    public Renderer[] grassRenderers;

    [Tooltip("轨迹贴图管理器——提供轨迹 RenderTexture。")]
    public TrailTextureManager trailTextureManager;

    [Header("设置")]
    [Range(1, 8)]
    [Tooltip("同时支持的最大交互器数量（对应 Shader 数组长度上限）。")]
    public int maxInteractors = 4;

    // ---- Shader 属性 ID（预缓存，避免每帧字符串查找开销）----
    static readonly int _InteractorPositions = Shader.PropertyToID("_InteractorPositions");
    static readonly int _InteractorCount     = Shader.PropertyToID("_InteractorCount");
    static readonly int _TrailTex            = Shader.PropertyToID("_TrailTex");
    static readonly int _TrailWorldOrigin    = Shader.PropertyToID("_TrailWorldOrigin");
    static readonly int _TrailWorldSize      = Shader.PropertyToID("_TrailWorldSize");

    // ---- 内部状态 ----
    private Vector4[]         _positions;  // 交互器位置数组（xyz = 世界坐标，w = 半径）
    private GrassInteractor[] _registered; // 已注册的交互器列表
    private int _count;                    // 当前有效交互器数量

    /// <summary>
    /// 初始化单例与内部数组（固定容量 8，与 Shader 端数组对齐）。
    /// </summary>
    void Awake()
    {
        Instance = this;
        _positions  = new Vector4[8];
        _registered = new GrassInteractor[8];
    }

    /// <summary>
    /// Start 阶段：若未在 Inspector 中指定 TrailTextureManager，
    /// 则尝试自动从场景中查找。
    /// </summary>
    void Start()
    {
        if (trailTextureManager == null)
            trailTextureManager = FindObjectOfType<TrailTextureManager>();
    }

    /// <summary>
    /// 注册一个交互器。如果已存在或已达上限则忽略。
    /// 由 GrassInteractor.OnEnable() 调用。
    /// </summary>
    /// <param name="g">要注册的 GrassInteractor 实例。</param>
    public void Register(GrassInteractor g)
    {
        // 防止重复注册
        for (int i = 0; i < _count; i++)
            if (_registered[i] == g) return;
        // 未超出最大数量则添加
        if (_count < maxInteractors)
        {
            _registered[_count] = g;
            _count++;
        }
    }

    /// <summary>
    /// 注销一个交互器（将其从列表中移除，用末尾元素填补空位）。
    /// 由 GrassInteractor.OnDisable() 调用。
    /// </summary>
    /// <param name="g">要注销的 GrassInteractor 实例。</param>
    public void Unregister(GrassInteractor g)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_registered[i] == g)
            {
                // 用最后一个元素覆盖当前位置，保持数组紧凑
                _registered[i] = _registered[_count - 1];
                _registered[_count - 1] = null;
                _count--;
                return;
            }
        }
    }

    /// <summary>
    /// LateUpdate：在所有 Update 之后执行，确保交互器位置已是本帧最终值。
    /// 1. 收集所有有效交互器的世界坐标和半径；
    /// 2. 将位置数组、轨迹 RT 及世界映射参数写入草地材质。
    /// </summary>
    void LateUpdate()
    {
        // ---- 收集交互器位置 ----
        int active = 0;
        for (int i = 0; i < _count; i++)
        {
            if (_registered[i] == null) continue;
            Vector3 p = _registered[i].transform.position;
            // Vector4：xyz = 世界坐标，w = 影响半径
            _positions[active] = new Vector4(p.x, p.y, p.z, _registered[i].radius);
            active++;
        }

        // 将超出有效数量的槽位清零，避免 Shader 读取到脏数据
        for (int i = active; i < 8; i++)
            _positions[i] = Vector4.zero;

        // ---- 判断轨迹 RT 是否可用 ----
        var ttm = trailTextureManager;
        var hasTrail = ttm != null && ttm.isActiveAndEnabled;

        // ---- 逐个草地渲染器更新材质属性 ----
        foreach (var r in grassRenderers)
        {
            if (r == null) continue;

            // 使用 sharedMaterial 直接设置属性（兼容 SRP Batcher CBUFFER，性能更优）
            var mat = r.sharedMaterial;
            if (mat != null)
            {
                // 写入交互器位置数组与数量
                mat.SetVectorArray(_InteractorPositions, _positions);
                mat.SetInt(_InteractorCount, active);

                // 若轨迹 RT 可用，同步传入贴图及其世界空间映射参数
                if (hasTrail)
                {
                    var rt = ttm.GetActiveTrailRT();
                    if (rt != null)
                    {
                        mat.SetTexture(_TrailTex, rt);
                        // 轨迹贴图对应的世界空间起点（xz 平面）
                        mat.SetVector(_TrailWorldOrigin, new Vector4(ttm.worldOrigin.x, 0, ttm.worldOrigin.y, 0));
                        // 轨迹贴图对应的世界空间尺寸（xz 平面）
                        mat.SetVector(_TrailWorldSize, new Vector4(ttm.worldSize.x, 0, ttm.worldSize.y, 0));
                    }
                }
            }
        }
    }
}