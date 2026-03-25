using UnityEngine;
using System.Collections;

public class PlanetDestruction : MonoBehaviour
{
    public GameObject planetFragments; // Parent object of all fragments
    public float explosionForce = 250f;
    [Range(0f, 1f)] public float forceMultiplier = 0.08f;
    public float maxImpulsePerFragment = 20f;
    public float maxDebrisSpeed = 4f;
    public float directionalSpread = 0.8f;
    [Range(0f, 1f)] public float directionalInfluence = 0.35f;
    public float disableCollisionsAfter = 2f;

    private bool hasExploded = false;
    private Rigidbody[] fragmentRigidbodies;
    private Collider[] fragmentColliders;

    void Awake()
    {
        if (planetFragments == null)
        {
            return;
        }

        fragmentRigidbodies = planetFragments.GetComponentsInChildren<Rigidbody>(true);
        fragmentColliders = planetFragments.GetComponentsInChildren<Collider>(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            Explode(collision);
        }
    }

    void Explode(Collision collision)
    {
        if (fragmentRigidbodies == null || fragmentColliders == null)
        {
            return;
        }

        Vector3 impactDirection = GetImpactDirection(collision);

        for (int index = 0; index < fragmentRigidbodies.Length; index++)
        {
            Rigidbody rb = fragmentRigidbodies[index];
            if (rb != null)
            {
                rb.isKinematic = false;
                Vector3 forceDirection = GetSpreadDirection(impactDirection, directionalSpread, directionalInfluence);
                float appliedImpulse = Mathf.Min(explosionForce * forceMultiplier, Mathf.Max(0f, maxImpulsePerFragment));
                rb.AddForce(forceDirection * appliedImpulse, ForceMode.Impulse);
                rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, Mathf.Max(0f, maxDebrisSpeed));
            }
        }

        StartCoroutine(DisableFragmentCollisionsAfterDelay());

        // Optional: hide the main planet mesh
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    IEnumerator DisableFragmentCollisionsAfterDelay()
    {
        yield return new WaitForSeconds(disableCollisionsAfter);

        for (int index = 0; index < fragmentColliders.Length; index++)
        {
            Collider fragmentCollider = fragmentColliders[index];
            if (fragmentCollider != null)
            {
                fragmentCollider.enabled = false;
            }
        }

        for (int index = 0; index < fragmentRigidbodies.Length; index++)
        {
            Rigidbody rb = fragmentRigidbodies[index];
            if (rb != null)
            {
                rb.detectCollisions = false;
                rb.WakeUp();
            }
        }
    }

    Vector3 GetImpactDirection(Collision collision)
    {
        if (collision != null)
        {
            Vector3 relativeVelocity = collision.relativeVelocity;
            if (relativeVelocity.sqrMagnitude > 0.0001f)
            {
                return relativeVelocity.normalized;
            }

            if (collision.contactCount > 0)
            {
                return -collision.GetContact(0).normal;
            }
        }

        return transform.forward;
    }

    Vector3 GetSpreadDirection(Vector3 baseDirection, float spreadAmount, float influence)
    {
        Vector3 randomDirection = Random.onUnitSphere;
        Vector3 mixedDirection = Vector3.Slerp(randomDirection, baseDirection, Mathf.Clamp01(influence));
        Vector3 randomOffset = Random.insideUnitSphere * Mathf.Max(0f, spreadAmount);
        return (mixedDirection + randomOffset).normalized;
    }
}