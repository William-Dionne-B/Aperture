using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ObjectManager : MonoBehaviour
{

    private GameObject selection; // Objet sélectionné

    public GameObject MainCamera; // Camera principale

    public GameObject InfoUI; // UI d'information

    public GameObject speed; // Champ de texte pour la vitesse

    public GameObject mass; // Champ de texte pour la masse

    public GameObject radius; // Champ de texte pour le rayon

    public GameObject obj_name; // Champ de texte pour le nom TODO : faire marcher le changement de nom

    public GameObject dist_etoile; // Champ de texte pour la distance à l'étoile (lecture seule)

    // Références liées aux listeners pour pouvoir détacher proprement
    TMP_InputField massTmp; InputField massUi; UnityAction<string> massListener;
    TMP_InputField speedTmp; InputField speedUi; UnityAction<string> speedListener;
    TMP_InputField radiusTmp; InputField radiusUi; UnityAction<string> radiusListener;
    TMP_InputField nameTmp; InputField nameUi; UnityAction<string> nameListener;

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
            BindFieldListeners(); // réassocie les listeners pour la nouvelle sélection / nouveau contexte UI
        }

        // Ne met pas à jour l'UI si l'utilisateur est en train d'éditer un champ
        if (IsAnyFieldEditing())
        {
            return;
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
                SetText(obj_name, props.objectName);

                // Calcul robuste/affichage de la distance à l'étoile :
                // - si l'EtoileParent est fourni, calcule la distance à la position courante et met à jour la propriété.
                // - sinon affiche la valeur existante ou "N/A".
                if (props.EtoileParent != null)
                {
                    try
                    {
                        Vector3 posEtoile = props.EtoileParent.transform.position;
                        Vector3 posBody = selection.transform.position;
                        float dist = Vector3.Distance(posEtoile, posBody);
                        props.distanceToEtoile = dist;
                        SetText(dist_etoile, dist.ToString("G"));
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Impossible de calculer la distance à l'étoile : {ex.Message}");
                        SetText(dist_etoile, "N/A");
                    }
                }
                else
                {
                    // Affiche la valeur déjà calculée ou "N/A" si invalide
                    if (props.distanceToEtoile >= 0f)
                        SetText(dist_etoile, props.distanceToEtoile.ToString("G"));
                    else
                        SetText(dist_etoile, "N/A");
                }
            }
            else
            {
                // Si le component est manquant, on vide les champs et loggue
                SetText(mass, "");
                SetText(speed, "");
                SetText(radius, "");
                SetText(dist_etoile, "");
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

    // Retourne true si l'un des champs assignés est en cours d'édition (focus)
    bool IsAnyFieldEditing()
    {
        if (IsFieldEditing(mass)) return true;
        if (IsFieldEditing(speed)) return true;
        if (IsFieldEditing(radius)) return true;
        if (IsFieldEditing(obj_name)) return true;
        if (IsFieldEditing(dist_etoile)) return true;
        return false;
    }

    // Vérifie un GameObject pour TMP_InputField ou InputField et teste la propriété isFocused
    bool IsFieldEditing(GameObject field)
    {
        if (field == null) return false;

        var tmpInput = field.GetComponent<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        var uiInput = field.GetComponent<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        // Cas où le composant InputField est sur un enfant (ex: structure TMP Input Field)
        tmpInput = field.GetComponentInChildren<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        uiInput = field.GetComponentInChildren<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        return false;
    }

    // Lie les callbacks OnEndEdit aux champs (détache d'abord les anciens listeners)
    void BindFieldListeners()
    {
        UnbindAllFieldListeners();

        // mass
        massTmp = GetTMPInput(mass);
        massUi = GetLegacyInput(mass);
        if (massTmp != null)
        {
            massListener = (s) => OnMassEndEdit(s);
            massTmp.onEndEdit.AddListener(massListener);
        }
        else if (massUi != null)
        {
            massListener = (s) => OnMassEndEdit(s);
            massUi.onEndEdit.AddListener(massListener);
        }

        // speed
        speedTmp = GetTMPInput(speed);
        speedUi = GetLegacyInput(speed);
        if (speedTmp != null)
        {
            speedListener = (s) => OnSpeedEndEdit(s);
            speedTmp.onEndEdit.AddListener(speedListener);
        }
        else if (speedUi != null)
        {
            speedListener = (s) => OnSpeedEndEdit(s);
            speedUi.onEndEdit.AddListener(speedListener);
        }

        // radius
        radiusTmp = GetTMPInput(radius);
        radiusUi = GetLegacyInput(radius);
        if (radiusTmp != null)
        {
            radiusListener = (s) => OnRadiusEndEdit(s);
            radiusTmp.onEndEdit.AddListener(radiusListener);
        }
        else if (radiusUi != null)
        {
            radiusListener = (s) => OnRadiusEndEdit(s);
            radiusUi.onEndEdit.AddListener(radiusListener);
        }

        // name
        nameTmp = GetTMPInput(obj_name);
        nameUi = GetLegacyInput(obj_name);
        if (nameTmp != null)
        {
            nameListener = (s) => OnNameEndEdit(s);
            nameTmp.onEndEdit.AddListener(nameListener);
        }
        else if (nameUi != null)
        {
            nameListener = (s) => OnNameEndEdit(s);
            nameUi.onEndEdit.AddListener(nameListener);
        }
    }

    void UnbindAllFieldListeners()
    {
        if (massTmp != null && massListener != null) massTmp.onEndEdit.RemoveListener(massListener);
        if (massUi != null && massListener != null) massUi.onEndEdit.RemoveListener(massListener);
        massTmp = null; massUi = null; massListener = null;

        if (speedTmp != null && speedListener != null) speedTmp.onEndEdit.RemoveListener(speedListener);
        if (speedUi != null && speedListener != null) speedUi.onEndEdit.RemoveListener(speedListener);
        speedTmp = null; speedUi = null; speedListener = null;

        if (radiusTmp != null && radiusListener != null) radiusTmp.onEndEdit.RemoveListener(radiusListener);
        if (radiusUi != null && radiusListener != null) radiusUi.onEndEdit.RemoveListener(radiusListener);
        radiusTmp = null; radiusUi = null; radiusListener = null;

        if (nameTmp != null && nameListener != null) nameTmp.onEndEdit.RemoveListener(nameListener);
        if (nameUi != null && nameListener != null) nameUi.onEndEdit.RemoveListener(nameListener);
        nameTmp = null; nameUi = null; nameListener = null;
    }

    TMP_InputField GetTMPInput(GameObject field)
    {
        if (field == null) return null;
        var tmp = field.GetComponent<TMP_InputField>();
        if (tmp != null) return tmp;
        return field.GetComponentInChildren<TMP_InputField>();
    }

    InputField GetLegacyInput(GameObject field)
    {
        if (field == null) return null;
        var ui = field.GetComponent<InputField>();
        if (ui != null) return ui;
        return field.GetComponentInChildren<InputField>();
    }

    // Callbacks de fin d'édition  valident et appliquent si valide, sinon restaurent l'affichage
    void OnMassEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
        {
            props.mass = v;
        }
        else
        {
            // invalide -> restaure l'affichage
            SetText(mass, props.mass.ToString("G"));
        }
        updateUIVisibility();
    }

    void OnSpeedEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
        {
            props.speedMagnitude = v;
        }
        else
        {
            SetText(speed, props.speedMagnitude.ToString("G"));
        }
        updateUIVisibility();
    }

    void OnRadiusEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
        {
            props.radius = v;
        }
        else
        {
            SetText(radius, props.radius.ToString("G"));
        }
        updateUIVisibility();
    }

    void OnNameEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        // Accepte toute chaîne non nulle ; si vous voulez interdire vide, changez la condition
        if (input != null)
        {
            props.objectName = input;
        }
        else
        {
            SetText(obj_name, props.objectName);
        }
        updateUIVisibility();
    }

    // Essaie plusieurs cultures pour être tolérant (ex : virgule ou point)
    bool TryParseFloatFlexible(string s, out float result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0f;
            return false;
        }

        // Première passe: culture courante (utile pour les utilisateurs fr-FR avec virgule)
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result))
            return true;

        // Deuxième passe: invariant (point)
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            return true;

        // Troisième passe: remplace virgule par point (au cas où)
        var replaced = s.Replace(',', '.');
        if (float.TryParse(replaced, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            return true;

        result = 0f;
        return false;
    }

    void OnDestroy()
    {
        UnbindAllFieldListeners();
    }
}