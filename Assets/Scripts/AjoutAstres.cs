using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    public GameObject planetPrefab;
    public float sunMass = 1000000;
    public Transform sunTransform;

    private int planetCount = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
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

                GameObject planet = Instantiate(
                    planetPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

                planetCount++;
                planet.name = "Planet_" + planetCount;

                // Calculate and apply orbital velocity using the calculator
                Vector3 sunPosition = sunTransform != null ? sunTransform.position : Vector3.zero;
                Vector3 vitesseOrbitale = CalculateurVitesseOptimale.CalculerVitesseOrbitaleStatique(
                    spawnPosition, 
                    sunMass, 
                    sunPosition
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
