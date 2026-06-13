using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 基于 RenderTexture 的轨迹贴图管理器。
/// 核心功能：
///   1. 每帧在交互器位置"盖章"（Stamp），将踩踏印记写入 RenderTexture；
///   2. 每帧对 RT 进行淡出（Fade），模拟草地逐渐恢复弹起；
///   3. 将最终 RT 同步给地面 Shader，实现地面可见的踩踏痕迹。
///
/// 地面 Shader 与草地 Shader 均对该 RT 进行采样：
///   - 地面 Shader：在踩踏区域显示泥土/深色痕迹；
///   - 草地 Shader（由 GrassInteractionManager 转发）：在踩踏区域压弯并加深草叶颜色。
///
/// 技术细节：使用 Ping-Pong 双缓冲 RT（_rtA / _rtB）避免同帧读写冲突。
/// </summary>
public class TrailTextureManager : MonoBehaviour
{
    [Header("RenderTexture 设置")]
    [Range(256, 4096)]
    [Tooltip("轨迹 RT 的分辨率（正方形）。值越高细节越清晰，性能开销越大。")]
    public int resolution = 1024;

    [Range(0.001f, 0.5f)]
    [Tooltip("每帧淡出速率：值越大，踩踏痕迹消失越快（草地恢复越快）。")]
    public float fadeRate = 0.005f;

    [Tooltip("轨迹 RT 覆盖的世界空间尺寸（XZ 平面，单位：Unity 世界单位）。")]
    public Vector2 worldSize = new Vector2(20f, 20f);

    [Tooltip("轨迹 RT 覆盖区域的世界空间起点（左下角 XZ 坐标）。")]
    public Vector2 worldOrigin = new Vector2(-10f, -10f);

    [Header("盖章设置")]
    [Range(0.1f, 10f)]
    [Tooltip("盖章强度：值越大，踩踏印记越深（Shader 中的黑度越高）。")]
    public float stampStrength = 5.0f;

    [Header("引用")]
    [Tooltip("（可选）地面渲染器，绑定后自动为其创建运行时材质并注入轨迹贴图。")]
    public Renderer groundRenderer;

    // ---- Shader 属性 ID（预缓存）----
    static readonly int s_TrailTex      = Shader.PropertyToID("_TrailTex");
    static readonly int s_StampPos      = Shader.PropertyToID("_StampPos");
    static readonly int s_StampRadius   = Shader.PropertyToID("_StampRadius");
    static readonly int s_StampStrength = Shader.PropertyToID("_StampStrength");
    static readonly int s_WorldOrigin   = Shader.PropertyToID("_WorldOrigin");
    static readonly int s_WorldSize     = Shader.PropertyToID("_WorldSize");
    static readonly int s_TrailWorldOrigin = Shader.PropertyToID("_TrailWorldOrigin");
    static readonly int s_TrailWorldSize   = Shader.PropertyToID("_TrailWorldSize");
    static readonly int s_FadeRate      = Shader.PropertyToID("_FadeRate");

    // ---- Ping-Pong 双缓冲 RT ----
    RenderTexture _rtA, _rtB;
    RenderTexture _activeRT; // 当前帧的"读取"RT，始终指向最新结果

    // ---- 运行时材质（HideFlags.DontSave，不会写入资产）----
    Material _stampMat; // 盖章 Shader 材质（Custom/TrailStamp）
    Material _fadeMat;  // 淡出 Shader 材质（Custom/TrailFade）
    Material _groundMat; // 地面运行时材质实例

    /// <summary>
    /// 返回当前帧的活跃轨迹 RT，供 GrassInteractionManager 将其传入草地 Shader。
    /// </summary>
    public RenderTexture GetActiveTrailRT() => _activeRT;

    /// <summary>
    /// 初始化：依次创建 RenderTexture、Shader 材质、地面材质绑定。
    /// </summary>
    void Awake()
    {
        CreateRTs();
        CreateMaterials();
        SetupGroundMaterial();
    }

