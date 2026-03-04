using System.Collections.Generic;
using Unity.VisualScripting;
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

        public static Vector3 GetCenterOfMass()
        {
            if (bodies.Count == 0)
                return Vector3.zero;

            Vector3 weightedSum = Vector3.zero;
            float totalMass = 0f;

            foreach (var body in bodies)
            {
                if (body != null && body.rb != null)
                {
                    weightedSum += body.rb.position * body.rb.mass;
                    totalMass += body.rb.mass;
                }
            }

            if (totalMass == 0f)
                return Vector3.zero;

            return weightedSum / totalMass;
        }

    void OrbitPredictor(GravityBody mainBody) 
    {
        float constanteGravitationnelle = 1.5e6f;
        int simulationSteps = 150;
        float timeStep = 0.1f;

        int count = bodies.Count;

        Vector3[] positions = new Vector3[count];
        Vector3[] vitesses = new Vector3[count];
        float[] masses = new float[count];

        for (int i = 0; i < count; i++)
        {
            positions[i] = bodies[i].rb.position;
            vitesses[i] = bodies[i].rb.linearVelocity;
            masses[i] = bodies[i].rb.mass;
        }

        Vector3[] accelerations = new Vector3[count];
        Vector3[] newAccelerations = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            accelerations[i] = Vector3.zero;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                Vector3 direction = positions[j] - positions[i];
                float distance = direction.magnitude + 0.001f;

                accelerations[i] += constanteGravitationnelle * masses[j] /
                                    (distance * distance) *
                                    direction.normalized;
            }
        }

        int targetIndex = bodies.IndexOf(mainBody);
        List<Vector3> orbitPoints = new List<Vector3>();

        for (int step = 0; step < simulationSteps; step++)
        {
            //position
            for (int i = 0; i < count; i++)
            {
                positions[i] += vitesses[i] * timeStep + 0.5f * accelerations[i] * timeStep * timeStep;
            }

            //accel
            for (int i = 0; i < count; i++)
            {
                newAccelerations[i] = Vector3.zero;

                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;

                    Vector3 direction = positions[j] - positions[i];
                    float distance = direction.magnitude + 0.001f;

                    newAccelerations[i] += constanteGravitationnelle * masses[j] /
                                           (distance * distance) *
                                           direction.normalized;
                }
            }

            //vitesse
            for (int i = 0; i < count; i++)
            {
                vitesses[i] += 0.5f * (accelerations[i] + newAccelerations[i]) * timeStep;

                accelerations[i] = newAccelerations[i];
            }


            orbitPoints.Add(positions[targetIndex]);
        }


        mainBody.line.useWorldSpace = true;
        mainBody.line.positionCount = orbitPoints.Count;
        mainBody.line.SetPositions(orbitPoints.ToArray());
    }

}