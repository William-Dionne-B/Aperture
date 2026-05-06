using System;
using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Émissivité thermique (ε). Un corps noir parfait est à 1.0.")]
    [Range(0.1f, 1f)] public float emissivity = 1.0f;
    [Tooltip("0.25 = Pas de redistribution (Chaleur concentrée face au Soleil), 1.0 = Température moyenne globale")]
    [Range(0.25f, 1.0f)] public float heatRedistributionFactor = 1.0f;
    [Tooltip("Effet de serre en Kelvin (Terre = environ +33 K")]
    public float greenhouseEffect = 0f;
    
    private GameObject thisObject; 
    private Transform thisTransform;
    private Rigidbody thisRigidbody;
    private GravityBody thisGravityBody;
    private bool hasConvertedToBlackHole;
    private bool aCalculerDistanceInitiale = false;
    private double vraieDistanceInitialeMetres = 0;

    public static List<ObjectProperties> AllStarsInSystem = new List<ObjectProperties>();
    
    // ==========================================
    // MÉTHODES UNITY
    // ==========================================
    
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

        if (EtoileParent != null && thisTransform != null)
        {
            distanceToEtoile = Vector3.Distance(EtoileParent.transform.position, thisTransform.position);
            
            if (!aCalculerDistanceInitiale && distanceToEtoile > 0)
            {
                vraieDistanceInitialeMetres = distanceToEtoile * distanceToMetersScale;
                aCalculerDistanceInitiale = true;
            }
        }
        else
        {
            distanceToEtoile = 0f;
        }

        // --- GRAVITÉ ---
        if (radius > 0 && GravityManager.Instance != null)
        {
            float vraiRayonEnMetres = radius * radiusToMetersScale;
            float vraieMasseEnKg = mass * unityToKgScale;
            float constanteGravitationnelle = GravityManager.G * GravityManager.Instance.gravityMultiplier;
            gravityMagnitude = (float)System.Math.Round(((constanteGravitationnelle * vraieMasseEnKg) / (vraiRayonEnMetres * vraiRayonEnMetres) / 1e9f), 2);
        }
        else
        {
            gravityMagnitude = 0f;
        }

        // --- PÉRIODE ORBITALE ---
        if (EtoileParent != null && aCalculerDistanceInitiale)
        {
            ObjectProperties starProps = EtoileParent.GetComponent<ObjectProperties>();

            if (starProps != null && starProps.Mass > 0)
            {
                double vraieMasseKg = starProps.Mass * unityToKgScale;
                double vraieConstanteG = 6.67430e-11;

                double mu = vraieConstanteG * vraieMasseKg;

                double r3 = vraieDistanceInitialeMetres * vraieDistanceInitialeMetres * vraieDistanceInitialeMetres;
                double periodeEnSecondes = 2.0 * Math.PI * Math.Sqrt(r3 / mu);

                periode = (float)System.Math.Round(periodeEnSecondes / 86400.0, 2);
            }
            else
            {
                periode = 0f;
            }
        }
        else
        {
            periode = 0f;
        }

        // --- DENSITÉ ---
        if (mass > 0 && radius > 0) 
        {
            float volumeM3 = (4f / 3f) * Mathf.PI * Mathf.Pow(radius * radiusToMetersScale, 3);
            float densityKgM3 = (mass * unityToKgScale) / volumeM3;
            density = (float)System.Math.Round(densityKgM3 / 1000f, 2);
        }
        else 
        {
            density = 0f;
        }

        // --- THERMODYNAMIQUE ---
        ActualiserTemperature();
    }

    /// <summary>
    /// Permet aux planetes de savoir quelles etoiles existent pour calculer leur temperature.
    /// </summary>
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
    
    /// <summary>
    /// Scanne la liste des etoiles et definit laquelle est le "parent"
    /// </summary>
    void ChercherEtoileLaPlusProche()
    {
        float distMin = float.MaxValue;
        foreach (var star in AllStarsInSystem)
        {
            if (star == null || star.gameObject == this.gameObject) continue;
            
            float d = Vector3.Distance(transform.position, star.transform.position);
            if (d < distMin)
            {
                distMin = d;
                EtoileParent = star.gameObject;
            }
        }
    }

    /// <summary>
    /// Utilise la loi de Stefan-Boltzmann pour calculer la temperature d'equilibre.
    /// Additionne l'energie recue de toutes les etoiles du systeme, prend en compte l'Albedo et l'Effet de serre.
    /// </summary>
    void ActualiserTemperature()
    {
        if (isBlackHole)
        {
            temperatureMagnitude = 0f;
            albedo = 0f; 
        }
        
        else if (isStar)
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
    
                float factor = Mathf.Lerp(4f, 16f, heatRedistributionFactor);
                float denominateur = factor * Mathf.PI * sigma * emissivity;

                float tempEquilibre = Mathf.Pow(numerateur / denominateur, 0.25f);
                temperatureMagnitude = tempEquilibre + greenhouseEffect;
            }
            
            else
            {
                temperatureMagnitude = greenhouseEffect;
            }
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