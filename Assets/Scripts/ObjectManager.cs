using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectManager : MonoBehaviour
{

    private GameObject selection; // Objet sélectionné

    public GameObject MainCamera; // Camera principale

    public GameObject InfoUI; // UI d'information

    public GameObject speed; // Champ de texte pour la vitesse

    public GameObject mass; // Champ de texte pour la masse

    public GameObject radius; // Champ de texte pour le rayon

    public GameObject obj_name; // Champ de texte pour le nom TODO : faire marcher le changement de nom

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (selection == null) Debug.Log("Sélection inexistante");
        updateUIVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        if (MainCamera == null)
        {
            Debug.LogWarning("MainCamera non assignée dans ObjectManager.");
            return;
        }

        var click = MainCamera.GetComponent<ClickDetection>(); // Aller chercher le component ClickDetection
        if (click == null)
        {
            Debug.LogWarning("ClickDetection manquant sur MainCamera.");
            return;
        }

        // Utilise le champ public `selectedObject` défini dans ClickDetection
        var selected = click.selectedObject;
        if (selection != selected)
        {
            selection = selected;
        }

        updateUIVisibility();
    }

    void updateUIVisibility()
    {
        if (InfoUI == null)
        {
            Debug.LogWarning("InfoUI non assignée dans ObjectManager.");
            return;
        }

        if (selection != null)
        {
            InfoUI.SetActive(true);

            var props = selection.GetComponent<ObjectProperties>();
            if (props != null)
            {
                // Met à jour les champs UI avec les valeurs des propriétés (utilise les champs tels que définis dans ObjectProperties)
                SetText(mass, props.mass.ToString("G"));
                SetText(speed, props.speedMagnitude.ToString("G"));
                SetText(radius, props.radius.ToString("G"));
            }
            else
            {
                // Si le component est manquant, on vide les champs et loggue
                SetText(mass, "");
                SetText(speed, "");
                SetText(radius, "");
                Debug.LogWarning("ObjectProperties manquant sur l'objet sélectionné.");
            }
        }
        else
        {
            InfoUI.SetActive(false);
        }
    }

    // Essaie de mettre à jour le texte sur plusieurs types courants :
    // TMP_InputField, legacy InputField, TMP_Text, Text, ou leurs enfants.
    void SetText(GameObject field, string value)
    {
        if (field == null) return;

        // TMP Input Field (TextMeshPro - Input Field)
        var tmpInput = field.GetComponent<TMP_InputField>();
        if (tmpInput != null)
        {
            tmpInput.text = value;
            return;
        }

        // Legacy UI InputField
        var uiInput = field.GetComponent<InputField>();
        if (uiInput != null)
        {
            uiInput.text = value;
            return;
        }

        // Direct TMP text component
        var tmp = field.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        // Direct legacy Text component
        var uiText = field.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = value;
            return;
        }

        // Cherche un TMP_Text dans les enfants (structure courante des TMP Input Field)
        tmp = field.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        // Cherche un Text legacy dans les enfants
        uiText = field.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = value;
            return;
        }

        Debug.LogWarning($"Champ UI \"{field.name}\" n'a pas de composant Text, TMP ou InputField.");
    }
}
