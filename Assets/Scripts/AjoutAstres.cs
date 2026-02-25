using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    public GameObject planetPrefab;
    public float spawnDistance = 200f;

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
            }
        }
    }
}
