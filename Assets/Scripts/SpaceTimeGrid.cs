// SpaceTimeGrid.cs
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
 [RequireComponent(typeof(MeshRenderer))]
public class SpaceTimeGrid : MonoBehaviour
{
    public int resolution = 200;
    public float size = 500f;
    public float maxWarpDepth = 2000f;
    public float gridCellWorldSize = 500f;
    public float gridVerticalOffset = -0.5f;
    public bool followCenterOfMassY = true;
    public bool followCamera = true;
    public Transform cameraTarget;
    public Color lineColor = new Color(0.45f, 0.45f, 0.45f, 0.28f);
    [Range(0.0001f, 0.01f)] public float lineWidth = 0.0006f;
    public float warpStrength = 0.01f;
    public float warpMultiplier = 4f;
    public float fadeStartDistance = 350f;
    public float fadeEndDistance = 1600f;

    static readonly int GridScaleId = Shader.PropertyToID("_GridScale");
    static readonly int LineColorId = Shader.PropertyToID("_LineColor");
    static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int WarpMultiplierId = Shader.PropertyToID("_WarpMultiplier");
    static readonly int FadeStartDistanceId = Shader.PropertyToID("_FadeStartDistance");
    static readonly int FadeEndDistanceId = Shader.PropertyToID("_FadeEndDistance");

    void Start()
    {
        resolution = Mathf.Max(1, resolution);
        size = Mathf.Max(0.01f, size);
        gridCellWorldSize = Mathf.Max(0.01f, gridCellWorldSize);
        fadeEndDistance = Mathf.Max(fadeStartDistance + 0.01f, fadeEndDistance);
        ApplyVerticalOffset();
        ApplyCameraFollow();

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
        resolution = Mathf.Max(1, resolution);
        size = Mathf.Max(0.01f, size);
        gridCellWorldSize = Mathf.Max(0.01f, gridCellWorldSize);
        fadeEndDistance = Mathf.Max(fadeStartDistance + 0.01f, fadeEndDistance);
        ApplyVerticalOffset();
        ApplyGridMaterialSettings();
    }

    void LateUpdate()
    {
        ApplyCameraFollow();
    }

    void ApplyVerticalOffset()
    {
        Vector3 position = transform.position;
        position.y = ResolveGridY();
        transform.position = position;
    }

    void ApplyCameraFollow()
    {
        Vector3 position = transform.position;

        if (followCamera)
        {
            Transform target = cameraTarget;
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (target != null)
            {
                position.x = target.position.x;
                position.z = target.position.z;
            }
        }

        position.y = ResolveGridY();
        transform.position = position;
    }

    float ResolveGridY()
    {
        if (!followCenterOfMassY)
        {
            return gridVerticalOffset;
        }

        return GravityManager.GetCenterOfMass().y + gridVerticalOffset;
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
        material.SetColor(LineColorId, lineColor);
        material.SetFloat(LineWidthId, lineWidth);
        material.SetFloat(StrengthId, warpStrength);
        material.SetFloat(WarpMultiplierId, warpMultiplier);
        material.SetFloat(FadeStartDistanceId, fadeStartDistance);
        material.SetFloat(FadeEndDistanceId, fadeEndDistance);
        material.renderQueue = (int)RenderQueue.Transparent - 100;
    }
}