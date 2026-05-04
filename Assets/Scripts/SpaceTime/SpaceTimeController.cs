using UnityEngine;
using System.Collections.Generic;

public class SpaceTimeController : MonoBehaviour
{
    const int MaxMasses = 64;

    [Header("Configuration")]
    [Tooltip("Si coché, utilise automatiquement tous les astres du GravityManager")]
    public bool useAllGravityBodies = true;
    
    [Tooltip("Utilisé uniquement si Use All Gravity Bodies est décoché")]
    public Transform[] gravitySources;
    public float[] masses;
    public float defaultMass = 1f;

    Material mat;
    readonly Vector4[] cachedPositions = new Vector4[MaxMasses];
    readonly float[] cachedValues = new float[MaxMasses];

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        if (mat == null)
        {
            return;
        }

        Vector4[] positions = new Vector4[MaxMasses];
        float[] values = new float[MaxMasses];
        
        int count = useAllGravityBodies
            ? FillFromGravityBodies(cachedPositions, cachedValues)
            : FillFromManualSources(cachedPositions, cachedValues);

        mat.SetInt("_MassCount", count);
        mat.SetVectorArray("_MassPositions", cachedPositions);
        mat.SetFloatArray("_MassValues", cachedValues);
    }

    int FillFromGravityBodies(Vector4[] positions, float[] values)
    {
        int count = 0;
        HashSet<int> seenTransforms = new HashSet<int>();
        var bodies = GravityManager.Bodies;

        for (int i = 0; i < bodies.Count && count < MaxMasses; i++)
        {
            GravityBody body = bodies[i];
            if (body == null)
            {
                continue;
            }

            AddMassSource(body.transform, ResolveMassFromBody(body), positions, values, seenTransforms, ref count);
        }

        if (count < MaxMasses)
        {
            ObjectProperties[] objectsWithMass = FindObjectsOfType<ObjectProperties>();
            for (int i = 0; i < objectsWithMass.Length && count < MaxMasses; i++)
            {
                ObjectProperties source = objectsWithMass[i];
                if (source == null)
                {
                    continue;
                }

                float mass = source.Mass;
                if (mass <= 0f)
                {
                    continue;
                }

                AddMassSource(source.transform, mass, positions, values, seenTransforms, ref count);
            }
        }

        if (count < MaxMasses)
        {
            Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
            for (int i = 0; i < rigidbodies.Length && count < MaxMasses; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null || rb.mass <= 0f)
                {
                    continue;
                }

                AddMassSource(rb.transform, rb.mass, positions, values, seenTransforms, ref count);
            }
        }

        return count;
    }

    void AddMassSource(Transform source, float mass, Vector4[] positions, float[] values, HashSet<int> seenTransforms, ref int count)
    {
        if (source == null || mass <= 0f || count >= MaxMasses)
        {
            return;
        }

        int instanceId = source.GetInstanceID();
        if (!seenTransforms.Add(instanceId))
        {
            return;
        }

        positions[count] = source.position;
        values[count] = mass;
        count++;
    }

    int FillFromManualSources(Vector4[] positions, float[] values)
    {
        int sourceCount = gravitySources != null ? gravitySources.Length : 0;
        int maxSources = Mathf.Min(sourceCount, MaxMasses);
        int count = 0;

        for (int i = 0; i < maxSources; i++)
        {
            Transform source = gravitySources[i];
            if (source == null)
            {
                continue;
            }

            positions[count] = source.position;
            values[count] = ResolveMass(i, source);
            count++;
        }

        return count;
    }

    float ResolveMassFromBody(GravityBody body)
    {
        if (body.rb != null && body.rb.mass > 0f)
        {
            return body.rb.mass;
        }

        if (body.Mass > 0f)
        {
            return body.Mass;
        }

        return Mathf.Max(defaultMass, 0.0001f);
    }

    float ResolveMass(int index, Transform source)
    {
        if (masses != null && index < masses.Length && masses[index] > 0f)
        {
            return masses[index];
        }

        if (source.TryGetComponent<GravityBody>(out GravityBody gravityBody))
        {
            if (gravityBody.rb != null && gravityBody.rb.mass > 0f)
            {
                return gravityBody.rb.mass;
            }

            if (gravityBody.Mass > 0f)
            {
                return gravityBody.Mass;
            }
        }

        if (source.TryGetComponent<Rigidbody>(out Rigidbody rb) && rb.mass > 0f)
        {
            return rb.mass;
        }
        
        if (source.TryGetComponent<ObjectProperties>(out ObjectProperties objectProperties) && objectProperties.Mass > 0f)
        {
            return objectProperties.Mass;
        }

        return Mathf.Max(defaultMass, 0.0001f);
    }
}