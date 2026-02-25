using System.Collections.Generic;
using UnityEngine;

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

            OrbitPredictor(bodies[i]);
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

    void OrbitPredictor(GravityBody mainBody) 
    {
        float constanteGravitationnelle = 0.1f;
        int simulationSteps = 1000;
        float timeStep = 0.02f;

        Dictionary<GravityBody, Vector3> positions = new Dictionary<GravityBody, Vector3>();
        Dictionary<GravityBody, Vector3> vitesses = new Dictionary<GravityBody, Vector3>();

        foreach (var body in bodies)
        {
            positions[body] = body.rb.position;
            vitesses[body] = body.rb.linearVelocity;
        }

        List<Vector3> pointsOrbites = new List<Vector3>();

        for (int step = 0; simulationSteps < simulationSteps; simulationSteps++) 
        {
            Dictionary<GravityBody, Vector3> accelerations = new Dictionary<GravityBody, Vector3>();

            foreach (var body in bodies)
            {
                Vector3 accelerationTotale = Vector3.zero;

                foreach (var other in bodies) 
                {
                    if (body == other) continue;

                    Vector3 direction = positions[other] - positions[body];
                    float dist = direction.magnitude + 0.001f;

                    accelerationTotale += constanteGravitationnelle *
                                          other.Mass /
                                          (dist * dist) *
                                          direction.normalized;
                }

                accelerations[body] = accelerationTotale;
            }

            foreach (var body in bodies) 
            {
                vitesses[body] += accelerations[body] * timeStep;
                positions[body] += vitesses[body] * timeStep;
            }

            pointsOrbites.Add(positions[mainBody]);
        }

        LineRenderer line = mainBody.line;
        line.useWorldSpace = true;
        line.positionCount = pointsOrbites.Count;
        line.SetPositions(pointsOrbites.ToArray());
    }
}