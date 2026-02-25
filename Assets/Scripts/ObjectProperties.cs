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
    public string objectName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Vérifications de la validité des propriétés procurés au lancement
        if (mass <= 0) mass = 1;
        if (speedMagnitude < 0) speedMagnitude = 0;
        if (radius <= 0) radius = 1;
        if (string.IsNullOrEmpty(objectName)) objectName = "Bla bla bla"; //TODO : donner un prénom et nom de famille au hazard à partir d'une banque de noms?
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
