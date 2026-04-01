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
    
    public GameObject surface_gravity;

    public GameObject temperature;

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
                // 1. CONVERSION POUR L'AFFICHAGE
                float vraieMasse = props.mass * props.unityToKgScale;
                float vraiRayon = props.radius * props.radiusToMetersScale;
                
                // Met à jour les champs UI avec les VRAIES valeurs
                SetText(mass, FormaterScientifiqueTMP(vraieMasse));
                SetText(radius, FormaterScientifiqueTMP(vraiRayon));
                
                // (La vitesse reste pareille pour l'instant, à moins que tu aies aussi une échelle pour elle !)
                SetText(speed, props.speedMagnitude.ToString("F2")); 
                SetText(obj_name, props.objectName);

                // Gravité de surface (inchangé, ton code gère déjà bien ça)
                float grav = props.gravityMagnitude;
                float gravEnG = grav / 9.81f; 
                if (surface_gravity != null) SetText(surface_gravity, $"{grav:0.##} ({gravEnG:0.##} g)");

                // 2. CONVERSION DE LA DISTANCE À L'ÉTOILE
                if (props.EtoileParent != null)
                {
                    try
                    {
                        float distUnity = Vector3.Distance(props.EtoileParent.transform.position, selection.transform.position);
                        
                        float vraieDistance = distUnity * props.distanceToMetersScale;
                        SetText(dist_etoile, FormaterScientifiqueTMP(vraieDistance));
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Impossible de calculer la distance à l'étoile : {ex.Message}");
                        SetText(dist_etoile, "N/A");
                    }
                }
                
                else
                {
                    if (props.distanceToEtoile >= 0f)
                    {
                        float vraieDistance = props.distanceToEtoile * props.distanceToMetersScale;
                        SetText(dist_etoile, vraieDistance.ToString("G"));
                    }
                    else
                    {
                        SetText(dist_etoile, "N/A");
                    }
                }
                
                if (temperature != null)
                {
                    if (props.temperatureMagnitude > 0f)
                    {
                        float tempK = props.temperatureMagnitude;
                        float tempC = tempK - 273.15f; // Conversion K -> °C
                        
                        SetText(temperature, $"{tempK:F1} ({tempC:F1} °C)");
                    }
                    else
                    {
                        SetText(temperature, "N/A");
                    }
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

        if (LireEntreeUtilisateur(input, out float vraieMasseTapee))
        {
            // On divise par ton échelle pour redonner la petite valeur à Unity (ex: 3.003)
            props.mass = vraieMasseTapee / props.unityToKgScale;
        }
        else
        {
            // Si le joueur a tapé n'importe quoi, on restaure l'affichage normal
            float vraieMasse = props.mass * props.unityToKgScale;
            SetText(mass, FormaterScientifiqueTMP(vraieMasse) + " kg");
        }
        updateUIVisibility();
    }

    void OnSpeedEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (LireEntreeUtilisateur(input, out float v))
        {
            props.speedMagnitude = v;
        }
        else
        {
            SetText(speed, props.speedMagnitude.ToString("F2") + " m/s");
        }
        updateUIVisibility();
    }

    void OnRadiusEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (LireEntreeUtilisateur(input, out float vraiRayonEnMetres))
        {
            props.radius = vraiRayonEnMetres / props.radiusToMetersScale;
        }
        else
        {
            float vraiRayon = props.radius * props.radiusToMetersScale;
            SetText(radius, FormaterDistance(vraiRayon)); // Ou FormaterScientifiqueTMP selon ce que tu avais choisi !
        }
        updateUIVisibility();
    }

    void OnNameEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        // Accepte toute chaîne non nulle ; si vous voulez interdire vide, changez la condition
        if (!string.IsNullOrWhiteSpace(input))
        {
            props.objectName = input;

            selection.name = input;
        }
        else
        {
            SetText(obj_name, props.objectName);
        }
        updateUIVisibility();
    }

    // Essaie plusieurs cultures pour être tolérant (ex : virgule ou point)
    bool LireEntreeUtilisateur(string input, out float resultatFinal)
    {
        resultatFinal = 0f;
        if (string.IsNullOrWhiteSpace(input)) return false;

        float multiplicateur = 1f;

        // 1. Détecter les unités de distance (si le joueur a laissé "km" ou "M km")
        if (input.Contains("M km")) multiplicateur = 1e9f; // Millions de km -> Mètres
        else if (input.Contains("km")) multiplicateur = 1e3f; // km -> Mètres

        // 2. Enlever toutes les lettres et unités pour ne garder que les nombres
        string textPropre = input.Replace(" kg", "")
            .Replace(" M km", "")
            .Replace(" km", "")
            .Replace(" m", "")
            .Replace(" m/s", "");

        // 3. Convertir les balises TextMeshPro (× 10<sup>24</sup>) en format C# (E24)
        textPropre = textPropre.Replace(" × 10<sup>", "E")
            .Replace(" x 10<sup>", "E")
            .Replace("</sup>", "");

        // 4. Nettoyer les espaces et forcer le point décimal (pour ignorer les virgules)
        textPropre = textPropre.Replace(" ", "").Replace(",", ".");

        // 5. Convertir le texte final (ex: "5.97E24" ou "6371") en vrai chiffre
        if (float.TryParse(textPropre, NumberStyles.Float, CultureInfo.InvariantCulture, out float valeurBrute))
        {
            // On applique le multiplicateur (ex: si c'était 7000 km, ça devient 7 000 000 mètres)
            resultatFinal = valeurBrute * multiplicateur;
            return true;
        }

        return false;
    }
    
    string FormaterDistance(float distanceEnMetres)
    {
        float distanceEnKm = distanceEnMetres / 1000f;

        if (distanceEnKm >= 1000000f)
        {
            float distanceEnMillions = distanceEnKm / 1000000f;
            return $"{distanceEnMillions:F2} M";
        }
        else if (distanceEnKm >= 1f)
        {
            return $"{distanceEnKm:F1}";
        }
        else
        {
            return $"{distanceEnMetres:F0} m";
        }
    }
    
    string FormaterScientifiqueTMP(float valeur)
    {
        // Si la valeur est 0, on affiche juste 0
        if (valeur == 0f) return "0";

        // 1. On récupère le format brut de C# (ex: "5.97E+24")
        string formatStandard = valeur.ToString("E2");

        // 2. On coupe le texte en deux parties au niveau du 'E'
        string[] parties = formatStandard.Split('E');

        if (parties.Length == 2)
        {
            string baseNum = parties[0]; // ex: "5.97"
            
            // int.Parse enlève automatiquement le "+" et les zéros inutiles (ex: "+24" devient 24)
            int exposant = int.Parse(parties[1]); 

            // 3. On reconstruit le texte avec le symbole "×" et la balise <sup> de TextMeshPro
            return $"{baseNum.Replace('.', ',')} × 10<sup>{exposant}</sup>";
        }

        // Sécurité au cas où
        return formatStandard;
    }

    void OnDestroy()
    {
        UnbindAllFieldListeners();
    }
}