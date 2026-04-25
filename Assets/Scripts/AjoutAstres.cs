using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    private GameObject selectedPrefab;
    private int spawnCount = 0;

    public void SetPrefab(GameObject prefab)
    {
        selectedPrefab = prefab;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (selectedPrefab == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (!plane.Raycast(ray, out float distance)) return;

        Vector3 spawnPos = ray.GetPoint(distance);
        spawnPos.y = 0f;

        GameObject instance = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        spawnCount++;
        instance.name = $"Astre_{spawnCount}";

        Debug.Log($"[PlanetSpawner] Spawned '{instance.name}' at {spawnPos}.");

        ObjectProperties prefabProps = selectedPrefab.GetComponent<ObjectProperties>();
        bool shouldOrbit = (prefabProps == null || prefabProps.isOrbitalBody);

        if (!shouldOrbit || ObjectProperties.AllStarsInSystem == null || ObjectProperties.AllStarsInSystem.Count == 0)
        {
            Debug.Log($"[PlanetSpawner] '{instance.name}' spawned as a free body.");
            return;
        }

        // Find nearest star
        GameObject nearestStar = null;
        float minDist = float.MaxValue;

        foreach (ObjectProperties star in ObjectProperties.AllStarsInSystem)
        {
            if (star == null) continue;
            float dist = Vector3.Distance(spawnPos, star.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestStar = star.gameObject;
            }
        }

        if (nearestStar == null)
        {
            Debug.Log($"[PlanetSpawner] No valid star found, '{instance.name}' spawned as a free body.");
            return;
        }

        Rigidbody starRb = nearestStar.GetComponent<Rigidbody>();
        if (starRb == null)
        {
            Debug.LogWarning($"[PlanetSpawner] Nearest star '{nearestStar.name}' has no Rigidbody.");
            return;
        }

        float G = GravityManager.G * GravityManager.Instance.gravityMultiplier;
        Vector3 toStar = (nearestStar.transform.position - spawnPos).normalized;
        Vector3 tangent = Vector3.Cross(toStar, Vector3.up).normalized;
        float orbitalSpeed = Mathf.Sqrt((G * starRb.mass) / minDist);
        Vector3 orbitalVelocity = -1.0f * (tangent * orbitalSpeed + starRb.linearVelocity);

        Rigidbody instanceRb = instance.GetComponent<Rigidbody>();
        GravityBody gravityBody = instance.GetComponent<GravityBody>();

        if (instanceRb != null) instanceRb.linearVelocity = orbitalVelocity;
        if (gravityBody != null)
        {
            gravityBody.initialVelocity = orbitalVelocity;
            gravityBody.applyInitialVelocity = true;
        }

        Debug.Log($"[PlanetSpawner] '{instance.name}' now orbiting '{nearestStar.name}' at speed {orbitalSpeed:F2}.");
    }
}