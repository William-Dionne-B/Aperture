// SpaceTimeGrid.cs
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpaceTimeGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int resolution = 200;
    public float size = 1500f;
    public float maxWarpDepth = 2000f;
    public float gridCellWorldSize = 500f;
    public float gridVerticalOffset = -0.5f;
    public bool followCenterOfMassY = true;
    public bool followCamera = true;
    public Transform cameraTarget;

    [Header("Line Appearance")]
    public Color lineColor = new Color(0.45f, 0.45f, 0.45f, 0.28f);
    [Range(0.0001f, 0.01f)] public float lineWidth = 0.0006f;

    [Header("Warp Settings")]
    public float warpStrength = 0.01f;
    public float warpMultiplier = 4f;

    [Header("Distance Fade")]
    public float fadeStartDistance = 150f;
    public float fadeEndDistance = 750f;

    [Header("Camera Distance Transparency")]
    public float alphaMin = 0.0f;   // Alpha quand très proche
    public float alphaMax = 1.0f;   // Alpha quand très loin

    // Shader property IDs
    static readonly int GridScaleId = Shader.PropertyToID("_GridScale");
    static readonly int LineColorId = Shader.PropertyToID("_LineColor");
    static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int WarpMultiplierId = Shader.PropertyToID("_WarpMultiplier");
    static readonly int FadeStartDistanceId = Shader.PropertyToID("_FadeStartDistance");
    static readonly int FadeEndDistanceId = Shader.PropertyToID("_FadeEndDistance");
    static readonly int CameraPosId = Shader.PropertyToID("_CameraWorldPos");
    static readonly int AlphaMinId = Shader.PropertyToID("_AlphaMin");
    static readonly int AlphaMaxId = Shader.PropertyToID("_AlphaMax");

    private Material materialInstance;

    void Start()
    {
        resolution = Mathf.Max(1, resolution);
        size = Mathf.Max(0.01f, size);
        gridCellWorldSize = Mathf.Max(0.01f, gridCellWorldSize);
        fadeEndDistance = Mathf.Max(fadeStartDistance + 0.01f, fadeEndDistance);

        // Crée une instance du material propre à cet objet
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer.sharedMaterial != null)
            materialInstance = meshRenderer.material; // crée l'instance

        ApplyVerticalOffset();
        ApplyCameraFollow();

        Mesh mesh = new Mesh();
        mesh.name = "SpaceTimeGridMesh";
        GetComponent<MeshFilter>().mesh = mesh;

        int vertCount = (resolution + 1) * (resolution + 1);
        if (vertCount > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] triangles = new int[resolution * resolution * 6];

        float step = size / resolution;
        int v = 0;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                vertices[v] = new Vector3(x * step - size / 2, 0, z * step - size / 2);
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
        mesh.bounds = new Bounds(
            Vector3.zero,
            new Vector3(size, Mathf.Max(1f, maxWarpDepth) * 2f, size)
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
        UpdateCameraDistanceFade();
    }

    // ==========================================
    // POSITION DE LA GRILLE
    // ==========================================

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
                target = Camera.main.transform;

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
            return gridVerticalOffset;

        return GravityManager.GetCenterOfMass().y + gridVerticalOffset;
    }

    // ==========================================
    // MATERIAL
    // ==========================================

    void ApplyGridMaterialSettings()
    {
        Material mat = GetActiveMaterial();
        if (mat == null) return;

        float gridScale = 1f / gridCellWorldSize;
        mat.SetFloat(GridScaleId, gridScale);
        mat.SetColor(LineColorId, lineColor);
        mat.SetFloat(LineWidthId, lineWidth);
        mat.SetFloat(StrengthId, warpStrength);
        mat.SetFloat(WarpMultiplierId, warpMultiplier);
        mat.SetFloat(FadeStartDistanceId, fadeStartDistance);
        mat.SetFloat(FadeEndDistanceId, fadeEndDistance);
        mat.SetFloat(AlphaMinId, alphaMin);
        mat.SetFloat(AlphaMaxId, alphaMax);
        mat.renderQueue = (int)RenderQueue.Transparent - 100;
    }

    void UpdateCameraDistanceFade()
    {
        Material mat = GetActiveMaterial();
        if (mat == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        mat.SetVector(CameraPosId, cam.transform.position);
        mat.SetFloat(AlphaMinId, alphaMin);
        mat.SetFloat(AlphaMaxId, alphaMax);
    }

    // Retourne l'instance du material (en jeu) ou le sharedMaterial (en éditeur)
    Material GetActiveMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return null;

        if (Application.isPlaying)
        {
            if (materialInstance == null)
                materialInstance = meshRenderer.material;
            return materialInstance;
        }

        return meshRenderer.sharedMaterial;
    }

    void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }
}