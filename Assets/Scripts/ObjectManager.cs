using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ObjectManager : MonoBehaviour
{

    private GameObject selection; // Objet s�lectionn�

    public GameObject MainCamera; // Camera principale

    public GameObject InfoUI; // UI d'information

    public GameObject speed; // Champ de texte pour la vitesse

    public GameObject mass; // Champ de texte pour la masse

    public GameObject radius; // Champ de texte pour le rayon
    
    public GameObject distanceSoleil;

    public GameObject obj_name; // Champ de texte pour le nom TODO : faire marcher le changement de nom

    // R�f�rences li�es aux listeners pour pouvoir d�tacher proprement
    TMP_InputField massTmp; InputField massUi; UnityAction<string> massListener;
    TMP_InputField speedTmp; InputField speedUi; UnityAction<string> speedListener;
    TMP_InputField radiusTmp; InputField radiusUi; UnityAction<string> radiusListener;
    TMP_InputField nameTmp; InputField nameUi; UnityAction<string> nameListener;
    TMP_InputField distanceSoleilTmp; InputField distanceSoleilUi; UnityAction<string> distanceSoleilListener;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (selection == null) Debug.Log("S�lection inexistante");
        updateUIVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        if (MainCamera == null)
        {
            Debug.LogWarning("MainCamera non assign�e dans ObjectManager.");
            return;
        }

        var click = MainCamera.GetComponent<ClickDetection>(); // Aller chercher le component ClickDetection
        if (click == null)
        {
            Debug.LogWarning("ClickDetection manquant sur MainCamera.");
            return;
        }

        // Utilise le champ public `selectedObject` d�fini dans ClickDetection
        var selected = click.selectedObject;
        if (selection != selected)
        {
            selection = selected;
            BindFieldListeners(); // r�associe les listeners pour la nouvelle s�lection / nouveau contexte UI
        }

        // Ne met pas � jour l'UI si l'utilisateur est en train d'�diter un champ
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
            Debug.LogWarning("InfoUI non assign�e dans ObjectManager.");
            return;
        }

        if (selection != null)
        {
            InfoUI.SetActive(true);

            var props = selection.GetComponent<ObjectProperties>();
            if (props != null)
            {
                // Met � jour les champs UI avec les valeurs des propri�t�s (utilise les champs tels que d�finis dans ObjectProperties)
                SetText(mass, props.mass.ToString("G"));
                SetText(speed, props.speedMagnitude.ToString("G"));
                SetText(radius, props.radius.ToString("G"));
                SetText(radius, props.distanceSoleil.ToString("G"));
                SetText(obj_name, props.objectName);
            }
            else
            {
                // Si le component est manquant, on vide les champs et loggue
                SetText(mass, "");
                SetText(speed, "");
                SetText(radius, "");
                SetText(distanceSoleil, "");
                Debug.LogWarning("ObjectProperties manquant sur l'objet s�lectionn�.");
            }
        }
        else
        {
            InfoUI.SetActive(false);
        }
    }

    // Essaie de mettre � jour le texte sur plusieurs types courants :
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

    // Retourne true si l'un des champs assign�s est en cours d'�dition (focus)
    bool IsAnyFieldEditing()
    {
        if (IsFieldEditing(mass)) return true;
        if (IsFieldEditing(speed)) return true;
        if (IsFieldEditing(radius)) return true;
        if (IsFieldEditing(distanceSoleil)) return true;
        if (IsFieldEditing(obj_name)) return true;
        return false;
    }

    // V�rifie un GameObject pour TMP_InputField ou InputField et teste la propri�t� isFocused
    bool IsFieldEditing(GameObject field)
    {
        if (field == null) return false;

        var tmpInput = field.GetComponent<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        var uiInput = field.GetComponent<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        // Cas o� le composant InputField est sur un enfant (ex: structure TMP Input Field)
        tmpInput = field.GetComponentInChildren<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        uiInput = field.GetComponentInChildren<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        return false;
    }

    // Lie les callbacks OnEndEdit aux champs (d�tache d'abord les anciens listeners)
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
        
        // distanceSoleil
        distanceSoleilTmp = GetTMPInput(distanceSoleil);
        distanceSoleilUi = GetLegacyInput(distanceSoleil);
        if (distanceSoleilTmp != null)
        {
            distanceSoleilListener = (s) => OnDistanceSoleilEndEdit(s);
            distanceSoleilTmp.onEndEdit.AddListener(distanceSoleilListener);
        }
        else if (distanceSoleilUi != null)
        {
            distanceSoleilListener = (s) => OnDistanceSoleilEndEdit(s);
            distanceSoleilUi.onEndEdit.AddListener(distanceSoleilListener);
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
        
        if (distanceSoleilTmp != null && distanceSoleilListener != null) distanceSoleilTmp.onEndEdit.RemoveListener(distanceSoleilListener);
        if (distanceSoleilUi != null && distanceSoleilListener != null) distanceSoleilUi.onEndEdit.RemoveListener(distanceSoleilListener);
        distanceSoleilTmp = null; distanceSoleilUi = null; distanceSoleilListener = null;
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

    // Callbacks de fin d'�dition � valident et appliquent si valide, sinon restaurent l'affichage
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

    void OnDistanceSoleilEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
        {
            props.distanceSoleil = v;
        }
        else
        {
            SetText(distanceSoleil, props.distanceSoleil.ToString("G"));
        }

        updateUIVisibility();
    }

    void OnNameEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        // Accepte toute cha�ne non nulle ; si vous voulez interdire vide, changez la condition
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

    // Essaie plusieurs cultures pour �tre tol�rant (ex : virgule ou point)
    bool TryParseFloatFlexible(string s, out float result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0f;
            return false;
        }

        // Premi�re passe: culture courante (utile pour les utilisateurs fr-FR avec virgule)
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result))
            return true;

        // Deuxi�me passe: invariant (point)
        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            return true;

        // Troisi�me passe: remplace virgule par point (au cas o�)
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
