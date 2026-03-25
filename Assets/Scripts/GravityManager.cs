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
    private static readonly List<float> periodes = new List<float>();
    private GravityBody soleil = null;

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
        int count = bodies.Count;

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                ApplyGravity(bodies[i], bodies[j]);
            }

            PredictOrbitHybrid(bodies[i]);
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

    //structure d'éléments orbitaux pour stocker les paramètres d'une orbite calculés à partir de la position et de la vitesse
    public struct OrbitalElements
    {
        public float semiMajorAxis;
        public float eccentricity;
        public float periapsis;
        public float apoapsis;
        public float period;
    }


    //calcul des éléments orbitaux à partir de la position et de la vitesse d'un corps par rapport à un corps central
    OrbitalElements CalculateOrbitalElements(GravityBody body, GravityBody centralBody)
    {
        float Gscaled = gravityMultiplier * G;

        Vector3 r = body.rb.position - centralBody.rb.position;
        Vector3 v = body.rb.linearVelocity - centralBody.rb.linearVelocity;

        float mu = Gscaled * centralBody.rb.mass; // standard gravitational parameter

        float rMag = r.magnitude;
        float vMag = v.magnitude;

        // Specific angular momentum
        Vector3 h = Vector3.Cross(r, v);

        // Eccentricity vector
        Vector3 eVec = (Vector3.Cross(v, h) / mu) - (r / rMag);
        float e = eVec.magnitude;

        // Specific orbital energy
        float energy = (vMag * vMag) / 2f - mu / rMag;

        OrbitalElements elements = new OrbitalElements();

        // Semi-major axis
        elements.semiMajorAxis = -mu / (2f * energy);

        // Periapsis / Apoapsis
        elements.periapsis = elements.semiMajorAxis * (1f - e);
        elements.apoapsis = elements.semiMajorAxis * (1f + e);

        elements.eccentricity = e;

        // Period (only if bound orbit)
        if (e < 1f)
        {
            elements.period = 2f * Mathf.PI * Mathf.Sqrt(
                Mathf.Pow(elements.semiMajorAxis, 3) / mu
            );
        }
        else
        {
            elements.period = -1f; // escape trajectory
        }

        return elements;
    }

    // Hybrid approach: if one body dominates the gravity, use orbital elements for a clean ellipse. Otherwise, do a short-term n-body prediction.
    void PredictOrbitHybrid(GravityBody body)
{

    if (IsTwoBodyDominated(body, out soleil))
    {
        DrawOrbitFromElements(body, soleil);
    }
    else
    {
        float predictionTime = 10f;
        int steps = 150;

        OrbitPredictor(body, predictionTime, steps);
    }
}

    // Check if one body’s gravitational influence is much stronger than all others → treat as 2-body
    bool IsTwoBodyDominated(GravityBody body, out GravityBody mainAttractor)
    {
        mainAttractor = null;

        float maxForce = 0f;
        float secondMaxForce = 0f;

        foreach (var other in bodies)
        {
            if (other == body) continue;

            float distance = Vector3.Distance(body.rb.position, other.rb.position);
            float force = other.rb.mass / (distance * distance);

            if (force > maxForce)
            {
                secondMaxForce = maxForce;
                maxForce = force;
                mainAttractor = other;
            }
            else if (force > secondMaxForce)
            {
                secondMaxForce = force;
            }
        }

        // If one body dominates enough → treat as 2-body
        return maxForce > secondMaxForce * 5f;//5f is tuneable
    }

    // Draw a clean ellipse based on orbital elements, instead of a noisy n-body prediction
    void DrawOrbitFromElements(GravityBody body, GravityBody centralBody)
    {
        OrbitalElements el = CalculateOrbitalElements(body, centralBody);

        if (el.eccentricity >= 1f)
            return; // escape trajectory → don’t draw ellipse

        int segments = 100;
        List<Vector3> points = new List<Vector3>();

        Vector3 center = centralBody.rb.position;

        // Basis vectors
        Vector3 r = (body.rb.position - center).normalized;
        Vector3 v = body.rb.linearVelocity.normalized;
        Vector3 normal = Vector3.Cross(r, v).normalized;
        Vector3 tangent = Vector3.Cross(normal, r).normalized;

        float a = el.semiMajorAxis;
        float e = el.eccentricity;
        float b = a * Mathf.Sqrt(1 - e * e);

        for (int i = 0; i < segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.PI * 2f;

            float x = a * Mathf.Cos(theta);
            float y = b * Mathf.Sin(theta);

            Vector3 point =
                center +
                r * (x - a * e) +
                tangent * y;

            points.Add(point);
        }

        body.line.positionCount = points.Count;
        body.line.SetPositions(points.ToArray());
    }

    // Full n-body prediction for a short time to capture complex interactions when no single body dominates
    void OrbitPredictor(GravityBody mainBody, float predictionTime, int steps)
    {
        float constanteGravitationnelle = gravityMultiplier * G;

        int count = bodies.Count;
        float timeStep = predictionTime / steps;

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

        // Initial acceleration
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

        for (int step = 0; step < steps; step++)
        {
            // Position
            for (int i = 0; i < count; i++)
            {
                positions[i] += vitesses[i] * timeStep + 0.5f * accelerations[i] * timeStep * timeStep;
            }

            // Acceleration
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

            // Velocity
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