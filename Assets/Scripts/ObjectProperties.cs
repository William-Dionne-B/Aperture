using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectProperties : MonoBehaviour
{
    const float SolarMassKg = 1.98847e30f;

    [SerializeField]
    public string objectName;

    [Header("Comportement Spawner")]
    [Tooltip("Cochez si cet astre doit se mettre en orbite automatiquement comme une planète autour de son soleil")]
    public bool isOrbitalBody = true;
    
    [SerializeField]
    public float speedMagnitude;
    
    [FormerlySerializedAs("mass")] 
    [SerializeField]
    public float mass = 1f;

    public float Mass
    {
        get { return mass; }
        set
        {
            mass = value;
            if (thisRigidbody != null) thisRigidbody.mass = mass;
            if (thisGravityBody != null) thisGravityBody.Mass = mass;
        }
    }    
    
    [SerializeField]
    public float radius;
    [SerializeField]
    public float distanceToEtoile;
    [SerializeField]
    public float gravityMagnitude;
    [SerializeField]
    public float temperatureMagnitude;
    [SerializeField]
    public float periode;
    [SerializeField]
    public float density;
    public GameObject EtoileParent;

    [Header("Simulation Scales (Système Solaire)")]
    [Tooltip("1 unité de rayon = 13 900 km (soit 13 900 000 mètres)")]
    public float radiusToMetersScale = 13900000f;
    
    [Tooltip("1 unité = 1 391 609 km (soit 1 391 609 000 mètres)")]
    public float distanceToMetersScale = 1391609000f;
    
    [Tooltip("1 unité de masse = 1.988 * 10^15 kg (Millionième solaire)")]
    public float unityToKgScale = 1.988e24f;

    [Header("Thermodynamique (Étoile)")] 
    [Tooltip("Cochez ceci si cet astre est un Soleil")]
    public bool isStar = false;
    [Tooltip("Cochez ceci si cet astre est un trou noir")]
    public bool isBlackHole = false;
    [Tooltip("Masse minimale (en masses solaires) pour qu'une étoile devienne un trou noir")]
    [Min(0.01f)]
    public float blackHoleFormationMassSolar = 3f;
    [Tooltip("Prefab à instancier quand une étoile s'effondre en trou noir")]
    public GameObject blackHolePrefab;
    [Tooltip("Température de surface si c'est une étoile (Soleil = 5778 K)")]
    public float starSurfaceTemperature = 5778f;
    [Tooltip("Luminosité de l'étoile en Watts (Soleil = 3.828e26")]
    public float starLuminosity = 3.828e26f;

    [Header("Thermodynamique (Planète)")] 
    [Tooltip("Albédo : Capacité à refléter la lumière (Terre = 0.3")] 
    [Range(0f, 1f)] public float albedo = 0.3f;
    [Tooltip("Effet de serre en Kelvin (Terre = environ +33 K")]
    public float greenhouseEffect = 0f;
    
    private GameObject thisObject; 
    private Transform thisTransform;
    private Rigidbody thisRigidbody;
    private GravityBody thisGravityBody;
    private bool hasConvertedToBlackHole;

    public static List<ObjectProperties> AllStarsInSystem = new List<ObjectProperties>();
    
    void OnEnable()
    {
        EnsureStarRegistryState();
    }

    void OnDisable()
    {
        if (AllStarsInSystem.Contains(this))
        {
            AllStarsInSystem.Remove(this);
        }
    }
    
    void Start()
    {
        thisObject = (transform.parent != null) ? transform.parent.gameObject : this.gameObject;
        thisTransform = thisObject.GetComponent<Transform>();
        thisRigidbody = thisObject.GetComponent<Rigidbody>();
        thisGravityBody = thisObject.GetComponent<GravityBody>();

        if (mass <= 0) mass = 1;
        if (distanceToEtoile < 0) distanceToEtoile = 0;
        if (speedMagnitude < 0) speedMagnitude = 0;
        if (radius <= 0) radius = 1;
        
        Mass = mass;
        
        if (string.IsNullOrEmpty(objectName)) objectName = thisObject.name;
        else thisObject.name = objectName;

        EnsureStarRegistryState();
        TryConvertStarToBlackHole();

        if (thisRigidbody != null) StartCoroutine(UpdateSpeedRoutine());
        else speedMagnitude = 0f;
    }
    
    void Update()
    {
        if (thisTransform != null) thisTransform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);

        if (thisGravityBody != null) thisGravityBody.Mass = mass;

        EnsureStarRegistryState();
        TryConvertStarToBlackHole();
        
        if (EtoileParent == null && AllStarsInSystem.Count > 0)
        {
            ChercherEtoileLaPlusProche();
        }

        // --- GRAVITÉ ---
        if (radius > 0 && GravityManager.Instance != null)
        {
            float vraiRayonEnMetres = radius * radiusToMetersScale;
            float vraieMasseEnKg = mass * unityToKgScale;

            float constanteGravitationnelle = GravityManager.G * GravityManager.Instance.gravityMultiplier;

            gravityMagnitude = (constanteGravitationnelle * vraieMasseEnKg) / (vraiRayonEnMetres * vraiRayonEnMetres) / 1e9f;
        }

        else
        {
            gravityMagnitude = 0f;
        }

        // Calcul de la distance à l'étoile parente
        if (EtoileParent != null && distanceToEtoile > 0 && thisGravityBody != null)
        {
            GravityBody starGravity = EtoileParent.GetComponent<GravityBody>();

            if (starGravity != null && starGravity.rb != null)
            {
                double G_phys = GravityManager.G * GravityManager.Instance.gravityMultiplier;
                double mu = G_phys * starGravity.Mass;

                // CORRECTION MAJEURE : On utilise la vitesse relative à l'étoile !
                Vector3 relativeVelocity = thisGravityBody.rb.linearVelocity - starGravity.rb.linearVelocity;
                double v = relativeVelocity.magnitude;
                double r = distanceToEtoile;

                double denom = (2.0 / r) - (v * v) / mu;

                // Si denom est positif, c'est une orbite fermée (Cercle ou Ellipse)
                if (denom > 1e-12)
                {
                    double a = 1.0 / denom;
                    double T = 2.0 * Math.PI * Math.Sqrt((a * a * a) / mu);
                    periode = (float)(T / 86400.0); // Conversion en jours
                }
                else
                {
                    // Si denom est négatif, l'astre s'échappe de l'étoile (Hyperbole) !
                    periode = 0f;
                }
            }
        }
        else
        {
            periode = 0f;
        }
        
        if (speedMagnitude > 0 && distanceToEtoile > 0)
        {
            GravityBody starGravity = EtoileParent.GetComponent<GravityBody>();
            const float SecondsToDays = 1f / 86400f;

            double G = GravityManager.G * GravityManager.Instance.gravityMultiplier;
            double masseEtoileKg = starGravity.Mass;
            double v = (thisGravityBody.rb.linearVelocity.magnitude);
            double r = distanceToEtoile;
            double mu = G * masseEtoileKg;

            double denom = (2.0 / r) - (v * v) / mu;

            Debug.Log("denom"+denom);
            //if (Math.Abs(denom) < 1e-12)
            //{
            //    periode = 0f;
            //    return;
            //}

            double a = 1.0 / denom;

            double T = 2.0 * Math.PI * Math.Sqrt((a * a * a) / mu);

            periode = (float)(T / 86400.0);
        }
        else
        {
            periode = 0f;
        }

        // --- DENSITÉ ---
        if (mass > 0 && radius > 0) density = mass * unityToKgScale / ((4f / 3f) * Mathf.PI * Mathf.Pow(radius * radiusToMetersScale, 3));
        else density = 0f;

        // --- THERMODYNAMIQUE ---
        ActualiserTemperature();
    }

    void EnsureStarRegistryState()
    {
        if (isStar)
        {
            if (!AllStarsInSystem.Contains(this))
            {
                AllStarsInSystem.Add(this);
            }
            return;
        }

        if (AllStarsInSystem.Contains(this))
        {
            AllStarsInSystem.Remove(this);
        }
    }

    void TryConvertStarToBlackHole()
    {
        if (!isStar || isBlackHole || hasConvertedToBlackHole)
        {
            return;
        }

        float massScale = Mathf.Max(unityToKgScale, 0.0001f);
        float blackHoleThreshold = (blackHoleFormationMassSolar * SolarMassKg) / massScale;

        if (mass < blackHoleThreshold)
        {
            return;
        }

        hasConvertedToBlackHole = true;

        if (blackHolePrefab != null)
        {
            ReplaceByBlackHolePrefab();
            return;
        }

        isBlackHole = true;
        isStar = false;
        starLuminosity = 0f;
        starSurfaceTemperature = 0f;

        EnsureStarRegistryState();

        Debug.Log($"{name} has collapsed into a black hole (mass threshold reached).");
    }

    void ReplaceByBlackHolePrefab()
    {
        GameObject sourceObject = thisObject != null ? thisObject : gameObject;
        Transform sourceTransform = sourceObject.transform;
        Rigidbody sourceBody = sourceObject.GetComponent<Rigidbody>();

        Vector3 position = sourceBody != null ? sourceBody.position : sourceTransform.position;
        Quaternion rotation = sourceBody != null ? sourceBody.rotation : sourceTransform.rotation;
        Vector3 velocity = sourceBody != null ? sourceBody.linearVelocity : Vector3.zero;
        Vector3 angularVelocity = sourceBody != null ? sourceBody.angularVelocity : Vector3.zero;

        Transform parent = sourceTransform.parent;
        GameObject blackHoleObject = Instantiate(blackHolePrefab, position, rotation, parent);
        blackHoleObject.name = sourceObject.name;
        blackHoleObject.transform.localScale = sourceTransform.localScale;

        ObjectProperties blackHoleProperties = blackHoleObject.GetComponent<ObjectProperties>();
        if (blackHoleProperties != null)
        {
            blackHoleProperties.objectName = objectName;
            blackHoleProperties.isStar = false;
            blackHoleProperties.isBlackHole = true;
            blackHoleProperties.isOrbitalBody = isOrbitalBody;
            blackHoleProperties.radius = radius;
            blackHoleProperties.distanceToEtoile = distanceToEtoile;
            blackHoleProperties.periode = periode;
            blackHoleProperties.EtoileParent = EtoileParent;
            blackHoleProperties.starLuminosity = 0f;
            blackHoleProperties.starSurfaceTemperature = 0f;
            blackHoleProperties.blackHoleFormationMassSolar = blackHoleFormationMassSolar;
            blackHoleProperties.unityToKgScale = unityToKgScale;
            blackHoleProperties.radiusToMetersScale = radiusToMetersScale;
            blackHoleProperties.distanceToMetersScale = distanceToMetersScale;
            blackHoleProperties.albedo = albedo;
            blackHoleProperties.greenhouseEffect = greenhouseEffect;
            blackHoleProperties.Mass = mass;
        }

        Rigidbody blackHoleBody = blackHoleObject.GetComponent<Rigidbody>();
        if (blackHoleBody != null)
        {
            blackHoleBody.mass = mass;
            blackHoleBody.linearVelocity = velocity;
            blackHoleBody.angularVelocity = angularVelocity;
        }

        ReassignChildrenStarParent(sourceObject, blackHoleObject);
        EnsureStarRegistryState();

        Debug.Log($"{sourceObject.name} has collapsed into a black hole prefab.", blackHoleObject);

        Destroy(sourceObject);
    }

    void ReassignChildrenStarParent(GameObject previousStar, GameObject newStar)
    {
        if (previousStar == null || newStar == null)
        {
            return;
        }

        ObjectProperties[] allBodies = FindObjectsByType<ObjectProperties>(FindObjectsSortMode.None);
        for (int index = 0; index < allBodies.Length; index++)
        {
            ObjectProperties body = allBodies[index];
            if (body == null)
            {
                continue;
            }

            if (body.EtoileParent == previousStar)
            {
                body.EtoileParent = newStar;
            }
        }
    }

    void ChercherEtoileLaPlusProche()
    {
        float distMin = float.MaxValue;
        foreach (var star in AllStarsInSystem)
        {
            // CORRECTION : On s'assure à 100% que l'astre ne s'adopte pas lui-même !
            if (star == null || star.gameObject == this.gameObject) continue;
            
            float d = Vector3.Distance(transform.position, star.transform.position);
            if (d < distMin)
            {
                distMin = d;
                EtoileParent = star.gameObject;
            }
        }
    }

    // Gestion propre de la température
    void ActualiserTemperature()
    {
        if (isStar)
        {
            temperatureMagnitude = starSurfaceTemperature;
        }
        else
        {
            float sommeEnergieStellaire = 0f;

            foreach (ObjectProperties star in AllStarsInSystem)
            {
                if (star == null) continue;

                float distUnity = Vector3.Distance(thisTransform.position, star.transform.position);
                if (distUnity > 0)
                {
                    float vraieDistanceMetres = distUnity * distanceToMetersScale;
                    sommeEnergieStellaire += star.starLuminosity / (vraieDistanceMetres * vraieDistanceMetres);
                }
            }

            if (sommeEnergieStellaire > 0f)
            {
                float sigma = 5.67e-8f;
                float numerateur = sommeEnergieStellaire * (1f - albedo);
                float denominateur = 16f * Mathf.PI * sigma;

                float tempEquilibre = Mathf.Pow(numerateur / denominateur, 0.25f);
                temperatureMagnitude = tempEquilibre + greenhouseEffect;
            }
            else
            {
                temperatureMagnitude = greenhouseEffect;
            }
        }
    }

    private IEnumerator UpdateSpeedRoutine()
    {
        var wait = new WaitForSeconds(0.1f); // 10 Hz
        while (true)
        {
            if (thisRigidbody != null) speedMagnitude = thisRigidbody.linearVelocity.magnitude;
            else speedMagnitude = 0f;
            yield return wait;
        }
    }
}