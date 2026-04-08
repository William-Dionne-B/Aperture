using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    private GameObject selection;
    private GameObject lastSelection;
    private Vector3 cameraLocalPositionWhenAttached;
    private Quaternion cameraLocalRotationWhenAttached;
    private bool cameraIsAttachedToSelection = false;

    public GameObject MainCamera;
    public GameObject InfoUI;
    public GameObject SelectionViewFrame;
    public GameObject ListObjet;
    public GameObject CameraFocusButton;

    public GameObject speed;
    public GameObject mass;
    public GameObject radius;
    public GameObject obj_name;
    public GameObject dist_etoile;

    public Camera SelectionCamera;
    public RenderTexture SelectionRenderTexture;

    private float initialYOffset = 95f;

    private RawImage selectionRawImage;

    public float cameraPadding = 1.5f;

    TMP_InputField massTmp; InputField massUi; UnityAction<string> massListener;
    TMP_InputField speedTmp; InputField speedUi; UnityAction<string> speedListener;
    TMP_InputField radiusTmp; InputField radiusUi; UnityAction<string> radiusListener;
    TMP_InputField nameTmp; InputField nameUi; UnityAction<string> nameListener;

    private ScrollRect listScrollRect;
    private Transform listContent;
    private GameObject buttonPrefab;
    private Dictionary<GameObject, GameObject> objectToButtonMap = new Dictionary<GameObject, GameObject>();
    private List<ObjectProperties> lastFrameObjects = new List<ObjectProperties>();

    // --- Preview layer logic ---
    private const int SelectionLayer = 31; // couche temporaire utilisée pour la prévisualisation
    private int SelectionLayerMask => (1 << SelectionLayer);
    private Dictionary<Transform, int> savedLayers = new Dictionary<Transform, int>();

    void Start()
    {
        if (SelectionRenderTexture == null)
        {
            SelectionRenderTexture = new RenderTexture(1024, 1024, 24);
        }

        if (SelectionCamera != null)
        {
            Destroy(SelectionCamera.gameObject);
        }

        GameObject camObj = new GameObject("SelectionCamera");
        SelectionCamera = camObj.AddComponent<Camera>();

        SelectionCamera.targetTexture = SelectionRenderTexture;
        SelectionCamera.usePhysicalProperties = true;
        SelectionCamera.nearClipPlane = 0.01f;
        SelectionCamera.farClipPlane = 10000f;
        SelectionCamera.clearFlags = CameraClearFlags.Skybox;  // skybox visible
        SelectionCamera.fieldOfView = 60f;
        SelectionCamera.cullingMask = SelectionLayerMask;     // ne rendre que la couche de sélection

        if (SelectionViewFrame != null)
        {
            selectionRawImage = SelectionViewFrame.GetComponent<RawImage>();
            if (selectionRawImage != null)
            {
                selectionRawImage.texture = SelectionRenderTexture;
            }
        }

        InitializeListUI();
        InitializeCameraFocusButton();
        updateUIVisibility();
    }

    void InitializeCameraFocusButton()
    {
        if (CameraFocusButton == null) return;

        Button button = CameraFocusButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(FocusMainCameraOnSelection);
        }
    }

    void FocusMainCameraOnSelection()
    {
        if (selection == null || MainCamera == null) return;

        DetachCameraFromSelection();

        var renderer = selection.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = size * cameraPadding;

        Vector3 direction = new Vector3(0f, 0f, -1f);

        MainCamera.transform.position = center + direction * distance;
        MainCamera.transform.LookAt(center);

        AttachCameraToSelection();
    }

    void AttachCameraToSelection()
    {
        if (selection == null || MainCamera == null) return;

        MainCamera.transform.SetParent(selection.transform);
        cameraLocalPositionWhenAttached = MainCamera.transform.localPosition;
        cameraLocalRotationWhenAttached = MainCamera.transform.localRotation;
        cameraIsAttachedToSelection = true;
    }

    void DetachCameraFromSelection()
    {
        if (MainCamera == null) return;

        if (MainCamera.transform.parent != null)
        {
            MainCamera.transform.SetParent(null);
        }
        cameraIsAttachedToSelection = false;
    }

    void CheckForMovementInput()
    {
        if (!cameraIsAttachedToSelection) return;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            DetachCameraFromSelection();
        }
    }

    void InitializeListUI()
    {
        if (ListObjet == null) return;

        listScrollRect = ListObjet.GetComponent<ScrollRect>();
        if (listScrollRect == null)
        {
            listScrollRect = ListObjet.AddComponent<ScrollRect>();
        }

        // Récupère ou crée le contenu du ScrollView
        listContent = listScrollRect.content;
        if (listContent == null)
        {
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(ListObjet.transform, false);
            listContent = contentObj.transform;
            listScrollRect.content = listContent.GetComponent<RectTransform>();

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            if (contentRect == null) contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Crée le préfab de bouton
        CreateButtonPrefab();
    }

    void CreateButtonPrefab()
    {
        buttonPrefab = new GameObject("ObjectButton");
        buttonPrefab.SetActive(false);

        RectTransform buttonRect = buttonPrefab.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 50);

        Image buttonImage = buttonPrefab.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f);

        Button buttonComponent = buttonPrefab.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;

        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
        colors.selectedColor = new Color(0.4f, 0.4f, 0.4f);
        buttonComponent.colors = colors;

        LayoutElement layoutElement = buttonPrefab.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;
        layoutElement.preferredWidth = 200;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonPrefab.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "Object";
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 16;
    }

    void Update()
    {
        if (MainCamera == null) return;

        var click = MainCamera.GetComponent<ClickDetection>();
        if (click == null) return;

        var selected = click.selectedObject;

        // conserve l'ancienne sélection pour restaurer ses couches
        GameObject oldSelection = selection;

        if (selection != selected)
        {
            DetachCameraFromSelection();
            selection = selected;
            lastSelection = selection;
            BindFieldListeners();

            // restaure l'ancienne sélection puis applique la couche de preview à la nouvelle sélection
            RestoreSelectionLayers();
            ApplySelectionLayer(selection);
        }

        UpdateSelectionCamera();

        // Vérifie les entrées de mouvement
        CheckForMovementInput();

        // Met à jour la liste des objets chaque frame
        UpdateObjectList();

        if (IsAnyFieldEditing())
            return;

        updateUIVisibility();
    }

    void UpdateObjectList()
    {
        if (listContent == null) return;

        // Récupère tous les objets avec ObjectProperties dans la scène
        ObjectProperties[] allObjectsInScene = FindObjectsOfType<ObjectProperties>();

        // Vérifie si la liste a changé
        bool listChanged = allObjectsInScene.Length != lastFrameObjects.Count;
        if (!listChanged)
        {
            for (int i = 0; i < allObjectsInScene.Length; i++)
            {
                if (allObjectsInScene[i] != lastFrameObjects[i])
                {
                    listChanged = true;
                    break;
                }
            }
        }

        // Si la liste a changé, reconstruit les boutons
        if (listChanged)
        {
            // Efface les anciens boutons
            foreach (Transform child in listContent)
            {
                Destroy(child.gameObject);
            }
            objectToButtonMap.Clear();

            // Crée les nouveaux boutons
            float yOffset = initialYOffset;
            foreach (ObjectProperties objProps in allObjectsInScene)
            {
                if (objProps == null) continue;

                GameObject buttonInstance = Instantiate(buttonPrefab, listContent);
                buttonInstance.SetActive(true);

                RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
                buttonRect.anchoredPosition = new Vector2(0, yOffset);
                yOffset -= 55; // 50 de hauteur + 5 de spacing

                TextMeshProUGUI textComponent = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = objProps.objectName;
                }

                Button buttonComponent = buttonInstance.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    GameObject objToSelect = objProps.gameObject;
                    if (objProps.transform.parent != null)
                    {
                        objToSelect = objProps.transform.parent.gameObject;
                    }

                    buttonComponent.onClick.AddListener(() => SelectObject(objToSelect));
                }

                objectToButtonMap[objProps.gameObject] = buttonInstance;
            }

            // Met à jour la liste de référence
            lastFrameObjects.Clear();
            foreach (ObjectProperties objProps in allObjectsInScene)
            {
                lastFrameObjects.Add(objProps);
            }
        }

        // Met à jour les textes des boutons existants et l'apparence de sélection
        foreach (var kvp in objectToButtonMap)
        {
            ObjectProperties objProps = kvp.Key.GetComponent<ObjectProperties>();
            if (objProps == null && kvp.Key.transform.parent != null)
            {
                objProps = kvp.Key.transform.parent.GetComponent<ObjectProperties>();
            }

            GameObject buttonGO = kvp.Value;
            TextMeshProUGUI textComponent = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null && objProps != null)
            {
                textComponent.text = objProps.objectName;
            }

            // Highlight le bouton si l'objet est sélectionné
            Button buttonComponent = buttonGO.GetComponent<Button>();
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonComponent != null && buttonImage != null)
            {
                bool isSelected = (objProps != null && (objProps.gameObject == selection || 
                    (objProps.transform.parent != null && objProps.transform.parent.gameObject == selection)));
                
                if (isSelected)
                {
                    buttonImage.color = new Color(0.4f, 0.4f, 0.4f);
                }
                else
                {
                    buttonImage.color = new Color(0.2f, 0.2f, 0.2f);
                }
            }
        }
    }

    void SelectObject(GameObject obj)
    {
        var clickDetection = MainCamera?.GetComponent<ClickDetection>();
        if (clickDetection != null)
        {
            clickDetection.selectedObject = obj;
        }
    }

    void UpdateSelectionCamera()
    {
        if (SelectionCamera == null || selection == null) return;

        var renderer = selection.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;

        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        float distance = size * cameraPadding;

        Vector3 direction = new Vector3(0f, 0f, -1f);

        SelectionCamera.transform.position = center + direction * distance;
        SelectionCamera.transform.LookAt(center);
    }

    void updateUIVisibility()
    {
        if (InfoUI == null) return;

        if (selection != null)
        {
            InfoUI.SetActive(true);

            var props = selection.GetComponent<ObjectProperties>();
            if (props != null)
            {
                SetText(mass, props.mass.ToString("G"));
                SetText(speed, props.speedMagnitude.ToString("G"));
                SetText(radius, props.radius.ToString("G"));
                SetText(obj_name, props.objectName);

                if (props.EtoileParent != null)
                {
                    Vector3 posEtoile = props.EtoileParent.transform.position;
                    Vector3 posBody = selection.transform.position;
                    float dist = Vector3.Distance(posEtoile, posBody);
                    props.distanceToEtoile = dist;
                    SetText(dist_etoile, dist.ToString("G"));
                }
                else
                {
                    if (props.distanceToEtoile >= 0f)
                        SetText(dist_etoile, props.distanceToEtoile.ToString("G"));
                    else
                        SetText(dist_etoile, "N/A");
                }
            }
            else
            {
                SetText(mass, "");
                SetText(speed, "");
                SetText(radius, "");
                SetText(dist_etoile, "");
            }
        }
        else
        {
            InfoUI.SetActive(false);
        }
    }

    void SetText(GameObject field, string value)
    {
        if (field == null) return;

        var tmpInput = field.GetComponent<TMP_InputField>();
        if (tmpInput != null) { tmpInput.text = value; return; }

        var uiInput = field.GetComponent<InputField>();
        if (uiInput != null) { uiInput.text = value; return; }

        var tmp = field.GetComponent<TMP_Text>();
        if (tmp != null) { tmp.text = value; return; }

        var uiText = field.GetComponent<Text>();
        if (uiText != null) { uiText.text = value; return; }

        tmp = field.GetComponentInChildren<TMP_Text>();
        if (tmp != null) { tmp.text = value; return; }

        uiText = field.GetComponentInChildren<Text>();
        if (uiText != null) { uiText.text = value; return; }
    }

    bool IsAnyFieldEditing()
    {
        if (IsFieldEditing(mass)) return true;
        if (IsFieldEditing(speed)) return true;
        if (IsFieldEditing(radius)) return true;
        if (IsFieldEditing(obj_name)) return true;
        if (IsFieldEditing(dist_etoile)) return true;
        return false;
    }

    bool IsFieldEditing(GameObject field)
    {
        if (field == null) return false;

        var tmpInput = field.GetComponent<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        var uiInput = field.GetComponent<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        tmpInput = field.GetComponentInChildren<TMP_InputField>();
        if (tmpInput != null) return tmpInput.isFocused;

        uiInput = field.GetComponentInChildren<InputField>();
        if (uiInput != null) return uiInput.isFocused;

        return false;
    }

    void BindFieldListeners()
    {
        UnbindAllFieldListeners();

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

    void OnMassEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
            props.mass = v;
        else
            SetText(mass, props.mass.ToString("G"));

        updateUIVisibility();
    }

    void OnSpeedEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
            props.speedMagnitude = v;
        else
            SetText(speed, props.speedMagnitude.ToString("G"));

        updateUIVisibility();
    }

    void OnRadiusEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (TryParseFloatFlexible(input, out float v))
            props.radius = v;
        else
            SetText(radius, props.radius.ToString("G"));

        updateUIVisibility();
    }

    void OnNameEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

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

    bool TryParseFloatFlexible(string s, out float result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = 0f;
            return false;
        }

        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result))
            return true;

        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            return true;

        var replaced = s.Replace(',', '.');
        if (float.TryParse(replaced, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            return true;

        result = 0f;
        return false;
    }

    void OnDestroy()
    {
        UnbindAllFieldListeners();
        DetachCameraFromSelection();
        RestoreSelectionLayers();
        if (buttonPrefab != null)
        {
            Destroy(buttonPrefab);
        }
    }

    // --- Layer helper methods ---
    private void ApplySelectionLayer(GameObject root)
    {
        if (root == null) return;
        savedLayers.Clear();

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!savedLayers.ContainsKey(t))
                savedLayers[t] = t.gameObject.layer;
            t.gameObject.layer = SelectionLayer;
        }
    }

    private void RestoreSelectionLayers()
    {
        if (savedLayers == null || savedLayers.Count == 0) return;

        foreach (var kv in savedLayers)
        {
            if (kv.Key != null)
                kv.Key.gameObject.layer = kv.Value;
        }
        savedLayers.Clear();
    }
}