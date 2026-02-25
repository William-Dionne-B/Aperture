using UnityEngine;

public class ObjectManager : MonoBehaviour
{

    private GameObject selection; // Objet sélectionné

    public GameObject MainCamera; // Camera principale

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (selection == null) Debug.Log("Sélection inexistante");
    }

    // Update is called once per frame
    void Update()
    {
        var props = MainCamera.GetComponent<ObjectProperties>(); // Aller chercher la propriété SelectedObject
        updateInfoAvailability();
    }

    // Affiche ou non le ui info si il y existe une sélection
    void updateInfoAvailability()
    {
         //if selection != null
    }
}
