using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrôle-la gravité des astres dans la simulation et les calculs de prediction
/// d'orbites des astres qui orbitent des astres.
/// </summary>
public class GravityManager : MonoBehaviour
{
    public static GravityManager Instance;

    public static float G = 6.674e-8f; // Constante gravitationnelle ajustée

    public float gravityMultiplier = 1e13f; 
    public float softening = 0.1f; // Prévient les explosions physiques
    public float Timestep = 3600f;
    public bool enableOrbitPrediction = true;
    public float orbitPredictionInterval = 0.25f;
    public int maxOrbitPredictionSteps = 2000;
    public float maxOrbitPredictionDuration = 100000f;
    public float orbitSampleInterval = 0.5f;
    public float maxOrbitPredictionDistance = 1e7f;
    public int minOrbitPointsToRender = 12;
    public float maxOrbitSegmentLength = 500f;
    public float maxOrbitSegmentToAverageRatio = 10f;
    public float maxOrbitSharpTurnDegrees = 120f;
    public float maxOrbitSharpTurnRatio = 0.8f;
    private float predictionTimer = 0f;

    public UnityEngine.UI.Toggle OrbitesCheck;
    public KeyCode toggleOrbitesKey = KeyCode.O;
    public bool orbitesOn = true;

    public float semiMajorAxis;
    public float mu;

    private static readonly List<GravityBody> bodies = new List<GravityBody>();
    public static IReadOnlyList<GravityBody> Bodies => bodies;
    private static readonly List<float> periodes = new List<float>();
    private float orbitPredictionTimer = 0f;

    private Camera mainCam;

    // ==========================================
    // MÉTHODES UNITY
    // ==========================================

    void Start()
    {
        mainCam = Camera.main;
        if (OrbitesCheck != null)
            OrbitesCheck.onValueChanged.AddListener(OnToggleChanged);
    }

