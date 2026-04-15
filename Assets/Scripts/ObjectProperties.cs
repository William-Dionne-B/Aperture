using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectProperties : MonoBehaviour
{
    [SerializeField]
    public string objectName;

    [Header("Comportement Spawner")]
    [Tooltip("Cochez si cet astre doit se mettre en orbite automatiquement comme une planète autour de son soleil")]
    public bool isOrbitalBody = true;
    
    [SerializeField]
    public float speedMagnitude;
    
    [FormerlySerializedAs("mass")] 
    [SerializeField]
    private float _mass = 1f;

    public float Mass
    {
        get { return _mass; }
        set
        {
            _mass = value;
            if (thisRigidbody != null) thisRigidbody.mass = _mass;
            if (thisGravityBody != null) thisGravityBody.Mass = _mass;
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

    public static List<ObjectProperties> AllStarsInSystem = new List<ObjectProperties>();
    
    void OnEnable()
    {
        if (isStar && !AllStarsInSystem.Contains(this))
        {
            AllStarsInSystem.Add(this);
        }
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

        if (_mass <= 0) _mass = 1;
        if (distanceToEtoile < 0) distanceToEtoile = 0;
        if (speedMagnitude < 0) speedMagnitude = 0;
        if (radius <= 0) radius = 1;
        
        Mass = _mass;
        
        if (string.IsNullOrEmpty(objectName)) objectName = thisObject.name;
        else thisObject.name = objectName;

        if (thisRigidbody != null) StartCoroutine(UpdateSpeedRoutine());
        else speedMagnitude = 0f;
    }
    
    void Update()
    {
        if (thisTransform != null) thisTransform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);

        if (thisGravityBody != null) thisGravityBody.Mass = _mass;
        
        if (EtoileParent == null && AllStarsInSystem.Count > 0)
        {
            ChercherEtoileLaPlusProche();
        }

        // --- GRAVITÉ ---
        if (radius > 0 && GravityManager.Instance != null)
        {
            float vraiRayonEnMetres = radius * radiusToMetersScale;
            float vraieMasseEnKg = _mass * unityToKgScale;
            float constanteGravitationnelle = GravityManager.G * GravityManager.Instance.gravityMultiplier;
            
            gravityMagnitude = (constanteGravitationnelle * vraieMasseEnKg) / (vraiRayonEnMetres * vraiRayonEnMetres) / 1e9f;
        }
        else gravityMagnitude = 0f;
        
        // --- DISTANCE & PÉRIODE ---
        if (EtoileParent != null && thisTransform != null)
        {
            Vector3 posEtoile = EtoileParent.transform.position;
            Vector3 posBody = thisTransform.position;
            distanceToEtoile = Vector3.Distance(posEtoile, posBody);
        }
        
        if (speedMagnitude > 0 && distanceToEtoile > 0) periode = (float)Math.Round((2 * Mathf.PI * distanceToEtoile) / speedMagnitude, 2);
        else periode = 0f;

        // --- DENSITÉ ---
        if (_mass > 0 && radius > 0) density = _mass / ((4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3));
        else density = 0f;

        // --- THERMODYNAMIQUE ---
        ActualiserTemperature();
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