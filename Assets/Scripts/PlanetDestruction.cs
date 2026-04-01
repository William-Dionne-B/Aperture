using System;
using UnityEngine;

public class PlanetDestruction : MonoBehaviour
{
    public static event Action<PlanetDestruction, Collision> CollisionDetected;

    [Header("Merge Rules")]
    public float minImpactSpeed = 0.1f;
    public bool requireGravityBodyOnOther = false;
    public bool logCollisionDetection = true;

    private bool isMerging;
    private Rigidbody sourceRigidbody;
    private ObjectProperties sourceProperties;
    private Renderer[] sourceRenderers;
    private Collider[] sourceColliders;

    void Awake()
    {
        sourceRigidbody = GetComponent<Rigidbody>();
        sourceProperties = GetComponent<ObjectProperties>();
        sourceRenderers = GetComponentsInChildren<Renderer>(true);
        sourceColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        CollisionDetected?.Invoke(this, collision);

        if (isMerging || collision == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < Mathf.Max(0f, minImpactSpeed))
        {
            return;
        }

        GameObject otherObject = GetCollisionObject(collision);
        if (otherObject == null || otherObject == gameObject)
        {
            return;
        }

        PlanetDestruction otherDestruction = otherObject.GetComponent<PlanetDestruction>();
        if (otherDestruction != null && otherDestruction.isMerging)
        {
            return;
        }

        if (requireGravityBodyOnOther && otherObject.GetComponent<GravityBody>() == null)
        {
            return;
        }

        if (!ShouldHandleMerge(otherObject))
        {
            return;
        }

        isMerging = true;
        if (otherDestruction != null)
        {
            otherDestruction.isMerging = true;
        }

        MergeWith(otherObject);

        isMerging = false;
        if (otherDestruction != null)
        {
            otherDestruction.isMerging = false;
        }
    }

    GameObject GetCollisionObject(Collision collision)
    {
        if (collision.collider == null)
        {
            return null;
        }

        Rigidbody otherRigidbody = collision.collider.attachedRigidbody;
        if (otherRigidbody != null)
        {
            return otherRigidbody.gameObject;
        }

        return collision.collider.transform.root.gameObject;
    }

    bool ShouldHandleMerge(GameObject otherObject)
    {
        float thisRadius = GetRadius(gameObject);
        float otherRadius = GetRadius(otherObject);

        if (thisRadius > otherRadius)
        {
            return true;
        }

        if (thisRadius < otherRadius)
        {
            return false;
        }

        return GetInstanceID() < otherObject.GetInstanceID();
    }

    void MergeWith(GameObject otherObject)
    {
        ObjectProperties otherProperties = otherObject.GetComponent<ObjectProperties>();
        Rigidbody otherRigidbody = otherObject.GetComponent<Rigidbody>();

        float thisRadius = GetRadius(gameObject);
        float otherRadius = GetRadius(otherObject);
        float thisMass = GetMass(gameObject);
        float otherMass = GetMass(otherObject);

        float combinedMass = Mathf.Max(0f, thisMass + otherMass);
        float combinedRadius = Mathf.Pow(Mathf.Pow(Mathf.Max(0f, thisRadius), 3f) + Mathf.Pow(Mathf.Max(0f, otherRadius), 3f), 1f / 3f);

        Vector3 thisPosition = sourceRigidbody != null ? sourceRigidbody.position : transform.position;
        Vector3 otherPosition = otherRigidbody != null ? otherRigidbody.position : otherObject.transform.position;
        Vector3 mergedPosition = combinedMass > 0f
            ? ((thisPosition * thisMass) + (otherPosition * otherMass)) / combinedMass
            : transform.position;

        Vector3 thisVelocity = sourceRigidbody != null ? sourceRigidbody.linearVelocity : Vector3.zero;
        Vector3 otherVelocity = otherRigidbody != null ? otherRigidbody.linearVelocity : Vector3.zero;
        Vector3 mergedVelocity = combinedMass > 0f
            ? ((thisVelocity * thisMass) + (otherVelocity * otherMass)) / combinedMass
            : Vector3.zero;

        Vector3 mergedAngularVelocity = sourceRigidbody != null ? sourceRigidbody.angularVelocity : Vector3.zero;
        bool thisWins = ShouldHandleMerge(otherObject);

        if (thisWins)
        {
            ApplyMergedState(gameObject, combinedMass, combinedRadius, mergedPosition, mergedVelocity, mergedAngularVelocity);
            Destroy(otherObject);
        }
        else
        {
            ApplyMergedState(otherObject, combinedMass, combinedRadius, mergedPosition, mergedVelocity, mergedAngularVelocity);
            Destroy(gameObject);
        }

        if (otherProperties != null && otherRigidbody != null)
        {
            otherProperties.mass = combinedMass;
            otherProperties.radius = combinedRadius;
        }

        if (logCollisionDetection)
        {
            string winnerName = thisWins ? name : otherObject.name;
            string loserName = thisWins ? otherObject.name : name;
            Debug.Log($"Merged collision: {winnerName} absorbed {loserName}.", thisWins ? this : null);
        }
    }

    void ApplyMergedState(GameObject targetObject, float combinedMass, float combinedRadius, Vector3 mergedPosition, Vector3 mergedVelocity, Vector3 mergedAngularVelocity)
    {
        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        Rigidbody body = targetObject.GetComponent<Rigidbody>();

        if (properties != null)
        {
            properties.mass = combinedMass;
            properties.radius = combinedRadius;
            targetObject.transform.localScale = new Vector3(combinedRadius * 2f, combinedRadius * 2f, combinedRadius * 2f);
        }
        else
        {
            targetObject.transform.localScale = Vector3.one * (combinedRadius * 2f);
        }

        if (body != null)
        {
            body.mass = combinedMass;
            body.position = mergedPosition;
            body.linearVelocity = mergedVelocity;
            body.angularVelocity = mergedAngularVelocity;
            body.isKinematic = false;
            body.detectCollisions = true;
            body.WakeUp();
        }
        else
        {
            targetObject.transform.position = mergedPosition;
        }

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].enabled = true;
            }
        }

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null)
            {
                colliders[index].enabled = true;
            }
        }
    }

    float GetRadius(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return 0f;
        }

        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        if (properties != null && properties.radius > 0f)
        {
            return properties.radius;
        }

        return Mathf.Max(targetObject.transform.lossyScale.x, targetObject.transform.lossyScale.y, targetObject.transform.lossyScale.z) * 0.5f;
    }

    float GetMass(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return 0f;
        }

        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        if (properties != null && properties.mass > 0f)
        {
            return properties.mass;
        }

        Rigidbody body = targetObject.GetComponent<Rigidbody>();
        if (body != null)
        {
            return body.mass;
        }

        return 1f;
    }
}