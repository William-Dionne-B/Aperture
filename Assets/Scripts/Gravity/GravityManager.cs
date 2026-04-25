using System;
using System.Collections.Generic;
using UnityEngine;

public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Constante gravitationnelle ajustée

    public float gravityMultiplier = 1e13f;
    public float softening = 0.1f; // Prévient les explosions physiques
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
    private float predictionTimer = 0f;

    private static readonly List<GravityBody> bodies = new List<GravityBody>();
    public static IReadOnlyList<GravityBody> Bodies => bodies;
    private static readonly List<float> periodes = new List<float>();
    private GravityBody soleil = null;
    private float orbitPredictionTimer = 0f;

    [Header("GPU Settings")]
    public bool useGpu = true;
    public ComputeShader gravityComputeShader;
    public int threadGroupSize = 256;

    // Compute buffers
    private ComputeBuffer inputBuffer;
    private ComputeBuffer outputBuffer;
    private bool buffersInitialized = false;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        // lazy init buffers on first FixedUpdate when needed
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void ReleaseBuffers()
    {
        if (inputBuffer != null)
        {
            inputBuffer.Release();
            inputBuffer = null;
        }
        if (outputBuffer != null)
        {
            outputBuffer.Release();
            outputBuffer = null;
        }
        buffersInitialized = false;
    }

    void Update()
    {
        predictionTimer += Time.unscaledDeltaTime;

        if (predictionTimer >= 0.06f)
        {
            predictionTimer = 0f;

            foreach (var body in bodies)
            {
                if (body != null && body.line != null)
                {
                    PredictOrbitHybrid(body);
                }
            }
        }
    }

    public void SetSimulationSpeed(float speed)
    {
        Time.timeScale = speed;

        if (speed > 0f)
        {
            float physicsResolution = Mathf.Clamp(speed / 3f, 1f, 4f);
            Time.fixedDeltaTime = 0.02f * physicsResolution;
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

        if (useGpu && gravityComputeShader != null)
        {
            RunGpuStep();
            return;
        }

        // Fallback CPU behaviour (original)
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

    // GPU data struct (must match HLSL layout)
    struct GpuBody
    {
        public Vector3 pos;
        public Vector3 vel;
        public float mass;
        public float pad; // pour alignement à 32 bytes
    }

    void EnsureBuffersInitialized()
    {
        int count = bodies.Count;
        if (count == 0) return;

        if (buffersInitialized && inputBuffer != null && inputBuffer.count == count) return;

        ReleaseBuffers();

        int stride = sizeof(float) * 8; // pos.xyz, vel.xyz, mass, pad = 8 floats
        inputBuffer = new ComputeBuffer(Math.Max(1, count), stride);
        outputBuffer = new ComputeBuffer(Math.Max(1, count), stride);

        buffersInitialized = true;
    }

    void RunGpuStep()
    {
        int count = bodies.Count;
        if (count == 0) return;
        if (gravityComputeShader == null) return;

        EnsureBuffersInitialized();
        if (!buffersInitialized) return;

        // prepare data
        GpuBody[] arr = new GpuBody[count];
        for (int i = 0; i < count; i++)
        {
            var b = bodies[i];
            if (b == null || b.rb == null)
            {
                arr[i].pos = Vector3.zero;
                arr[i].vel = Vector3.zero;
                arr[i].mass = 0f;
                arr[i].pad = 0f;
            }
            else
            {
                arr[i].pos = b.rb.position;
                // Some projects use rb.linearVelocity; keep compatibility by writing both fields below when applying back
                arr[i].vel = b.rb.velocity;
                arr[i].mass = b.rb.mass;
                arr[i].pad = 0f;
            }
        }

        inputBuffer.SetData(arr);

        int kernel = gravityComputeShader.FindKernel("NBody");
        gravityComputeShader.SetBuffer(kernel, "_InputBodies", inputBuffer);
        gravityComputeShader.SetBuffer(kernel, "_OutputBodies", outputBuffer);

        float gravConst = gravityMultiplier * G;
        gravityComputeShader.SetFloat("_DeltaTime", Timestep);
        gravityComputeShader.SetFloat("_Softening", softening);
        gravityComputeShader.SetFloat("_GravityConst", gravConst);
        gravityComputeShader.SetInt("_Count", count);

        int groups = Mathf.CeilToInt((float)count / threadGroupSize);
        gravityComputeShader.Dispatch(kernel, groups, 1, 1);

        // read back
        GpuBody[] results = new GpuBody[count];
        outputBuffer.GetData(results);

        // apply positions and velocities on CPU side (main thread)
        for (int i = 0; i < count; i++)
        {
            var b = bodies[i];
            if (b == null || b.rb == null) continue;

            Vector3 newPos = results[i].pos;
            Vector3 newVel = results[i].vel;

            // Apply to Rigidbody: prefer direct assignment to velocity and linearVelocity if present
            try
            {
                // Some projects use non-standard 'linearVelocity'. Keep setting both when available.
                b.rb.velocity = newVel;
            }
            catch (Exception) { /* ignore if not present */ }

            // Attempt to set 'linearVelocity' if the property exists (kept compatible with existing code using it)
            var rbType = b.rb.GetType();
            var prop = rbType.GetProperty("linearVelocity");
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(b.rb, newVel, null); } catch { }
            }

            // set position directly — note: setting position directly may interfere with physics; if undesirable use MovePosition
            try
            {
                b.rb.position = newPos;
            }
            catch (Exception)
            {
                try { b.rb.MovePosition(newPos); } catch { }
            }
        }
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

    // Hybrid approach: if one body dominates the gravity, use orbital elements for a clean ellipse. Otherwise, do a short-term n-body prediction.
    void PredictOrbitHybrid(GravityBody body)
    {
        if (body == null || body.rb == null || body.line == null)
        {
            return;
        }

        if (IsTwoBodyDominated(body, out GravityBody mainAttractor))
        {
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
        float semiMajorAxis = 1 / ((2 / distance) - (Mathf.Pow(body.rb.linearVelocity.magnitude, 2)) / mu);
        return 2f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(semiMajorAxis, 3) / mu);
    }

    void DrawOrbitHybrid(GravityBody body, GravityBody centralBody, float period, int steps)
    {
        float dt = period / steps;
        Vector3 startPosition = body.rb.position;
        Vector3 position = startPosition;
        Vector3 velocity = body.rb.linearVelocity;
        float gravConst = G * gravityMultiplier;
        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance);

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

            if ((position - startPosition).sqrMagnitude > maxDistance * maxDistance)
            {
                break;
            }

            points.Add(position);
        }


        ApplyOrbitLineIfStable(body.line, points, startPosition);
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
        List<Vector3> orbitPoints = new List<Vector3> { mainBody.rb.position };
        Vector3 startPosition = mainBody.rb.position;
        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance);

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

            Vector3 predictedPosition = positions[targetIndex];
            if ((predictedPosition - startPosition).sqrMagnitude > maxDistance * maxDistance)
            {
                break;
            }

            orbitPoints.Add(predictedPosition);
        }

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