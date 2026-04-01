using UnityEngine;

public class PlanetExplosion : MonoBehaviour
{
    public GameObject explosionFX;
    public float impactThreshold = 10f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > impactThreshold)
        {
            Vector3 hitPoint = collision.contacts[0].point;

            Explode(hitPoint);
        }
    }

    void Explode(Vector3 position)
    {
        GameObject fx = Instantiate(explosionFX, position, Quaternion.identity);

        Destroy(fx, 2f);

        Light light = new GameObject("Flash").AddComponent<Light>();
        light.transform.position = position;
        light.intensity = 5f;
        light.range = 50f;

        Destroy(light.gameObject, 0.2f);

    }
}