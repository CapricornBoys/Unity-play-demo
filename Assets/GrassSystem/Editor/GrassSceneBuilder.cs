using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 仅在编辑器中运行的场景构建工具。
/// 通过顶部菜单 "GrassSystem > Build Grass Scene" 一键自动生成
/// 完整的交互式草地演示场景（包含地面、草地、玩家、摄像机等）。
/// </summary>
public static class GrassSceneBuilder
{
    [MenuItem("GrassSystem/Build Grass Scene")]
    public static void BuildScene()
    {
        // ---- 确保材质目录存在 ----
        if (!AssetDatabase.IsValidFolder("Assets/GrassSystem"))
            AssetDatabase.CreateFolder("Assets", "GrassSystem");
        if (!AssetDatabase.IsValidFolder("Assets/GrassSystem/Materials"))
            AssetDatabase.CreateFolder("Assets/GrassSystem", "Materials");

        // ---- 创建或加载草地/地面材质 ----
        Material grassMat  = CreateOrLoadMat("Assets/GrassSystem/Materials/GrassMat.mat",  "Custom/GrassShader");
        Material groundMat = CreateOrLoadMat("Assets/GrassSystem/Materials/GroundMat.mat", "Custom/GroundShader");

        // ---- 配置草地材质默认参数 ----
        grassMat.SetColor("_BaseColor",   new Color(0.08f, 0.42f, 0.08f)); // 草叶根部颜色（深绿）
        grassMat.SetColor("_TipColor",    new Color(0.55f, 0.85f, 0.2f));  // 草叶尖部颜色（亮黄绿）
        grassMat.SetFloat("_WindStrength",     0.35f);  // 风力强度
        grassMat.SetFloat("_WindSpeed",        1.2f);   // 风速
        grassMat.SetFloat("_WindFrequency",    1.5f);   // 风频（波动频率）
        grassMat.SetVector("_WindDirection",   new Vector4(1f, 0, 0.3f, 0)); // 风向（xz 平面）
        grassMat.SetFloat("_BladeWidth",       0.05f);  // 草叶宽度
        grassMat.SetFloat("_BladeHeight",      0.55f);  // 草叶高度
        grassMat.SetFloat("_InteractionStrength", 1.2f); // 交互压弯强度
        EditorUtility.SetDirty(grassMat); // 标记为已修改，确保保存

        // ---- 配置地面材质默认参数 ----
        groundMat.SetColor("_MainColor",   new Color(0.35f, 0.25f, 0.15f)); // 地面主色（泥土色）
        groundMat.SetColor("_GrassColor",  new Color(0.15f, 0.40f, 0.12f)); // 地面草色（杂草底色）
        groundMat.SetColor("_TrailColor",  new Color(0.28f, 0.18f, 0.08f)); // 踩踏轨迹颜色（深泥土）
        EditorUtility.SetDirty(groundMat);

        AssetDatabase.SaveAssets();

        // ---- 创建地面平面（20m × 20m）----
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2, 1, 2); // Plane 默认 10m，Scale×2 = 20m
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // ---- 创建草地 GameObject ----
        GameObject grassGO = new GameObject("GrassField");
        grassGO.transform.position = Vector3.zero;

        MeshFilter   mf = grassGO.AddComponent<MeshFilter>();
        MeshRenderer mr = grassGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = grassMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 草地点云不参与阴影投射

        // 配置草地网格生成器参数
        GrassMeshGenerator gen = grassGO.AddComponent<GrassMeshGenerator>();
        gen.densityX     = 80;   // X 方向 80 根/行
        gen.densityZ     = 80;   // Z 方向 80 根/列，共 6400 根草叶
        gen.width        = 18f;  // 草地宽度（略小于地面，留出边缘）
        gen.depth        = 18f;  // 草地深度
        gen.randomOffset = 0.2f; // 位置随机抖动

        // ---- 创建草地交互管理器 ----
        GameObject mgr = new GameObject("GrassInteractionManager");
        GrassInteractionManager gim = mgr.AddComponent<GrassInteractionManager>();
        gim.grassRenderers = new Renderer[] { mr }; // 关联草地渲染器
        gim.maxInteractors = 4;                     // 最多支持 4 个同时交互

        // ---- 创建玩家胶囊体 ----
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1.0f, 0);

        // 给玩家设置蓝色材质
        var pRend = player.GetComponent<Renderer>();
        var pMat  = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        pMat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.8f)); // 蓝色玩家
        pRend.sharedMaterial = pMat;

        // 添加 CharacterController（胶囊碰撞体尺寸匹配角色）
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0, 0);

        // 添加草地交互器（玩家走路时压弯草叶）
        GrassInteractor interactor = player.AddComponent<GrassInteractor>();
        interactor.radius = 0.8f; // 玩家脚下影响半径

        // 添加移动控制器
        GrassWalker walker = player.AddComponent<GrassWalker>();

        // ---- 创建静态障碍物（岩石，演示多交互器支持）----
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock";
        rock.transform.position = new Vector3(3, 0.5f, 2);
        rock.transform.localScale = Vector3.one * 1.0f;
        var rMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rMat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f)); // 灰色石头
        rock.GetComponent<Renderer>().sharedMaterial = rMat;
        rock.AddComponent<GrassInteractor>().radius = 1.1f; // 石头压弯半径略大

        // ---- 配置摄像机（俯视角）----
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 8, -10);
        cam.transform.rotation = Quaternion.Euler(35, 0, 0); // 俯视 35°

        // ---- 调整方向光（确保草叶有良好光照）----
        Light dLight = Object.FindObjectOfType<Light>();
        if (dLight != null)
        {
            dLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            dLight.intensity = 1.2f;
        }

        Debug.Log("[GrassSystem] Scene built! Press Play and use WASD to walk on the grass.");
        // 构建完成后自动选中玩家对象，便于查看
        Selection.activeGameObject = player;
    }

    /// <summary>
    /// 创建或加载指定路径的材质。
    /// 若材质已存在则直接加载复用；若不存在则用指定 Shader 创建并保存。
    /// 当 Shader 不存在时回退到 URP/Lit。
    /// </summary>
    /// <param name="path">材质在 Assets 中的路径（含文件名）。</param>
    /// <param name="shaderName">目标 Shader 的名称。</param>
    /// <returns>加载或新建的 Material 实例。</returns>
    static Material CreateOrLoadMat(string path, string shaderName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader sh = Shader.Find(shaderName);
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit"); // 回退 Shader
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
}
