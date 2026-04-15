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

    public void OnPrefabButtonPressed(int buttonID)
    {
        if (celestialPrefabs != null && buttonID >= 0 && buttonID < celestialPrefabs.Length)
        {
            selectedPrefabIndex = buttonID;
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

                // 1. On lit les propriétés du Prefab AVANT de le créer
                ObjectProperties prefabProps = prefabToSpawn.GetComponent<ObjectProperties>();
                bool doitOrbiter = (prefabProps == null || prefabProps.isOrbitalBody);

                // ==========================================
                // 2. LE NOUVEAU RADAR : Recherche du Soleil le plus proche
                // ==========================================
                GameObject starSelectionnee = null;
                
                if (doitOrbiter && ObjectProperties.AllStarsInSystem.Count > 0)
                {
                    float distanceMinimum = float.MaxValue;

                    // On passe en revue TOUTES les étoiles enregistrées dans le jeu
                    foreach (ObjectProperties star in ObjectProperties.AllStarsInSystem)
                    {
                        if (star == null) continue;

                        // On calcule la distance entre le clic de la souris et cette étoile
                        float dist = Vector3.Distance(spawnPosition, star.transform.position);
                        
                        // Si c'est la plus proche trouvée jusqu'à présent, on la mémorise !
                        if (dist < distanceMinimum)
                        {
                            distanceMinimum = dist;
                            starSelectionnee = star.gameObject;
                        }
                    }
                }
                // ==========================================

                // 3. Création de l'astre
                GameObject astre = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                planetCount++;
                astre.name = "Astre_" + planetCount;

                // 4. Application de la vitesse orbitale si on a trouvé un soleil proche
                if (doitOrbiter && starSelectionnee != null)
                {
                    ObjectProperties astreProps = astre.GetComponent<ObjectProperties>();
                    if (astreProps != null) astreProps.EtoileParent = starSelectionnee;

                    Rigidbody sunRb = starSelectionnee.GetComponent<Rigidbody>();
                    if (sunRb != null)
                    {
                        float G_jeu = GravityManager.G * GravityManager.Instance.gravityMultiplier;
                        
                        // On utilise la distance avec l'étoile trouvée
                        float distUnity = Vector3.Distance(spawnPosition, starSelectionnee.transform.position);
                        
                        // Calcul Vitesse
                        float vOrbitaleMag = Mathf.Sqrt((G_jeu * sunRb.mass) / distUnity);
                        Vector3 dirVersSoleil = (starSelectionnee.transform.position - spawnPosition).normalized;
                        Vector3 dirTangente = Vector3.Cross(dirVersSoleil, Vector3.up).normalized;
                        Vector3 velociteOrbitalePure = dirTangente * vOrbitaleMag;

                        // Addition avec la vitesse de l'étoile parente
                        Vector3 velociteFinale = velociteOrbitalePure + sunRb.linearVelocity;

                        // Application Physique
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
                    // Si aucune étoile n'existe dans la scène, la planète devient orpheline
                    Debug.Log($"{astre.name} créé en tant que corps libre (Aucune étoile à proximité).");
                }
            }
        }
    }
}