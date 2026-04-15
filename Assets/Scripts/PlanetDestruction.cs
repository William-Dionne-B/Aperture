using System;
using System.Collections;
using UnityEngine;

public class PlanetDestruction : MonoBehaviour
{
    public static event Action<PlanetDestruction, Collision> CollisionDetected;

    [Header("Merge Rules")]
    public bool requireGravityBodyOnOther = false;
    public bool logCollisionDetection = true;

    [Header("Merge Visuals")]
    public float mergeDuration = 0.35f;
    public AnimationCurve mergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

        StartCoroutine(MergeWithRoutine(otherObject, otherDestruction));
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
        float thisMass = GetMass(gameObject);
        float otherMass = GetMass(otherObject);

        if (thisMass > otherMass)
        {
            return true;
        }

        if (thisMass < otherMass)
        {
            return false;
        }

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

    IEnumerator MergeWithRoutine(GameObject otherObject, PlanetDestruction otherDestruction)
    {
        if (otherObject == null)
        {
            isMerging = false;
            if (otherDestruction != null)
            {
                otherDestruction.isMerging = false;
            }
            yield break;
        }

        bool thisWins = ShouldHandleMerge(otherObject);
        GameObject winner = thisWins ? gameObject : otherObject;
        GameObject loser = thisWins ? otherObject : gameObject;

        PlanetDestruction winnerDestruction = thisWins ? this : otherDestruction;
        PlanetDestruction loserDestruction = thisWins ? otherDestruction : this;

        Rigidbody winnerRigidbody = winner != null ? winner.GetComponent<Rigidbody>() : null;
        Rigidbody loserRigidbody = loser != null ? loser.GetComponent<Rigidbody>() : null;

        float winnerRadius = GetRadius(winner);
        float loserRadius = GetRadius(loser);
        float winnerMass = GetMass(winner);
        float loserMass = GetMass(loser);

        float combinedMass = Mathf.Max(0f, winnerMass + loserMass);
        float combinedRadius = Mathf.Pow(Mathf.Pow(Mathf.Max(0f, winnerRadius), 3f) + Mathf.Pow(Mathf.Max(0f, loserRadius), 3f), 1f / 3f);

        Vector3 winnerPosition = winnerRigidbody != null ? winnerRigidbody.position : winner.transform.position;
        Vector3 loserPosition = loserRigidbody != null ? loserRigidbody.position : loser.transform.position;
        Vector3 mergedPosition = combinedMass > 0f
            ? ((winnerPosition * winnerMass) + (loserPosition * loserMass)) / combinedMass
            : winner.transform.position;

        Vector3 winnerVelocity = winnerRigidbody != null ? winnerRigidbody.linearVelocity : Vector3.zero;
        Vector3 loserVelocity = loserRigidbody != null ? loserRigidbody.linearVelocity : Vector3.zero;
        Vector3 mergedVelocity = combinedMass > 0f
            ? ((winnerVelocity * winnerMass) + (loserVelocity * loserMass)) / combinedMass
            : Vector3.zero;

        Vector3 winnerAngularVelocity = winnerRigidbody != null ? winnerRigidbody.angularVelocity : Vector3.zero;
        Vector3 loserAngularVelocity = loserRigidbody != null ? loserRigidbody.angularVelocity : Vector3.zero;
        Vector3 mergedAngularVelocity = combinedMass > 0f
            ? ((winnerAngularVelocity * winnerMass) + (loserAngularVelocity * loserMass)) / combinedMass
            : winnerAngularVelocity;

        Vector3 winnerStartScale = winner.transform.localScale;
        Vector3 loserStartScale = loser.transform.localScale;
        Vector3 combinedScale = Vector3.one * (combinedRadius * 2f);

        if (winnerRigidbody != null)
        {
            winnerRigidbody.isKinematic = true;
            winnerRigidbody.detectCollisions = false;
            winnerRigidbody.linearVelocity = Vector3.zero;
            winnerRigidbody.angularVelocity = Vector3.zero;
        }

        if (loserRigidbody != null)
        {
            loserRigidbody.isKinematic = true;
            loserRigidbody.detectCollisions = false;
            loserRigidbody.linearVelocity = Vector3.zero;
            loserRigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(loser, false);

        float duration = Mathf.Max(0.01f, mergeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (winner == null || loser == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float blend = mergeCurve != null ? mergeCurve.Evaluate(normalized) : normalized;

            Vector3 winnerStepPosition = Vector3.Lerp(winnerPosition, mergedPosition, blend);
            Vector3 loserStepPosition = Vector3.Lerp(loserPosition, mergedPosition, blend * blend);

            SetObjectPosition(winner, winnerRigidbody, winnerStepPosition);
            SetObjectPosition(loser, loserRigidbody, loserStepPosition);

            winner.transform.localScale = Vector3.Lerp(winnerStartScale, combinedScale, blend);
            loser.transform.localScale = Vector3.Lerp(loserStartScale, Vector3.zero, blend);

            yield return null;
        }

        if (winner != null)
        {
            ApplyMergedState(winner, combinedMass, combinedRadius, mergedPosition, mergedVelocity, mergedAngularVelocity);
        }

        if (loser != null)
        {
            Destroy(loser);
        }

        if (logCollisionDetection)
        {
            string winnerName = winner != null ? winner.name : "Unknown";
            string loserName = loser != null ? loser.name : "Unknown";
            Debug.Log($"Merged collision: {winnerName} absorbed {loserName}.", winnerDestruction);
        }

        if (winnerDestruction != null)
        {
            winnerDestruction.isMerging = false;
        }

        if (loserDestruction != null)
        {
            loserDestruction.isMerging = false;
        }
    }

    void SetObjectPosition(GameObject targetObject, Rigidbody body, Vector3 position)
    {
        if (body != null)
        {
            body.position = position;
            return;
        }

        if (targetObject != null)
        {
            targetObject.transform.position = position;
        }
    }

    void SetCollidersEnabled(GameObject targetObject, bool enabledState)
    {
        if (targetObject == null)
        {
            return;
        }

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null)
            {
                colliders[index].enabled = enabledState;
            }
        }
    }

    void ApplyMergedState(GameObject targetObject, float combinedMass, float combinedRadius, Vector3 mergedPosition, Vector3 mergedVelocity, Vector3 mergedAngularVelocity)
    {
        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        Rigidbody body = targetObject.GetComponent<Rigidbody>();

        if (properties != null)
        {
            properties.Mass = combinedMass;
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
        if (properties != null && properties.Mass > 0f)
        {
            return properties.Mass;
        }

        Rigidbody body = targetObject.GetComponent<Rigidbody>();
        if (body != null)
        {
            return body.mass;
        }

        return 1f;
    }
}