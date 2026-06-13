using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 草地网格生成器。
/// 在运行时（或通过右键菜单）生成一个点云 Mesh，
/// 每个顶点对应草地几何 Shader 中的一根草叶。
/// 生成的 Mesh 拓扑类型为 Points，专供 Geometry Shader 扩展成草叶几何体。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassMeshGenerator : MonoBehaviour
{
    [Header("草地密度")]
    [Tooltip("X 方向（行）的草叶数量。")]
    public int   densityX   = 50;

    [Tooltip("Z 方向（列）的草叶数量。")]
    public int   densityZ   = 50;

    [Tooltip("草地区域的总宽度（X 轴，Unity 世界单位）。")]
    public float width      = 10f;

    [Tooltip("草地区域的总深度（Z 轴，Unity 世界单位）。")]
    public float depth      = 10f;

    [Tooltip("草叶位置的随机抖动幅度（相对于格子步长的比例），避免规则网格感。")]
    public float randomOffset = 0.15f;

    [Header("地形贴合")]
    [Tooltip("是否启用地形贴合：通过射线检测将草叶落在地形表面。")]
    public bool  conformToTerrain = false;

    [Tooltip("向下射线的起始高度偏移（从草叶位置向上偏移后向下投射）。")]
    public float raycastHeight    = 10f;

    [Tooltip("地形贴合所使用的层级遮罩，只检测指定层的碰撞体。")]
    public LayerMask terrainLayer = -1;

    /// <summary>
    /// 在 Awake 时自动生成草地点云 Mesh。
    /// </summary>
    void Awake()
    {
        GenerateMesh();
    }

    /// <summary>
    /// 生成草地点云 Mesh 并赋给 MeshFilter。
    /// 可通过右键菜单 "Regenerate Mesh" 在编辑器中手动触发重新生成。
    /// </summary>
    [ContextMenu("Regenerate Mesh")]
    public void GenerateMesh()
    {
        var mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "GrassPoints";

        int total = densityX * densityZ;
        var verts   = new List<Vector3>(total); // 顶点列表（草叶根部世界/局部坐标）
        var normals = new List<Vector3>(total); // 法线列表（统一朝上，供 Shader 使用）
        var uvs     = new List<Vector2>(total); // UV 列表（存储归一化行列坐标）
        var indices = new List<int>(total);     // 索引列表（Points 拓扑，一一对应）

        // 计算每格步长与起始偏移（使草地区域以 Transform 原点为中心）
        float stepX = width  / (densityX - 1);
        float stepZ = depth  / (densityZ - 1);
        float ox    = -width  * 0.5f;
        float oz    = -depth  * 0.5f;

        for (int z = 0; z < densityZ; z++)
        for (int x = 0; x < densityX; x++)
        {
            // 在格子步长内随机抖动，减少均匀网格感
            float px = ox + x * stepX + Random.Range(-randomOffset, randomOffset) * stepX;
            float pz = oz + z * stepZ + Random.Range(-randomOffset, randomOffset) * stepZ;
            float py = 0f;

            Vector3 worldPos = transform.TransformPoint(new Vector3(px, py, pz));

            // 若启用地形贴合，从上方向下射线检测地形高度
            if (conformToTerrain)
            {
                Ray ray = new Ray(worldPos + Vector3.up * raycastHeight, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, raycastHeight * 2, terrainLayer))
                {
                    // 将命中点转回局部空间的 Y 坐标
                    py = transform.InverseTransformPoint(hit.point).y;
                }
            }

            int idx = verts.Count;
            verts.Add(new Vector3(px, py, pz));
            normals.Add(Vector3.up);                                              // 法线朝上
            uvs.Add(new Vector2((float)x / (densityX - 1), (float)z / (densityZ - 1))); // 归一化 UV
            indices.Add(idx);
        }

        // 将数据写入 Mesh
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices, MeshTopology.Points, 0); // Points 拓扑：每个顶点 = 一根草叶
        // 设置包围盒（额外外扩，容纳草叶几何高度）
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(width + 2, 5, depth + 2));

        mf.sharedMesh = mesh;
    }
}
