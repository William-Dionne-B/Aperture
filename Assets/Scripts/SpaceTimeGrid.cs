// SpaceTimeGrid.cs
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
 [RequireComponent(typeof(MeshRenderer))]
public class SpaceTimeGrid : MonoBehaviour
{
    public int resolution = 200;
    public float size = 50f;
    public float maxWarpDepth = 2000f;
    public float gridCellWorldSize = 500f;

    static readonly int GridScaleId = Shader.PropertyToID("_GridScale");

    void Start()
    {
        resolution = Mathf.Max(1, resolution);
        size = Mathf.Max(0.01f, size);
        gridCellWorldSize = Mathf.Max(0.01f, gridCellWorldSize);

        Mesh mesh = new Mesh();
        mesh.name = "SpaceTimeGridMesh";
        GetComponent<MeshFilter>().mesh = mesh;

        int vertCount = (resolution + 1) * (resolution + 1);
        if (vertCount > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] triangles = new int[resolution * resolution * 6];

        float step = size / resolution;
        int v = 0;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                vertices[v] = new Vector3(x * step - size/2, 0, z * step - size/2);
                uv[v] = new Vector2((float)x / resolution, (float)z / resolution);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * (resolution + 1) + x;

                triangles[t++] = i;
                triangles[t++] = i + resolution + 1;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + resolution + 1;
                triangles[t++] = i + resolution + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        float extent = size * 0.5f;
        float depth = Mathf.Max(1f, maxWarpDepth);
        mesh.bounds = new Bounds(
            Vector3.zero,
            new Vector3(size, depth * 2f, size)
        );

        ApplyGridMaterialSettings();
    }

    void OnValidate()
    {
        gridCellWorldSize = Mathf.Max(0.01f, gridCellWorldSize);
        ApplyGridMaterialSettings();
    }

    void ApplyGridMaterialSettings()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            return;
        }

        Material material = meshRenderer.sharedMaterial;
        if (material == null)
        {
            return;
        }

        float gridScale = 1f / gridCellWorldSize;
        material.SetFloat(GridScaleId, gridScale);
    }
}