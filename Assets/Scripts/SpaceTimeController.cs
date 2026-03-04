using UnityEngine;

public class SpaceTimeController : MonoBehaviour
{
    public Transform[] gravitySources;
    public float[] masses;

    Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        int count = gravitySources.Length;
        mat.SetInt("_MassCount", count);

        Vector4[] positions = new Vector4[10];
        float[] values = new float[10];

        for (int i = 0; i < count; i++)
        {
            positions[i] = gravitySources[i].position;
            values[i] = masses[i];
        }

        mat.SetVectorArray("_MassPositions", positions);
        mat.SetFloatArray("_MassValues", values);
    }
}