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
    private float stayRetryTimer = 0f;
    private const float StayRetryInterval = 0.3f;

    private Rigidbody sourceRigidbody;
    private ObjectProperties sourceProperties;
    private Renderer[] sourceRenderers;
    private Collider[] sourceColliders;

    /// <summary>
    /// Met en cache les references utiles pour la fusion.
    /// </summary>
    void Awake()
    {
        sourceRigidbody = GetComponent<Rigidbody>();
        sourceProperties = GetComponent<ObjectProperties>();
        sourceRenderers = GetComponentsInChildren<Renderer>(true);
        sourceColliders = GetComponentsInChildren<Collider>(true);
    }

    /// <summary>
    /// Detecte une collision et tente de lancer une fusion.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        CollisionDetected?.Invoke(this, collision);
        TryBeginMerge(collision);
    }

    /// <summary>
    /// Retente la fusion a intervalle regulier pendant la collision.
    /// </summary>
    void OnCollisionStay(Collision collision)
    {
        if (isMerging) return;
        stayRetryTimer -= Time.deltaTime;
        if (stayRetryTimer > 0f) return;
        stayRetryTimer = StayRetryInterval;
        TryBeginMerge(collision);
    }

    /// <summary>
    /// Verifie les conditions et demarre la fusion si possible.
    /// </summary>
    void TryBeginMerge(Collision collision)
    {
        if (isMerging || collision == null) return;

        GameObject otherObject = GetCollisionObject(collision);
        if (otherObject == null || otherObject == gameObject) return;

        PlanetDestruction otherDestruction = otherObject.GetComponent<PlanetDestruction>();
        if (otherDestruction != null && otherDestruction.isMerging) return;

        if (requireGravityBodyOnOther && otherObject.GetComponent<GravityBody>() == null) return;

        if (!ShouldHandleMerge(otherObject)) return;

        isMerging = true;
        if (otherDestruction != null) otherDestruction.isMerging = true;

        StartCoroutine(MergeWithRoutine(otherObject, otherDestruction));
    }

    /// <summary>
    /// Recupere l'objet principal touche par la collision.
    /// </summary>
    GameObject GetCollisionObject(Collision collision)
    {
        if (collision.collider == null) return null;

        Rigidbody otherRigidbody = collision.collider.attachedRigidbody;
        if (otherRigidbody != null) return otherRigidbody.gameObject;

        return collision.collider.transform.root.gameObject;
    }

    /// <summary>
    /// Determine quel objet doit gerer la fusion.
    /// </summary>
    bool ShouldHandleMerge(GameObject otherObject)
    {
        float thisMass = GetMass(gameObject);
        float otherMass = GetMass(otherObject);

        if (thisMass > otherMass) return true;
        if (thisMass < otherMass) return false;

        float thisRadius = GetRadius(gameObject);
        float otherRadius = GetRadius(otherObject);

        if (thisRadius > otherRadius) return true;
        if (thisRadius < otherRadius) return false;

        return GetInstanceID() < otherObject.GetInstanceID();
    }

    /// <summary>
    /// Anime et applique la fusion entre deux corps.
    /// </summary>
    IEnumerator MergeWithRoutine(GameObject otherObject, PlanetDestruction otherDestruction)
    {
        if (otherObject == null)
        {
            isMerging = false;
            if (otherDestruction != null) otherDestruction.isMerging = false;
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
        float combinedRadius = Mathf.Pow(
            Mathf.Pow(Mathf.Max(0f, winnerRadius), 3f) + Mathf.Pow(Mathf.Max(0f, loserRadius), 3f),
            1f / 3f
        );

        Vector3 winnerPosition = winnerRigidbody != null ? winnerRigidbody.position : winner.transform.position;
        Vector3 loserPosition = loserRigidbody != null ? loserRigidbody.position : loser.transform.position;

        Vector3 winnerVelocity = winnerRigidbody != null ? winnerRigidbody.linearVelocity : Vector3.zero;
        Vector3 loserVelocity = loserRigidbody != null ? loserRigidbody.linearVelocity : Vector3.zero;
        Vector3 winnerAngularVelocity = winnerRigidbody != null ? winnerRigidbody.angularVelocity : Vector3.zero;
        Vector3 loserAngularVelocity = loserRigidbody != null ? loserRigidbody.angularVelocity : Vector3.zero;

        Vector3 finalVelocity = combinedMass > 0f
            ? ((winnerVelocity * winnerMass) + (loserVelocity * loserMass)) / combinedMass
            : winnerVelocity;
        Vector3 finalAngularVelocity = combinedMass > 0f
            ? ((winnerAngularVelocity * winnerMass) + (loserAngularVelocity * loserMass)) / combinedMass
            : winnerAngularVelocity;

        Vector3 winnerStartScale = winner.transform.localScale;
        Vector3 loserStartScale = loser.transform.localScale;
        Vector3 combinedScale = Vector3.one * (combinedRadius * 2f);

        if (loserRigidbody != null)
        {
            loserRigidbody.linearVelocity = Vector3.zero;
            loserRigidbody.angularVelocity = Vector3.zero;
            loserRigidbody.isKinematic = true;
            loserRigidbody.detectCollisions = false;
        }

        if (winnerRigidbody != null)
        {
            winnerRigidbody.linearVelocity = Vector3.zero;
            winnerRigidbody.angularVelocity = Vector3.zero;
            winnerRigidbody.isKinematic = true;
            winnerRigidbody.detectCollisions = false;
        }

        SetCollidersEnabled(loser, false);
        SetCollidersEnabled(winner, false);

        float duration = Mathf.Max(0.01f, mergeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (winner == null || loser == null)
            {
                if (winnerDestruction != null) winnerDestruction.isMerging = false;
                if (loserDestruction != null) loserDestruction.isMerging = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float blend = mergeCurve != null ? mergeCurve.Evaluate(normalized) : normalized;

            Vector3 loserStepPosition = Vector3.Lerp(loserPosition, winnerPosition, blend * blend);
            SetObjectPosition(loser, loserRigidbody, loserStepPosition);

            winner.transform.localScale = Vector3.Lerp(winnerStartScale, combinedScale, blend);
            loser.transform.localScale = Vector3.Lerp(loserStartScale, Vector3.zero, blend);

            yield return null;
        }

        if (winnerRigidbody != null)
        {
            winnerRigidbody.isKinematic = false;
            winnerRigidbody.detectCollisions = true;
        }

        if (winner != null)
        {
            ApplyMergedState(winner, combinedMass, combinedRadius, finalVelocity, finalAngularVelocity);
        }

        if (loser != null)
        {
            DetachCamerasParentedTo(loser);
            Destroy(loser);
        }

        if (logCollisionDetection)
        {
            string winnerName = winner != null ? winner.name : "Unknown";
            string loserName = loser != null ? loser.name : "Unknown";
            Debug.Log($"Merged collision: {winnerName} absorbed {loserName}.", winnerDestruction);
        }

        if (winnerDestruction != null) winnerDestruction.isMerging = false;
        if (loserDestruction != null) loserDestruction.isMerging = false;

        if (winner != null)
        {
            PlanetDestruction winnerPD = winner.GetComponent<PlanetDestruction>();
            if (winnerPD != null) winnerPD.StartCoroutine(winnerPD.CheckOverlappingAfterMerge());
        }
    }

    /// <summary>
    /// Verifie les recouvrements apres la fusion pour enchainees.
    /// </summary>
    IEnumerator CheckOverlappingAfterMerge()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (gameObject == null || isMerging) yield break;

        float radius = GetRadius(gameObject);
        Collider[] hits = Physics.OverlapSphere(transform.position, radius * 1.05f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            Rigidbody rb = hits[i].attachedRigidbody;
            GameObject other = rb != null ? rb.gameObject : hits[i].transform.root.gameObject;

            if (other == null || other == gameObject) continue;
            if (isMerging) break;

            PlanetDestruction otherPD = other.GetComponent<PlanetDestruction>();
            if (otherPD != null && otherPD.isMerging) continue;
            if (requireGravityBodyOnOther && other.GetComponent<GravityBody>() == null) continue;
            if (!ShouldHandleMerge(other)) continue;

            isMerging = true;
            if (otherPD != null) otherPD.isMerging = true;
            StartCoroutine(MergeWithRoutine(other, otherPD));
            break;
        }
    }

    /// <summary>
    /// Deplace un objet en preferant le rigidbody si present.
    /// </summary>
    void SetObjectPosition(GameObject targetObject, Rigidbody body, Vector3 position)
    {
        if (body != null)
        {
            body.position = position;
            return;
        }

        if (targetObject != null) targetObject.transform.position = position;
    }

    /// <summary>
    /// Active ou desactive tous les colliders d'un objet.
    /// </summary>
    void SetCollidersEnabled(GameObject targetObject, bool enabledState)
    {
        if (targetObject == null) return;

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null) colliders[index].enabled = enabledState;
        }
    }

    /// <summary>
    /// Applique masse, rayon et vitesses a l'objet vainqueur.
    /// </summary>
    void ApplyMergedState(GameObject targetObject, float combinedMass, float combinedRadius, Vector3 mergedVelocity, Vector3 mergedAngularVelocity)
    {
        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        Rigidbody body = targetObject.GetComponent<Rigidbody>();
        GravityBody gravityBody = targetObject.GetComponent<GravityBody>();

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
            body.linearVelocity = mergedVelocity;
            body.angularVelocity = mergedAngularVelocity;
        }

        if (gravityBody != null)
        {
            gravityBody.initialVelocity = mergedVelocity;
            gravityBody.applyInitialVelocity = false;
            gravityBody.Mass = combinedMass;
        }

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null) renderers[index].enabled = true;
        }

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            if (colliders[index] != null) colliders[index].enabled = true;
        }
    }

    /// <summary>
    /// Calcule le rayon a partir des proprietes ou de l'echelle.
    /// </summary>
    float GetRadius(GameObject targetObject)
    {
        if (targetObject == null) return 0f;

        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        if (properties != null && properties.radius > 0f) return properties.radius;

        return Mathf.Max(
            targetObject.transform.lossyScale.x,
            targetObject.transform.lossyScale.y,
            targetObject.transform.lossyScale.z
        ) * 0.5f;
    }

    /// <summary>
    /// Calcule la masse a partir des proprietes ou du rigidbody.
    /// </summary>
    float GetMass(GameObject targetObject)
    {
        if (targetObject == null) return 0f;

        ObjectProperties properties = targetObject.GetComponent<ObjectProperties>();
        if (properties != null && properties.Mass > 0f) return properties.Mass;

        Rigidbody body = targetObject.GetComponent<Rigidbody>();
        if (body != null) return body.mass;

        return 1f;
    }

    /// <summary>
    /// Detache les cameras d'un objet avant sa destruction.
    /// </summary>
    void DetachCamerasParentedTo(GameObject target)
    {
        if (target == null) return;

        Camera[] allCams = FindObjectsOfType<Camera>();
        for (int i = 0; i < allCams.Length; i++)
        {
            var cam = allCams[i];
            if (cam == null) continue;
            if (cam.transform.IsChildOf(target.transform))
            {
                cam.transform.SetParent(null, true);
            }
        }

        GameObject mainAnchor = GameObject.Find("MainCameraAnchor");
        if (mainAnchor != null && mainAnchor.transform.IsChildOf(target.transform))
        {
            for (int i = mainAnchor.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = mainAnchor.transform.GetChild(i);
                if (child != null) child.SetParent(null, true);
            }
            Destroy(mainAnchor);
        }

        if (Camera.main != null)
        {
            var click = Camera.main.GetComponent<ClickDetection>();
            if (click != null && click.selectedObject == target)
            {
                click.selectedObject = null;
            }
        }
    }
}