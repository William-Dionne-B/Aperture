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
    public static IReadOnlyList<GravityBody> Bodies => bodies;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Change simulation speed with number keys 1, 2, 3, etc.
        //if (Input.GetKeyDown(KeyCode.Alpha1)) SetSimulationSpeed(1f);
        //if (Input.GetKeyDown(KeyCode.Alpha2)) SetSimulationSpeed(3f);
        //if (Input.GetKeyDown(KeyCode.Alpha3)) SetSimulationSpeed(6f);     L'ACCELERATION DE LA MORT!!!
        //if (Input.GetKeyDown(KeyCode.Alpha4)) SetSimulationSpeed(8f);
        //if (Input.GetKeyDown(KeyCode.Alpha5)) SetSimulationSpeed(10f);
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
        CleanupInvalidBodies();

        int count = bodies.Count;

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                ApplyGravity(bodies[i], bodies[j]);
            }

            if (bodies[i] != null)
            {
                OrbitPredictor(bodies[i]);
            }
            //OrbitPredictorIndividual();
        }

    }

    void CleanupInvalidBodies()
    {
        for (int index = bodies.Count - 1; index >= 0; index--)
        {
            GravityBody body = bodies[index];
            if (body == null || body.rb == null)
            {
                bodies.RemoveAt(index);
            }
        }
    }

    void ApplyGravity(GravityBody a, GravityBody b)
    {
        if (a == null || b == null || a.rb == null || b.rb == null)
        {
            return;
        }

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
        if (mainBody == null || mainBody.rb == null || mainBody.line == null)
        {
            return;
        }

        float constanteGravitationnelle = gravityMultiplier * G;
        int simulationSteps = 150;
        float timeStep = 0.1f;

        int count = bodies.Count;

        Vector3[] positions = new Vector3[count];
        Vector3[] vitesses = new Vector3[count];
        float[] masses = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (bodies[i] == null || bodies[i].rb == null)
            {
                return;
            }

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

    void OrbitPredictorIndividual()
    {
        float Gconst = gravityMultiplier * G;
        float timeStep = 0.95f;
        int maxSteps = 480;

        int count = bodies.Count;

        Vector3[] positions = new Vector3[count];
        Vector3[] velocities = new Vector3[count];
        float[] masses = new float[count];

        for (int i = 0; i < count; i++)
        {
            positions[i] = bodies[i].rb.position;
            velocities[i] = bodies[i].rb.linearVelocity;
            masses[i] = bodies[i].rb.mass;
        }

        Vector3[] accelerations = ComputeAccelerations(positions, masses, Gconst);

        Vector3 barycenter = ComputeBarycenter(positions, masses);

        Vector3[] startDirections = new Vector3[count];
        float[] accumulatedAngles = new float[count];
        float[] previousAngles = new float[count];
        bool[] completed = new bool[count];

        List<Vector3>[] orbitPoints = new List<Vector3>[count];

        for (int i = 0; i < count; i++)
        {
            startDirections[i] = (positions[i] - barycenter).normalized;
            accumulatedAngles[i] = 0f;
            previousAngles[i] = 0f;
            completed[i] = false;
            orbitPoints[i] = new List<Vector3>();
        }

        int remaining = count;

        for (int step = 0; step < maxSteps && remaining > 0; step++)
        {
            // Position update (Velocity Verlet)
            for (int i = 0; i < count; i++)
                positions[i] += velocities[i] * timeStep + 0.5f * accelerations[i] * timeStep * timeStep;

            Vector3[] newAccelerations = ComputeAccelerations(positions, masses, Gconst);

            for (int i = 0; i < count; i++)
            {
                velocities[i] += 0.5f * (accelerations[i] + newAccelerations[i]) * timeStep;
                accelerations[i] = newAccelerations[i];
            }

            barycenter = ComputeBarycenter(positions, masses);

            for (int i = 0; i < count; i++)
            {
                if (completed[i]) continue;

                orbitPoints[i].Add(positions[i]);

                Vector3 currentDir = (positions[i] - barycenter).normalized;

                Vector3 orbitNormal = Vector3.Cross(
                    positions[i] - barycenter,
                    velocities[i]
                ).normalized;

                float angle = Vector3.SignedAngle(startDirections[i], currentDir, orbitNormal);
                float deltaAngle = Mathf.DeltaAngle(previousAngles[i], angle);

                accumulatedAngles[i] += deltaAngle;
                previousAngles[i] = angle;

                if (Mathf.Abs(accumulatedAngles[i]) >= 360f)
                {
                    completed[i] = true;
                    remaining--;
                    continue;
                }

                float distance = Vector3.Distance(positions[i], barycenter);
                if (distance > 10000f)
                {
                    completed[i] = true;
                    remaining--;
                }
            }
        }

        // Draw all orbits
        for (int i = 0; i < count; i++)
        {
            bodies[i].line.useWorldSpace = true;
            bodies[i].line.positionCount = orbitPoints[i].Count;
            bodies[i].line.SetPositions(orbitPoints[i].ToArray());
        }
    }

    Vector3 ComputeBarycenter(Vector3[] positions, float[] masses)
    {
        Vector3 center = Vector3.zero;
        float totalMass = 0f;

        for (int i = 0; i < positions.Length; i++)
        {
            center += positions[i] * masses[i];
            totalMass += masses[i];
        }

        return center / totalMass;
    }

    Vector3[] ComputeAccelerations(Vector3[] positions, float[] masses, float G)
    {
        int count = positions.Length;
        Vector3[] accelerations = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                Vector3 direction = positions[j] - positions[i];
                float distance = direction.magnitude + 0.001f;

                Vector3 forceDir = direction.normalized;
                float force = G * masses[i] * masses[j] / (distance * distance);

                Vector3 accelI = force / masses[i] * forceDir;
                Vector3 accelJ = force / masses[j] * -forceDir;

                accelerations[i] += accelI;
                accelerations[j] += accelJ;
            }
        }

        return accelerations;
    }
}