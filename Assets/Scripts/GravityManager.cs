using NUnit.Framework.Constraints;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Scaled gravity constant

    public float gravityMultiplier = 1e13f; // Tune this for fun & stability
    public float softening = 0.1f; // Prevents singularities / explosions
    public float Timestep = 3600f;

    private static readonly List<GravityBody> bodies = new List<GravityBody>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Change simulation speed with number keys 1, 2, 3, etc.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSimulationSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSimulationSpeed(2f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSimulationSpeed(3f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSimulationSpeed(4f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetSimulationSpeed(5f);
    }

    public void SetSimulationSpeed(float speed)
    {
        Time.timeScale = speed;
        Time.fixedDeltaTime = 0.02f / speed; // Keep physics stable
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
        //int count = bodies.Count;

        //for (int i = 0; i < count; i++)
        //{
        //    for (int j = i + 1; j < count; j++)
        //    {
        //        ApplyGravity(bodies[i], bodies[j]);
        //    }
        //}
        Step(Timestep);
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

    void Step(float dt)
    {
        int count = bodies.Count;

        Vector3[] oldAcc = new Vector3[count];

        for (int i = 0; i < count; i++) 
        {
            oldAcc[i] = ComputeAcc(i);
        }

        for (int i = 0; i < count; i++)
        {
            GravityBody p  = bodies[i];
            Vector3 a = oldAcc[i];

            p.rb.position += p.rb.linearVelocity * dt + 0.5f * a * dt * dt;
            p.rb.linearVelocity += 0.5f * a * dt;
        }

        Vector3[] newAccel = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            newAccel[i] = ComputeAcc(i);
        }

        for (int i = 0; i < count; i++)
        {
            GravityBody p = bodies[i];
            p.rb.linearVelocity += 0.5f * newAccel[i] * dt;
        }
        
    }

    Vector3 ComputeAcc(int index)
    {
        GravityBody pi = bodies[index];
        Vector3 acc = Vector3.zero;

        for (int j = 0; j < bodies.Count; j++)
        {
            if (j == index) continue;

            GravityBody pj = bodies[j];
            Vector3 r = pj.rb.position - pi.rb.position;
            float distSqr = r.sqrMagnitude + Softening * Softening;
            float invDistCube = 1f / Mathf.Pow(distSqr, 1.5f);
            acc += G * pj.Mass * r * invDistCube;
        }

        return acc;
    }
}
