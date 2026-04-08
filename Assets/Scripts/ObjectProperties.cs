using System;
using System.Collections;
using UnityEngine;

public class ObjectProperties : MonoBehaviour
{
    [SerializeField]
    public string objectName;
    [SerializeField]
    public float speedMagnitude;
    [SerializeField]
    public float mass;
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

    [Header("Thermodynamique")] [Tooltip("Luminosité de l'étoile en Watts (Soleil = 3.828e26")]
    public float starLuminosity = 3.828e26f;

    [Tooltip("Albédo : Capacité à refléter la lumière (Terre = 0.3")] [Range(0f, 1f)]
    public float albedo = 0.3f;

    [Tooltip("Effet de serre en Kelvin (Terre = environ +33 K")]
    public float greenhouseEffect = 0f;
        
    
    private GameObject thisObject; // L'objet parent du script
    private Transform thisTransform;
    private Rigidbody thisRigidbody;
    private GravityBody thisGravityBody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Assigne le parent si présent, sinon garde le GameObject courant
        thisObject = (transform.parent != null) ? transform.parent.gameObject : this.gameObject;

        // Cache des composants fréquemment utilisés
        thisTransform = thisObject.GetComponent<Transform>();
        thisRigidbody = thisObject.GetComponent<Rigidbody>();
        thisGravityBody = thisObject.GetComponent<GravityBody>();

        // Vérifications de la validité des propriétés procurés au lancement
        if (mass <= 0) mass = 1;
        if (distanceToEtoile < 0) distanceToEtoile = 0;
        if (speedMagnitude < 0) speedMagnitude = 0;
        if (radius <= 0) radius = 1;
        if (string.IsNullOrEmpty(objectName)) 
        {
            objectName = thisObject.name; 
        }
        
        else 
        {
            thisObject.name = objectName;
        }
        //TODO : donner un prénom et nom de famille au hazard à partir d'une banque de noms?

        // Démarre la mise à jour de la vitesse à 10 Hz si un Rigidbody est présent
        if (thisRigidbody != null)
        {
            StartCoroutine(UpdateSpeedRoutine());
        }
        else
        {
            // Si pas de Rigidbody, on garde speedMagnitude à 0 (ou valeur définie)
            speedMagnitude = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Applique l'échelle
        if (thisTransform != null)
            thisTransform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);

        // Met à jour la masse du Rigidbody et du GravityBody chaque frame si présents
        if (thisRigidbody != null)
            thisRigidbody.mass = mass;

        if (thisGravityBody != null)
            thisGravityBody.Mass = mass;

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
        if (EtoileParent != null && thisTransform != null)
        {
            Vector3 posEtoile = EtoileParent.transform.position;
            Vector3 posBody = thisTransform.position;
            Vector3 s = posEtoile - posBody;

            distanceToEtoile = s.magnitude;
        }

        //calcule de la période de révolution autour de l'étoile parente
        if (speedMagnitude > 0 && distanceToEtoile > 0)
        {
            periode = (float)Math.Round((2 * Mathf.PI * distanceToEtoile) / speedMagnitude, 2);
        }
        else
        {
            periode = 0f; // Période indéfinie si vitesse ou distance nulle
        }

        if (mass > 0 && radius > 0) 
        {
            density = mass / ((4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3));
        }
        else
        {
            density = 0f; // Densité indéfinie si masse ou rayon nulle
        }

        if (EtoileParent != null && distanceToEtoile > 0)
        {
            float vraieDistanceMetres = distanceToEtoile * distanceToMetersScale;

            float sigma = 5.67e-8f;

            float numerateur = starLuminosity * (1f - albedo);
            float denominateur = 16f * Mathf.PI * sigma * (vraieDistanceMetres * vraieDistanceMetres);

            float tempEquilibre = Mathf.Pow(numerateur / denominateur, 0.25f);

            temperatureMagnitude = tempEquilibre + greenhouseEffect;
        }    
    }
    

    // Coroutine qui met à jour speedMagnitude 10 fois par seconde (toutes les 0.1s)
    private IEnumerator UpdateSpeedRoutine()
    {
        var wait = new WaitForSeconds(0.1f); // 10 Hz
        while (true)
        {
            if (thisRigidbody != null)
                speedMagnitude = thisRigidbody.linearVelocity.magnitude;
            else
                speedMagnitude = 0f;

            yield return wait;
        }
    }
}