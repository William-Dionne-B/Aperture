using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] celestialPrefabs; // Array of different prefabs (planets, stars, black holes, etc.)
    
    [Header("Sun Settings")]
    public float sunMass = 1000000;
    public Transform sunTransform;

    private int planetCount = 0;
    private int selectedPrefabIndex = 0;

    void Start()
    {
        // Subscribe to DragDropManager button events
        DragDropManager dragDropManager = FindFirstObjectByType<DragDropManager>();
        if (dragDropManager != null)
        {
            dragDropManager.OnButtonPressed.AddListener(OnPrefabButtonPressed);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe when destroyed
        DragDropManager dragDropManager = FindFirstObjectByType<DragDropManager>();
        if (dragDropManager != null)
        {
            dragDropManager.OnButtonPressed.RemoveListener(OnPrefabButtonPressed);
        }
    }

    /// <summary>
    /// Called when a prefab selection button is pressed
    /// </summary>
    public void OnPrefabButtonPressed(int buttonID)
    {
        if (celestialPrefabs != null && buttonID >= 0 && buttonID < celestialPrefabs.Length)
        {
            selectedPrefabIndex = buttonID;
            Debug.Log($"Selected prefab changed to: {celestialPrefabs[selectedPrefabIndex].name}");
        }
        else
        {
            Debug.LogWarning($"Invalid button ID: {buttonID}. Array has {celestialPrefabs?.Length ?? 0} prefabs.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Plane spawnPlane = new Plane(
                Vector3.up,
                Vector3.zero
            );      

            if (spawnPlane.Raycast(ray, out float distance))
            {
                Vector3 spawnPosition = ray.GetPoint(distance);
                spawnPosition.y = 0;

                // Get the currently selected prefab
                GameObject prefabToSpawn = celestialPrefabs != null && celestialPrefabs.Length > 0 
                    ? celestialPrefabs[selectedPrefabIndex] 
                    : null;

                if (prefabToSpawn == null)
                {
                    Debug.LogWarning("No prefab selected or prefab array is empty!");
                    return;
                }

                GameObject planet = Instantiate(
                    prefabToSpawn,
                    spawnPosition,
                    Quaternion.identity
                );

                planetCount++;
                planet.name = "Planet_" + planetCount;

                // Calculate and apply orbital velocity using the center of mass as reference
                Vector3 centerOfMass = GravityManager.GetCenterOfMass();
                Vector3 vitesseOrbitale = CalculateurVitesseOptimale.CalculerVitesseOrbitaleStatique(
                    spawnPosition,
                    sunMass,
                    centerOfMass
                );

                // Apply velocity to the planet
                GravityBody gravityBody = planet.GetComponent<GravityBody>();
                if (gravityBody != null)
                {
                    gravityBody.initialVelocity = vitesseOrbitale;
                    gravityBody.applyInitialVelocity = true;
                    
                    if (gravityBody.rb != null)
                    {
                        gravityBody.rb.linearVelocity = vitesseOrbitale;
                    }
                }
            }
        }
    }
}