    /// <summary>
    /// 销毁时释放所有运行时创建的 RT 和材质，防止内存泄漏。
    /// </summary>
    void OnDestroy()
    {
        if (_rtA != null) { _rtA.Release(); Destroy(_rtA); }
        if (_rtB != null) { _rtB.Release(); Destroy(_rtB); }
        if (_stampMat != null) Destroy(_stampMat);
        if (_fadeMat != null) Destroy(_fadeMat);
        if (_groundMat != null) Destroy(_groundMat);
    }

    /// <summary>
    /// 创建 Ping-Pong 双缓冲 RenderTexture（ARGB32，无 Mipmap，线性色彩空间）。
    /// 双缓冲可避免 Graphics.Blit 在同一 RT 上同时读写导致的未定义行为。
    /// </summary>
    void CreateRTs()
    {
        var desc = new RenderTextureDescriptor(resolution, resolution,
            RenderTextureFormat.ARGB32, 0)
        {
            sRGB = false,             // 线性空间，避免 Gamma 矫正干扰数值
            autoGenerateMips = false,
            useMipMap = false
        };

        _rtA = new RenderTexture(desc) { name = "TrailRT_A", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        _rtA.Create();
        _rtB = new RenderTexture(desc) { name = "TrailRT_B", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        _rtB.Create();

        // 初始清空为全黑（无踩踏痕迹）
        ClearRT(_rtA);
        ClearRT(_rtB);
        _activeRT = _rtA;

        Debug.Log($"[TTM] RTs created: {resolution}x{resolution} ARGB32, stampStr={stampStrength}, fadeRate={fadeRate}");
    }

    /// <summary>
    /// 将指定 RenderTexture 清除为纯黑（代表无轨迹状态）。
    /// </summary>
    /// <param name="rt">要清除的 RenderTexture。</param>
    void ClearRT(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;
    }

    /// <summary>
    /// 创建盖章（TrailStamp）和淡出（TrailFade）材质，
    /// 并设置初始的世界空间映射参数与强度值。
    /// </summary>
    void CreateMaterials()
    {
        var shaderStamp = Shader.Find("Custom/TrailStamp");
        var shaderFade  = Shader.Find("Custom/TrailFade");

        _stampMat = new Material(shaderStamp) { hideFlags = HideFlags.DontSave };
        _fadeMat  = new Material(shaderFade)  { hideFlags = HideFlags.DontSave };

        // 初始化盖章参数
        _stampMat.SetFloat(s_StampStrength, stampStrength);
        _stampMat.SetVector(s_WorldOrigin, new Vector4(worldOrigin.x, 0, worldOrigin.y, 0));
        _stampMat.SetVector(s_WorldSize, new Vector4(worldSize.x, 0, worldSize.y, 0));
        // 初始化淡出速率
        _fadeMat.SetFloat(s_FadeRate, fadeRate);

        Debug.Log("[TTM] Stamp/Fade materials created");
    }

    /// <summary>
    /// 为地面渲染器创建运行时材质实例，并将轨迹 RT 及映射参数注入其中。
    /// 若未指定 groundRenderer 则跳过。
    /// </summary>
    void SetupGroundMaterial()
    {
        if (groundRenderer == null) return;

        // 优先使用 GroundShader，回退到 TrailGroundShader
        var shader = Shader.Find("Custom/GroundShader");
        if (shader == null)
            shader = Shader.Find("Custom/TrailGroundShader");

        // 创建运行时材质实例（不影响原始资产）
        _groundMat = new Material(shader) { name = "GroundMat_Instance" };
        _groundMat.SetColor("_GrassColor", new Color(0.15f, 0.4f, 0.12f, 1f));   // 草地底色
        _groundMat.SetColor("_TrailColor", new Color(0.35f, 0.2f, 0.08f, 1f));   // 轨迹颜色（泥土色）
        _groundMat.SetFloat("_TrailStrength", 2f);                                 // 轨迹混合强度
        groundRenderer.material = _groundMat;
        UpdateGroundTrailProperties();
    }

    /// <summary>
    /// 返回 Ping-Pong 对中"另一块" RT（即当前活跃 RT 之外的那块）。
    /// 用于确保每次 Blit 的源和目标不是同一块 RT。
    /// </summary>
    /// <param name="current">当前活跃的 RT。</param>
    RenderTexture OtherRT(RenderTexture current) => (current == _rtA) ? _rtB : _rtA;

    /// <summary>
    /// LateUpdate：在所有 Update 之后执行（确保交互器位置已更新）。
    /// 流程：
    ///   1. 同步 Inspector 可能调整的参数到材质；
    ///   2. 收集本帧所有有效交互器；
    ///   3. 对每个交互器执行一次 Blit 盖章（Ping-Pong 累积）；
    ///   4. 对累积结果执行淡出 Blit；
    ///   5. 将最终 RT 同步给地面材质。
    /// </summary>
    void LateUpdate()
    {
        // GrassInteractionManager 负责将轨迹 RT 转发给草地 Shader，此处只管理 RT 和地面
        if (_stampMat == null || _fadeMat == null) return;

        // 同步 Inspector 实时调整的参数（支持运行时调节）
        _fadeMat.SetFloat(s_FadeRate, fadeRate);
        _stampMat.SetFloat(s_StampStrength, stampStrength);

        var interactors = GatherInteractors();

        if (interactors.Count > 0)
        {
            // ---- 逐交互器 Ping-Pong 盖章 ----
            RenderTexture src = _activeRT;
            RenderTexture dst = OtherRT(_activeRT);

            for (int i = 0; i < interactors.Count; i++)
            {
                var (pos, radius) = interactors[i];
                // 设置本次盖章的世界坐标、半径、强度
                _stampMat.SetVector(s_StampPos, new Vector4(pos.x, pos.y, pos.z, 0));
                _stampMat.SetFloat(s_StampRadius, radius);
                _stampMat.SetFloat(s_StampStrength, stampStrength);
                // 将 src 盖章到 dst
                Graphics.Blit(src, dst, _stampMat);
                // 交换源与目标，为下一次盖章准备
                var tmp = src; src = dst; dst = tmp;
            }

            // ---- 对盖章结果执行淡出 ----
            RenderTexture fadedRT = OtherRT(src);
            Graphics.Blit(src, fadedRT, _fadeMat);
            _activeRT = fadedRT; // 更新活跃 RT 指针
        }
        else
        {
            // 无交互器时，只做淡出（草地缓慢恢复）
            RenderTexture fadedRT = OtherRT(_activeRT);
            Graphics.Blit(_activeRT, fadedRT, _fadeMat);
            _activeRT = fadedRT;
        }

        // 将最新 RT 同步给地面材质
        UpdateGroundTrailProperties();
    }

    /// <summary>
    /// 收集场景中所有当前活跃的 GrassInteractor，
    /// 返回其世界坐标和影响半径的列表，供本帧盖章使用。
    /// </summary>
    List<(Vector3 pos, float radius)> GatherInteractors()
    {
        var list = new List<(Vector3 pos, float radius)>();
        var all = FindObjectsByType<GrassInteractor>(FindObjectsSortMode.None);
        foreach (var g in all)
        {
            if (g != null && g.isActiveAndEnabled)
                list.Add((g.transform.position, g.radius));
        }
        return list;
    }

    /// <summary>
    /// 将当前活跃的轨迹 RT 及其世界空间映射参数同步给地面材质，
    /// 保证地面 Shader 始终采样最新的踩踏痕迹数据。
    /// </summary>
    void UpdateGroundTrailProperties()
    {
        if (_groundMat == null) return;

        _groundMat.SetTexture(s_TrailTex, _activeRT);
        // 世界空间起点与尺寸，地面 Shader 用其将 UV 映射回世界坐标
        _groundMat.SetVector(s_TrailWorldOrigin, new Vector4(worldOrigin.x, 0, worldOrigin.y, 0));
        _groundMat.SetVector(s_TrailWorldSize, new Vector4(worldSize.x, 0, worldSize.y, 0));
    }
}
