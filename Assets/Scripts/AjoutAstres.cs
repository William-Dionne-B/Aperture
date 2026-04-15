using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] celestialPrefabs;
    
    private int planetCount = 0;
    private int selectedPrefabIndex = 0;

    void Start()
    {
        DragDropManager dragDropManager = FindFirstObjectByType<DragDropManager>();
        if (dragDropManager != null)
        {
            dragDropManager.OnButtonPressed.AddListener(OnPrefabButtonPressed);
        }
    }

    void OnDestroy()
    {
        DragDropManager dragDropManager = FindFirstObjectByType<DragDropManager>();
        if (dragDropManager != null)
        {
            dragDropManager.OnButtonPressed.RemoveListener(OnPrefabButtonPressed);
        }
    }

    public void OnPrefabButtonPressed(int buttonID)
    {
        if (celestialPrefabs != null && buttonID >= 0 && buttonID < celestialPrefabs.Length)
        {
            selectedPrefabIndex = buttonID;
            Debug.Log($"Selected prefab changed to: {celestialPrefabs[selectedPrefabIndex].name}");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane spawnPlane = new Plane(Vector3.up, Vector3.zero);      

            if (spawnPlane.Raycast(ray, out float distance))
            {
                Vector3 spawnPosition = ray.GetPoint(distance);
                spawnPosition.y = 0;

                GameObject prefabToSpawn = celestialPrefabs != null && celestialPrefabs.Length > 0 
                    ? celestialPrefabs[selectedPrefabIndex] 
                    : null;

                if (prefabToSpawn == null) return;

                ObjectProperties prefabProps = prefabToSpawn.GetComponent<ObjectProperties>();
                bool doitOrbiter = (prefabProps == null || prefabProps.isOrbitalBody);

                GameObject starSelectionnee = null;
                
                if (ObjectProperties.AllStarsInSystem.Count > 0)
                {
                    float distanceMinimum = float.MaxValue;

                    foreach (ObjectProperties star in ObjectProperties.AllStarsInSystem)
                    {
                        if (star == null) continue;

                        float dist = Vector3.Distance(spawnPosition, star.transform.position);
                        
                        if (dist < distanceMinimum)
                        {
                            distanceMinimum = dist;
                            starSelectionnee = star.gameObject;
                        }
                    }
                }
                
                GameObject astre = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                planetCount++;
                astre.name = "Astre_" + planetCount;

                ObjectProperties astreProps = astre.GetComponent<ObjectProperties>();
                if (astreProps != null && starSelectionnee != null)
                {
                    astreProps.EtoileParent = starSelectionnee;
                }

                if (doitOrbiter && starSelectionnee != null)
                {
                    Rigidbody sunRb = starSelectionnee.GetComponent<Rigidbody>();
                    if (sunRb != null)
                    {
                        float G_jeu = GravityManager.G * GravityManager.Instance.gravityMultiplier;
                        
                        float distUnity = Vector3.Distance(spawnPosition, starSelectionnee.transform.position);
                        
                        float vOrbitaleMag = Mathf.Sqrt((G_jeu * sunRb.mass) / distUnity);
                        Vector3 dirVersSoleil = (starSelectionnee.transform.position - spawnPosition).normalized;
                        Vector3 dirTangente = Vector3.Cross(dirVersSoleil, Vector3.up).normalized;
                        Vector3 velociteOrbitalePure = dirTangente * vOrbitaleMag;

                        Vector3 velociteFinale = velociteOrbitalePure + sunRb.linearVelocity;

                        Rigidbody astreRb = astre.GetComponent<Rigidbody>();
                        GravityBody gravityBody = astre.GetComponent<GravityBody>();
                        
                        if (astreRb != null) astreRb.linearVelocity = velociteFinale;
                        if (gravityBody != null)
                        {
                            gravityBody.initialVelocity = velociteFinale;
                            gravityBody.applyInitialVelocity = true;
                        }
                        
                        Debug.Log($"{astre.name} orbite désormais autour de {starSelectionnee.name} !");
                    }
                }
                else
                {
                    string refDistance = starSelectionnee != null ? $" (Distance mesurée par rapport à {starSelectionnee.name})" : "";
                    Debug.Log($"{astre.name} créé en tant que corps libre{refDistance}.");
                }
            }
        }
    }
}