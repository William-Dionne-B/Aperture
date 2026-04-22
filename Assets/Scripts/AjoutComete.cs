using UnityEngine;
using System.Collections.Generic;

public class AjoutComete : MonoBehaviour
{
    [Header("Input")]
    public KeyCode shootKey = KeyCode.F;

    [Header("Références")]
    public Camera sourceCamera;
    public GameObject asteroidPrefab;

    [Header("Projectile")]
    [Min(0.01f)] public float spawnDistance = 2f;
    [Min(0.01f)] public float projectileSpeed = 30f;
    [Min(0.01f)] public float asteroidScale = 2f;
    [Min(0.001f)] public float asteroidMass = 100000f;
    [Min(1f)] public float maxDistanceFromCamera = 2000f;

    readonly Queue<GameObject> activeProjectiles = new Queue<GameObject>();

    void Awake()
    {
        if (sourceCamera == null)
        {
            sourceCamera = GetComponent<Camera>();
        }

        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }
    }

    void Update()
    {
        CleanupFarProjectiles();

        if (Input.GetKeyDown(shootKey))
        {
            ShootAsteroid();
        }
    }

    void OnDestroy()
    {
        activeProjectiles.Clear();
    }

    void ShootAsteroid()
    {
        Camera cameraToUse = sourceCamera != null ? sourceCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.LogWarning("AjoutComete: aucune caméra trouvée pour tirer la comète.");
            return;
        }

        Ray centerRay = cameraToUse.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 shootDirection = centerRay.direction.normalized;
        Vector3 spawnPosition = centerRay.origin + shootDirection * spawnDistance;

        GameObject asteroid = asteroidPrefab != null
            ? Instantiate(asteroidPrefab, spawnPosition, Quaternion.identity)
            : CreateFallbackAsteroid(spawnPosition);

        asteroid.transform.localScale = Vector3.one * asteroidScale;

        Rigidbody rb = asteroid.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = asteroid.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.mass = asteroidMass;
        rb.linearVelocity = shootDirection * projectileSpeed;

        GravityBody gravityBody = asteroid.GetComponent<GravityBody>();
        if (gravityBody != null)
        {
            gravityBody.initialVelocity = rb.linearVelocity;
            gravityBody.applyInitialVelocity = true;
            gravityBody.Mass = asteroidMass;
        }

        ObjectProperties objectProperties = asteroid.GetComponent<ObjectProperties>();
        if (objectProperties != null)
        {
            objectProperties.radius = asteroidScale * 0.5f;
            objectProperties.Mass = asteroidMass;
        }

        RegisterProjectile(asteroid);
    }

    GameObject CreateFallbackAsteroid(Vector3 spawnPosition)
    {
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.name = "AsteroidProjectile";
        fallback.transform.position = spawnPosition;
        return fallback;
    }

    void RegisterProjectile(GameObject projectile)
    {
        RemoveMissingProjectiles();
        activeProjectiles.Enqueue(projectile);
    }

    void RemoveMissingProjectiles()
    {
        while (activeProjectiles.Count > 0 && activeProjectiles.Peek() == null)
        {
            activeProjectiles.Dequeue();
        }
    }

    void CleanupFarProjectiles()
    {
        if (activeProjectiles.Count == 0)
        {
            return;
        }

        Camera cameraToUse = sourceCamera != null ? sourceCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Vector3 cameraPosition = cameraToUse.transform.position;
        float maxDistanceSqr = maxDistanceFromCamera * maxDistanceFromCamera;
        int projectileCount = activeProjectiles.Count;

        for (int i = 0; i < projectileCount; i++)
        {
            GameObject projectile = activeProjectiles.Dequeue();
            if (projectile == null)
            {
                continue;
            }

            float distanceSqr = (projectile.transform.position - cameraPosition).sqrMagnitude;
            if (distanceSqr > maxDistanceSqr)
            {
                Destroy(projectile);
                continue;
            }

            activeProjectiles.Enqueue(projectile);
        }
    }
}
