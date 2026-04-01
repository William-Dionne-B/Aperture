using System;
using System.Collections;
using UnityEngine;

public class ObjectProperties : MonoBehaviour
{
    [SerializeField]
    public float mass;
    [SerializeField]
    public float speedMagnitude;
    [SerializeField]
    public float radius;
    [SerializeField]
    public GameObject EtoileParent;
    [SerializeField]
    public float distanceToEtoile;
    [SerializeField]
    public string objectName;
    [SerializeField]
    public float periode;
    [SerializeField]
    public float density;

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