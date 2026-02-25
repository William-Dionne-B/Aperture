using System.Collections.Generic;
using UnityEngine;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Scaled gravity constant

    public float gravityMultiplier = 1e13f; // Tune this for fun & stability
    public float softening = 0.1f; // Prevents singularities / explosions

    private static readonly List<GravityBody> bodies = new List<GravityBody>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Change simulation speed with number keys 1, 2, 3, etc.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSimulationSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSimulationSpeed(3f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSimulationSpeed(6f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSimulationSpeed(8f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetSimulationSpeed(10f);
    }

    public void SetSimulationSpeed(float speed)
    {
        Time.timeScale = speed;
        
        if (speed > 0f)
        {
            Time.fixedDeltaTime = 0.02f / speed; 
        }
        
        Debug.Log("Simulation speed set to " + speed + "x");
    }

    public static void Register(GravityBody body)
    {
        if (!bodies.Contains(body))
            bodies.Add(body);
    }

    public static void Unregister(GravityBody body)
    {
        bodies.Remove(body);
    }

    void FixedUpdate()
    {
        int count = bodies.Count;

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                ApplyGravity(bodies[i], bodies[j]);
            }
        }
    }

    void ApplyGravity(GravityBody a, GravityBody b)
    {
        Vector3 direction = b.rb.position - a.rb.position;
        float distanceSqr = direction.sqrMagnitude + softening;

        float forceMagnitude =
            gravityMultiplier *
            G *
            (a.rb.mass * b.rb.mass) /
            distanceSqr;

        Vector3 force = direction.normalized * forceMagnitude;

        a.rb.AddForce(force);
        b.rb.AddForce(-force);
    }
}
