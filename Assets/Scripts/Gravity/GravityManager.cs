using System.Collections.Generic;
﻿using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Scaled gravity constant

    public float gravityMultiplier = 1e13f; // Tune this for fun & stability
    public float softening = 0.1f; // Prevents singularities / explosions
    public float Timestep = 3600f;
    public bool enableOrbitPrediction = true;
    public float orbitPredictionInterval = 0.25f;
    public int maxOrbitPredictionSteps = 256;
    public float maxOrbitPredictionDuration = 120f;
    public float orbitSampleInterval = 0.5f;
    public float maxOrbitPredictionDistance = 500f;
    public int minOrbitPointsToRender = 12;
    public float maxOrbitSegmentLength = 60f;
    public float maxOrbitSegmentToAverageRatio = 4f;
    public float maxOrbitSharpTurnDegrees = 120f;
    public float maxOrbitSharpTurnRatio = 0.35f;

    private static readonly List<GravityBody> bodies = new List<GravityBody>();
    public static IReadOnlyList<GravityBody> Bodies => bodies;
    private static readonly List<float> periodes = new List<float>();
    private GravityBody soleil = null;
    private float orbitPredictionTimer = 0f;

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
        bool shouldPredictOrbit = false;

        if (enableOrbitPrediction)
        {
            float interval = Mathf.Max(0.01f, orbitPredictionInterval);
            orbitPredictionTimer += Time.fixedDeltaTime;
            shouldPredictOrbit = orbitPredictionTimer >= interval;
            if (shouldPredictOrbit)
            {
                orbitPredictionTimer = 0f;
            }
        }

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                ApplyGravity(bodies[i], bodies[j]);
            }

            if (shouldPredictOrbit)
            {
                PredictOrbitHybrid(bodies[i]);
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
        if (body == null || body.rb == null || body.line == null)
        {
            return;
        }

        if (IsTwoBodyDominated(body, out GravityBody mainAttractor))
        {
            // Calculate orbital period
            float period = CalculateOrbitalPeriod(body, mainAttractor);
            float duration = Mathf.Min(period, Mathf.Max(0.1f, maxOrbitPredictionDuration));

            // Calculate steps based on period
            float sample = Mathf.Max(0.01f, orbitSampleInterval);
            int steps = Mathf.Clamp(Mathf.CeilToInt(duration / sample), 8, Mathf.Max(8, maxOrbitPredictionSteps));

            // Dominated by one body → draw clean ellipse
            DrawOrbitHybrid(body, mainAttractor, duration, steps);
        }
        else
        {
            // No single dominant body → full short-term n-body integration
            float duration = Mathf.Min(50f, Mathf.Max(0.1f, maxOrbitPredictionDuration));
            float sample = Mathf.Max(0.01f, orbitSampleInterval);
            int steps = Mathf.Clamp(Mathf.CeilToInt(duration / sample), 8, Mathf.Max(8, maxOrbitPredictionSteps));
            OrbitPredictor(body, duration, steps);
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

    // Compute orbital period using Kepler's 3rd law approximation
    float CalculateOrbitalPeriod(GravityBody body, GravityBody centralBody)
    {
        float distance = Vector3.Distance(body.rb.position, centralBody.rb.position);
        float mu = G * gravityMultiplier * (body.rb.mass + centralBody.rb.mass);
        return 2f * Mathf.PI * Mathf.Sqrt(distance * distance * distance / mu);
    }

    // Draw a clean ellipse based on orbital elements, instead of a noisy n-body prediction
    void DrawOrbitHybrid(GravityBody body, GravityBody centralBody, float period, int steps)
    {
        float dt = period / steps;
        Vector3 startPosition = body.rb.position;
        Vector3 position = startPosition;
        Vector3 velocity = body.rb.linearVelocity;
        float gravConst = G * gravityMultiplier;
        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance);

        List<Vector3> points = new List<Vector3> {position};

        for (int i = 0; i < steps; i++)
        {
            // Compute acceleration from all bodies
            Vector3 accel = Vector3.zero;
            foreach (var other in bodies)
            {
                if (other == body) continue;
                Vector3 dir = other.rb.position - position;
                float dist = dir.magnitude + 0.001f;
                accel += gravConst * other.rb.mass / (dist * dist) * dir.normalized;
            }

            // Integrate using simple Verlet step
            position += velocity * dt + 0.5f * accel * dt * dt;

            // Compute new acceleration for velocity update
            Vector3 newAccel = Vector3.zero;
            foreach (var other in bodies)
            {
                if (other == body) continue;
                Vector3 dir = other.rb.position - position;
                float dist = dir.magnitude + 0.001f;
                newAccel += gravConst * other.rb.mass / (dist * dist) * dir.normalized;
            }

            velocity += 0.5f * (accel + newAccel) * dt;

            if ((position - startPosition).sqrMagnitude > maxDistance * maxDistance)
            {
                break;
            }

            points.Add(position);
        }
        // Ensure the last point matches the planet's current position
        //points[points.Count - 1] = body.rb.position;


        ApplyOrbitLineIfStable(body.line, points, startPosition);
    }

    // Full n-body prediction for a short time to capture complex interactions when no single body dominates
    void OrbitPredictor(GravityBody mainBody, float predictionTime, int steps)
    {
        if (mainBody == null || mainBody.rb == null || mainBody.line == null)
        {
            return;
        }

        float constanteGravitationnelle = gravityMultiplier * G;

        int count = bodies.Count;
        float timeStep = predictionTime / steps;

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
        List<Vector3> orbitPoints = new List<Vector3> { mainBody.rb.position };
        Vector3 startPosition = mainBody.rb.position;
        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance);

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

            Vector3 predictedPosition = positions[targetIndex];
            if ((predictedPosition - startPosition).sqrMagnitude > maxDistance * maxDistance)
            {
                break;
            }

            orbitPoints.Add(predictedPosition);
        }
        // Ensure last point is the planet's real-time position
        //orbitPoints[orbitPoints.Count - 1] = mainBody.rb.position;

        ApplyOrbitLineIfStable(mainBody.line, orbitPoints, startPosition);
    }

    void ApplyOrbitLineIfStable(LineRenderer line, List<Vector3> points, Vector3 origin)
    {
        if (line == null)
        {
            return;
        }

        if (!IsOrbitPredictionStable(points, origin))
        {
            line.positionCount = 0;
            return;
        }

        line.useWorldSpace = true;
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }

    bool IsOrbitPredictionStable(List<Vector3> points, Vector3 origin)
    {
        if (points == null)
        {
            return false;
        }

        int minimumPoints = Mathf.Max(2, minOrbitPointsToRender);
        if (points.Count < minimumPoints)
        {
            return false;
        }

        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance);
        float maxDistanceSqr = maxDistance * maxDistance;
        float hardMaxSegment = Mathf.Max(0.01f, maxOrbitSegmentLength);
        float segmentToAverageRatioLimit = Mathf.Max(1f, maxOrbitSegmentToAverageRatio);
        float sharpTurnAngle = Mathf.Clamp(maxOrbitSharpTurnDegrees, 1f, 179f);
        float sharpTurnRatioLimit = Mathf.Clamp01(maxOrbitSharpTurnRatio);

        float totalSegmentLength = 0f;
        float maxSegmentLengthObserved = 0f;
        int segmentCount = 0;
        int sharpTurnCount = 0;
        int turnCount = 0;
        bool hasPreviousDirection = false;
        Vector3 previousDirection = Vector3.zero;

        for (int i = 1; i < points.Count; i++)
        {
            if ((points[i] - origin).sqrMagnitude > maxDistanceSqr)
            {
                return false;
            }

            Vector3 segment = points[i] - points[i - 1];
            float segmentLength = segment.magnitude;
            if (segmentLength <= 0.0001f)
            {
                continue;
            }

            if (segmentLength > hardMaxSegment)
            {
                return false;
            }

            Vector3 direction = segment / segmentLength;
            if (hasPreviousDirection)
            {
                float angle = Vector3.Angle(previousDirection, direction);
                turnCount++;
                if (angle > sharpTurnAngle)
                {
                    sharpTurnCount++;
                }
            }

            previousDirection = direction;
            hasPreviousDirection = true;

            totalSegmentLength += segmentLength;
            maxSegmentLengthObserved = Mathf.Max(maxSegmentLengthObserved, segmentLength);
            segmentCount++;
        }

        if (segmentCount < minimumPoints - 1)
        {
            return false;
        }

        float averageSegmentLength = totalSegmentLength / segmentCount;
        if (averageSegmentLength <= 0.0001f)
        {
            return false;
        }

        if (maxSegmentLengthObserved > averageSegmentLength * segmentToAverageRatioLimit)
        {
            return false;
        }

        if (turnCount > 0)
        {
            float sharpTurnRatio = (float)sharpTurnCount / turnCount;
            if (sharpTurnRatio > sharpTurnRatioLimit)
            {
                return false;
            }
        }

        return true;
    }
}