using System.Collections.Generic;
using UnityEngine;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Constante gravitationnelle ajustée

    public float gravityMultiplier = 1e13f; 
    public float softening = 0.1f; // Prévient les explosions physiques
    public float Timestep = 3600f;

    private static readonly List<GravityBody> bodies = new List<GravityBody>();
    public static IReadOnlyList<GravityBody> Bodies => bodies;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (var body in bodies)
        {
            if (body != null && body.line != null)
            {
                PredictOrbitHybrid(body);
            }
        }
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
        if (a == null || b == null || a.rb == null || b.rb == null) return;

        Vector3 direction = b.rb.position - a.rb.position;
        float distanceSqr = direction.sqrMagnitude + softening;

        float forceMagnitude = gravityMultiplier * G * (a.rb.mass * b.rb.mass) / distanceSqr;
        Vector3 force = direction.normalized * forceMagnitude;

        a.rb.AddForce(force);
        b.rb.AddForce(-force);
    }

    public static Vector3 GetCenterOfMass()
    {
        if (bodies.Count == 0) return Vector3.zero;

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

        if (totalMass == 0f) return Vector3.zero;
        return weightedSum / totalMass;
    }

    // ==========================================
    // SYSTÈME DE PRÉDICTION D'ORBITE
    // ==========================================

    void PredictOrbitHybrid(GravityBody body)
    {
        if (IsTwoBodyDominated(body, out GravityBody mainAttractor))
        {
            float period = CalculateOrbitalPeriod(body, mainAttractor);
            
            // SÉCURITÉ ANTI-CRASH : On limite le nombre d'étapes de dessin à 300 maximum !
            int steps = Mathf.CeilToInt(period / 1f);
            steps = Mathf.Clamp(steps, 60, 300); 

            DrawOrbitHybrid(body, mainAttractor, period, steps);
        }
        else
        {
            // SÉCURITÉ ANTI-CRASH : 150 points max pour l'algorithme à N-Corps
            OrbitPredictor(body, 50f, 150);
        }
    }

    bool IsTwoBodyDominated(GravityBody body, out GravityBody mainAttractor)
    {
        mainAttractor = null;
        float maxForce = 0f;
        float secondMaxForce = 0f;

        foreach (var other in bodies)
        {
            if (other == body || other.rb == null) continue;

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

        return maxForce > secondMaxForce * 5f;
    }

    float CalculateOrbitalPeriod(GravityBody body, GravityBody centralBody)
    {
        float distance = Vector3.Distance(body.rb.position, centralBody.rb.position);
        float mu = G * gravityMultiplier * (body.rb.mass + centralBody.rb.mass);
        return 2f * Mathf.PI * Mathf.Sqrt(distance * distance * distance / mu);
    }

    void DrawOrbitHybrid(GravityBody body, GravityBody centralBody, float period, int steps)
    {
        float dt = period / steps;
        Vector3 position = body.rb.position;
        Vector3 velocity = body.rb.linearVelocity;
        float gravConst = G * gravityMultiplier;

        List<Vector3> points = new List<Vector3> { position };

        for (int i = 0; i < steps; i++)
        {
            Vector3 accel = Vector3.zero;
            float tempsEcoule = i * dt;

            foreach (var other in bodies)
            {
                if (other == body || other.rb == null) continue;
                
                Vector3 positionFutureDeLautre = other.rb.position + (other.rb.linearVelocity * tempsEcoule);
                
                Vector3 dir = positionFutureDeLautre - position;
                float dist = dir.magnitude + softening;
                accel += gravConst * other.rb.mass / (dist * dist) * dir.normalized;
            }

            position += velocity * dt + 0.5f * accel * dt * dt;

            Vector3 newAccel = Vector3.zero;
            foreach (var other in bodies)
            {
                if (other == body || other.rb == null) continue;
                
                Vector3 positionFutureDeLautre = other.rb.position + (other.rb.linearVelocity * (tempsEcoule + dt));
                Vector3 dir = positionFutureDeLautre - position;
                float dist = dir.magnitude + softening;
                newAccel += gravConst * other.rb.mass / (dist * dist) * dir.normalized;
            }

            velocity += 0.5f * (accel + newAccel) * dt;
            points.Add(position);
        }

        if (body.line != null)
        {
            body.line.useWorldSpace = true;
            body.line.positionCount = points.Count;
            body.line.SetPositions(points.ToArray());
        }
    }

    void OrbitPredictor(GravityBody mainBody, float predictionTime, int steps)
    {
        if (mainBody == null || mainBody.rb == null || mainBody.line == null) return;

        float constanteGravitationnelle = gravityMultiplier * G;
        int count = bodies.Count;
        float timeStep = predictionTime / steps;

        Vector3[] positions = new Vector3[count];
        Vector3[] vitesses = new Vector3[count];
        float[] masses = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (bodies[i] == null || bodies[i].rb == null) return;
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
                float distance = direction.magnitude + softening;
                accelerations[i] += constanteGravitationnelle * masses[j] / (distance * distance) * direction.normalized;
            }
        }

        int targetIndex = bodies.IndexOf(mainBody);
        List<Vector3> orbitPoints = new List<Vector3>();

        for (int step = 0; step < steps; step++)
        {
            for (int i = 0; i < count; i++)
            {
                positions[i] += vitesses[i] * timeStep + 0.5f * accelerations[i] * timeStep * timeStep;
            }

            for (int i = 0; i < count; i++)
            {
                newAccelerations[i] = Vector3.zero;
                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;
                    Vector3 direction = positions[j] - positions[i];
                    float distance = direction.magnitude + softening;
                    newAccelerations[i] += constanteGravitationnelle * masses[j] / (distance * distance) * direction.normalized;
                }
            }

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

    // ==========================================
    // STRUCTURES ORBITALES (Pour utilisation future)
    // ==========================================
    public struct OrbitalElements
    {
        public float semiMajorAxis;
        public float eccentricity;
        public float periapsis;
        public float apoapsis;
        public float period;
    }

    public OrbitalElements CalculateOrbitalElements(GravityBody body, GravityBody centralBody)
    {
        float Gscaled = gravityMultiplier * G;
        Vector3 r = body.rb.position - centralBody.rb.position;
        Vector3 v = body.rb.linearVelocity - centralBody.rb.linearVelocity;
        float mu = Gscaled * centralBody.rb.mass; 

        float rMag = r.magnitude;
        float vMag = v.magnitude;
        Vector3 h = Vector3.Cross(r, v);
        Vector3 eVec = (Vector3.Cross(v, h) / mu) - (r / rMag);
        float e = eVec.magnitude;
        float energy = (vMag * vMag) / 2f - mu / rMag;

        OrbitalElements elements = new OrbitalElements();
        elements.semiMajorAxis = -mu / (2f * energy);
        elements.periapsis = elements.semiMajorAxis * (1f - e);
        elements.apoapsis = elements.semiMajorAxis * (1f + e);
        elements.eccentricity = e;

        if (e < 1f) elements.period = 2f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(elements.semiMajorAxis, 3) / mu);
        else elements.period = -1f; 

        return elements;
    }
}