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
            // 1. Détection de la position de clic
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane spawnPlane = new Plane(Vector3.up, Vector3.zero);      

            if (spawnPlane.Raycast(ray, out float distance))
            {
                Vector3 spawnPosition = ray.GetPoint(distance);
                spawnPosition.y = 0;

                // 2. Identification de l'étoile parente (La sélection actuelle)
                GameObject starSelectionnee = null;
                var click = Camera.main.GetComponent<ClickDetection>();
                
                if (click != null && click.selectedObject != null)
                {
                    var props = click.selectedObject.GetComponent<ObjectProperties>();
                    // On ne peut orbiter que si l'objet sélectionné est une étoile
                    if (props != null && props.isStar)
                    {
                        starSelectionnee = click.selectedObject;
                    }
                }

                if (starSelectionnee == null)
                {
                    Debug.LogWarning("Veuillez sélectionner un Soleil avant de créer une planète !");
                    return;
                }

                // 3. Création de la planète
                GameObject prefabToSpawn = celestialPrefabs[selectedPrefabIndex];
                GameObject planet = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                planetCount++;
                planet.name = "Planet_" + planetCount;

                // 4. Attribution du Soleil Parent
                ObjectProperties planetProps = planet.GetComponent<ObjectProperties>();
                if (planetProps != null)
                {
                    planetProps.EtoileParent = starSelectionnee;
                }

                // 5. CALCUL DE LA VITESSE RELATIVE + VITESSE DU SOLEIL
                Rigidbody sunRb = starSelectionnee.GetComponent<Rigidbody>();
                if (sunRb != null)
                {
                    float G_jeu = GravityManager.G * GravityManager.Instance.gravityMultiplier;
                    float distUnity = Vector3.Distance(spawnPosition, starSelectionnee.transform.position);
                    
                    // Vitesse orbitale pure (v = sqrt(GM/r))
                    float vOrbitaleMag = Mathf.Sqrt((G_jeu * sunRb.mass) / distUnity);

                    // Direction perpendiculaire
                    Vector3 dirVersSoleil = (starSelectionnee.transform.position - spawnPosition).normalized;
                    Vector3 dirTangente = Vector3.Cross(dirVersSoleil, Vector3.up).normalized;
                    
                    Vector3 velociteOrbitalePure = dirTangente * vOrbitaleMag;

                    // --- LA MAGIE EST ICI ---
                    // On additionne la vitesse du soleil pour que la planète l'accompagne
                    Vector3 velociteFinale = velociteOrbitalePure + sunRb.linearVelocity;

                    // 6. Application physique
                    Rigidbody planetRb = planet.GetComponent<Rigidbody>();
                    GravityBody gravityBody = planet.GetComponent<GravityBody>();
                    
                    if (planetRb != null) planetRb.linearVelocity = velociteFinale;
                    if (gravityBody != null)
                    {
                        gravityBody.initialVelocity = velociteFinale;
                        gravityBody.applyInitialVelocity = true;
                    }
                }
            }
        }
    }
}