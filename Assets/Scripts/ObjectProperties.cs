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
    public float distanceSoleil;
    [SerializeField]
    public string objectName;

    private GameObject thisObject; // L'objet parent du script

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Assigne le parent si pr�sent, sinon garde le GameObject courant
        thisObject = (transform.parent != null) ? transform.parent.gameObject : this.gameObject;

        // V�rifications de la validit� des propri�t�s procur�s au lancement
        if (mass <= 0) mass = 1;
        if (speedMagnitude < 0) speedMagnitude = 0;
        if (radius <= 0) radius = 1;
        if (distanceSoleil < 0) distanceSoleil = 0;
        if (string.IsNullOrEmpty(objectName)) objectName = "Bla bla bla"; //TODO : donner un pr�nom et nom de famille au hazard � partir d'une banque de noms?
    }

    // Update is called once per frame
    void Update()
    {
        thisObject.GetComponent<Transform>().localScale = new Vector3(2*radius, 2*radius, 2*radius);
    }
}