    void Awake()
    {
        Instance = this;
        //bodies.Clear();
        //periodes.Clear();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    void Update()
    {
        predictionTimer += Time.unscaledDeltaTime;

        if (OrbitesCheck != null)
            OrbitesCheck.isOn = orbitesOn;

        if (predictionTimer >= 0.06f && orbitesOn) 
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

        if (Input.GetKeyDown(toggleOrbitesKey))
        {
            orbitesOn = !orbitesOn;

            if (OrbitesCheck != null)
            {
                OrbitesCheck.isOn = orbitesOn;
            }

            if (!orbitesOn)
            {
                ClearAllOrbitLines();
            }
        }
    }
    
    // ==========================================
    // ADMINISTRATION DU SYSTEME
    // ==========================================
    
    /// <summary>
    /// Permet aux scripts GravityBody de s'ajouter.
    /// </summary>
    public static void Register(GravityBody body)
    {
        if (!bodies.Contains(body))
            bodies.Add(body);
    }

    /// <summary>
    /// Permet aux scripts GravityBody de s'enlever
    /// </summary>
    public static void Unregister(GravityBody body)
    {
        bodies.Remove(body);
    }
    
    /// <summary>
    /// Calcule le point d'equilibre gravitationnel de tout le systeme solaire.
    /// </summary>
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
    // GESTION DE L'ATTRACTION PHYSIQUE
    // ==========================================
    
    /// <summary>
    /// Permet de calculer les mouvements a chaque pas de temps physique des objets de la simulation et d'appliquer les mouvements.
    /// </summary>
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

            if (shouldPredictOrbit && orbitesOn)
            {
                PredictOrbitHybrid(bodies[i]);
            }
        }
    }
    
    /// <summary>
    /// Calcule la force d'attraction entre deux objets en utilisant la formule F = G*(m1*m2)/(d^2),
    /// ajoute un parametre de softening pour eviter que les forces deviennent infinies.
    /// </summary>
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
    
    /// <summary>
    /// Modifie la vitesse de l'ecoulement du temps, ajuste aussi la precision physique
    /// pour eviter les saccades quand on accelere le temps.
    /// </summary>
    public void SetSimulationSpeed(float speed)
    {
        Time.timeScale = speed;
        
        if (speed > 0f)
        {
            float physicsResolution = Mathf.Clamp(speed / 3f, 1f, 4f);
            Time.fixedDeltaTime = 0.02f * physicsResolution;
        }
    }
    
    // ==========================================
    // GESTION DE L'ATTRACTION PHYSIQUE
    // ==========================================
    
    /// <summary>
    /// Determine quelle methode de dessin utiliser, si une planete est domincee par une seule etoile,
    /// dessine une ellipse propre. Si plusieurs forces s'affrontent, elle simule une trajectoire complexe.
    /// </summary>
    void PredictOrbitHybrid(GravityBody body)
    {
        if (body == null || body.rb == null || body.line == null)
        {
            return;
        }

        if (IsTwoBodyDominated(body, out GravityBody mainAttractor))
        {
            float period = CalculateOrbitalPeriod(body, mainAttractor);
            float duration = period * 1.05f;
            
            float sample = Mathf.Max(0.01f, orbitSampleInterval);
            int steps = Mathf.Clamp(Mathf.CeilToInt(duration / sample), 8, Mathf.Max(8, maxOrbitPredictionSteps));
            
            DrawOrbitHybrid(body, mainAttractor, duration, steps);
        }
        else
        {
            float duration = Mathf.Min(50f, Mathf.Max(0.1f, maxOrbitPredictionDuration));
            float sample = Mathf.Max(0.01f, orbitSampleInterval);
            int steps = Mathf.Clamp(Mathf.CeilToInt(duration / sample), 8, Mathf.Max(8, maxOrbitPredictionSteps));
            OrbitPredictor(body, duration, steps);
        }
    }

    /// <summary>
    /// Verifie si un objet est principalement attire par un seul corps.
    /// Si la force de l'astre principal est 5 fois superieure a la seconde,
    /// on considere que c'est une orbite stable a 2 corps.
    /// </summary>
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

    /// <summary>
    /// Utilise la 3e loi de Kepler et l'energie orbitale pour calculer la duree exacte d'une revolution complete.
    /// Permet de savoir quelle longueur de ligne dessiner pour faire un cercle parfait.
    /// </summary>
    float CalculateOrbitalPeriod(GravityBody body, GravityBody centralBody)
    {
        float distance = Vector3.Distance(body.rb.position, centralBody.rb.position);
        mu = G * gravityMultiplier * (body.rb.mass + centralBody.rb.mass);
        semiMajorAxis = Mathf.Clamp(1 / ((2 / distance) - (Mathf.Pow(body.rb.linearVelocity.magnitude, 2)) / mu), 0.1f, 10000f);
        return 2f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(semiMajorAxis, 3) / mu);
    }
    
    // ==========================================
    // GESTION DE L'ATTRACTION PHYSIQUE
    // ==========================================

    /// <summary>
    /// Prend les positions actuelles de tous les astres et calcules ou ils seront dans le futur
    /// en iterant rapidemnet sans impacter la vraie position des objets.
    /// </summary>
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
    
    /// <summary>
    /// Prend les positions actuelles de tous les astres et calculs ou ils seront dans le futur
    /// en iterant rapidements sans impacter la vraie position des objets.
    /// Optimisee pour les cas ou les objets bougent de facon previsible autour d'un astre.
    /// </summary>
    void DrawOrbitHybrid(GravityBody body, GravityBody centralBody, float period, int steps)
    {
        float maxDt = 0.5f;
        float dt = Mathf.Min(period / steps, maxDt);
        steps = Mathf.CeilToInt(period / dt);
        Vector3 startPosition = body.rb.position;
        Vector3 position = startPosition;
        Vector3 velocity = body.rb.linearVelocity;
        float gravConst = G * gravityMultiplier;
        float maxDistance = Mathf.Max(1f, maxOrbitPredictionDistance * 10f);

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
    
    // ==========================================
    // AFFICHAGE ET STABILITE VISUELLE
    // ==========================================
    
    /// <summary>
    /// Fonction de securite si la trajectoire calculee fait des virages trop brusques ou des segments trop longs,
    /// la focntion refuse de dessiner la ligne pour eviter des glitchs.
    /// </summary>
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
        float hardMaxSegment = Mathf.Max(0.01f, maxOrbitSegmentLength * semiMajorAxis);
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
            if ((points[i] - origin).sqrMagnitude > maxDistanceSqr * 10f)
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
    
    void OnToggleChanged(bool value)
    {
        orbitesOn = value;

        if (!orbitesOn)
        {
            ClearAllOrbitLines();
        }
    }

    void ClearAllOrbitLines()
    {
        foreach (var body in bodies)
        {
            if (body != null && body.line != null)
            {
                body.line.positionCount = 0;
            }
        }
    }
    
    /// <summary>
    /// Envoie les points calcules au composant LineRenderer de l'objet pour afficher la ligne.
    /// </summary>
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
    
    // ==========================================
    // FONCTIONS DIVERS
    // ==========================================
    
    /// <summary>
    /// Cette fonction sert à garder la liste d'astres propre et à éviter que le jeu plante.
    /// </summary>
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
    
    /// <summary>
    /// structure d'éléments orbitaux pour stocker les paramètres d'une orbite calculés à partir de la position et de la vitesse.
    /// </summary>
    public struct OrbitalElements
    {
        public float semiMajorAxis;
        public float eccentricity;
        public float periapsis;
        public float apoapsis;
        public float period;
    }
}