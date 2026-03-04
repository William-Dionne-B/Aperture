// SpaceTimeGrid.cs
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SpaceTimeGrid : MonoBehaviour
{
    public int resolution = 200;
    public float size = 50f;

    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int vertCount = (resolution + 1) * (resolution + 1);
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
    }
}