using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    private GameObject selection;
    private GameObject lastSelection;

    // Verrouillage / anchors pour empêcher l'héritage de rotation
    private bool cameraLockedToSelection = false;
    private GameObject mainCameraAnchor;
    private Vector3 mainCameraOffset; // offset utilisé pour positionner l'anchor = cameraWorldPos - selectionPos
    private Quaternion mainCameraRotationWhenLocked;

    private bool selectionCameraLockedToSelection = false;
    private GameObject selectionCameraAnchor;
    private Vector3 selectionCameraOffset;
    private Quaternion selectionCameraRotationWhenLocked;

    public GameObject MainCamera;
    public GameObject InfoUI;

    public GameObject SelectionViewFrame;
    public GameObject ListObjet; // Contient TMP_Dropdown ou Dropdown
    public GameObject CameraFocusButton;

    public Camera SelectionCamera;
    public RenderTexture SelectionRenderTexture;

    [Header("Champs UI Textes")]
    public GameObject speed;
    public GameObject mass;
    public GameObject radius;
    public GameObject obj_name;
    public GameObject dist_etoile;
    public GameObject periode;
    public GameObject density;
    public GameObject surface_gravity;
    public GameObject temperature;

    [Header("Boutons ×10 / ÷10")]
    public Button massMultiply10Button;
    public Button massDivide10Button;
    public Button speedMultiply10Button;
    public Button speedDivide10Button;
    public Button radiusMultiply10Button;
    public Button radiusDivide10Button;

    private float initialYOffset = 95f;
    private RawImage selectionRawImage;
    public float cameraPadding = 1.5f;

    [Header("Verrou caméra - déplacement")]
    public float lockedMoveSpeed = 5f;         // vitesse WASD pour la caméra principale quand verrouillée
    public float previewLockedMoveSpeed = 2f; // (conservé si besoin mais WASD n'affecte plus la preview)

    [Header("Buffer de sélection")]
    public float selectionBufferDuration = 0.15f; // délai pendant lequel la sélection doit être stable
    private GameObject pendingSelection;
    private float pendingSelectionElapsed;
    private bool pendingSelectionActive = false;

    // Références liées aux listeners
    TMP_InputField massTmp; InputField massUi; UnityAction<string> massListener;
    TMP_InputField speedTmp; InputField speedUi; UnityAction<string> speedListener;
    TMP_InputField radiusTmp; InputField radiusUi; UnityAction<string> radiusListener;
    TMP_InputField nameTmp; InputField nameUi; UnityAction<string> nameListener;

    // Dropdown-based list UI
    private TMP_Dropdown tmpDropdown;
    private Dropdown legacyDropdown;
    private List<GameObject> dropdownObjects = new List<GameObject>();
    private List<ObjectProperties> lastFrameObjects = new List<ObjectProperties>();

    // --- Preview layer logic ---
    private const int SelectionLayer = 31; // couche temporaire utilisée pour la prévisualisation
    private int SelectionLayerMask => (1 << SelectionLayer);
    private Dictionary<Transform, int> savedLayers = new Dictionary<Transform, int>();

    void Start()
    {
        if (SelectionRenderTexture == null) SelectionRenderTexture = new RenderTexture(1024, 1024, 24);
        if (SelectionCamera != null) Destroy(SelectionCamera.gameObject);

        GameObject camObj = new GameObject("SelectionCamera");
        SelectionCamera = camObj.AddComponent<Camera>();
        SelectionCamera.targetTexture = SelectionRenderTexture;
        SelectionCamera.usePhysicalProperties = true;
        SelectionCamera.nearClipPlane = 0.01f;
        SelectionCamera.farClipPlane = 10000f;
        SelectionCamera.clearFlags = CameraClearFlags.Skybox;
        SelectionCamera.fieldOfView = 60f;
        SelectionCamera.cullingMask = SelectionLayerMask;

        if (SelectionViewFrame != null)
        {
            selectionRawImage = SelectionViewFrame.GetComponent<RawImage>();
            if (selectionRawImage != null) selectionRawImage.texture = SelectionRenderTexture;
        }

        InitializeListUI();
        InitializeCameraFocusButton();
        InitializeMultiplierButtons();
        updateUIVisibility();
    }

    void InitializeCameraFocusButton()
    {
        if (CameraFocusButton == null) return;
        Button button = CameraFocusButton.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(FocusMainCameraOnSelection);
    }

    void InitializeMultiplierButtons()
    {
        if (massMultiply10Button != null) massMultiply10Button.onClick.AddListener(OnMassMultiply10);
        if (massDivide10Button != null) massDivide10Button.onClick.AddListener(OnMassDivide10);

        if (speedMultiply10Button != null) speedMultiply10Button.onClick.AddListener(OnSpeedMultiply10);
        if (speedDivide10Button != null) speedDivide10Button.onClick.AddListener(OnSpeedDivide10);

        if (radiusMultiply10Button != null) radiusMultiply10Button.onClick.AddListener(OnRadiusMultiply10);
        if (radiusDivide10Button != null) radiusDivide10Button.onClick.AddListener(OnRadiusDivide10);
    }

    void OnMassMultiply10()   => MultiplyMass(10f);
    void OnMassDivide10()     => MultiplyMass(0.1f);
    void OnSpeedMultiply10()  => MultiplySpeed(10f);
    void OnSpeedDivide10()    => MultiplySpeed(0.1f);
    void OnRadiusMultiply10() => MultiplyRadius(10f);
    void OnRadiusDivide10()   => MultiplyRadius(0.1f);

    void MultiplyMass(float factor)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;
        props.Mass *= factor;
        updateUIVisibility();
    }

    void MultiplySpeed(float factor)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;
        props.speedMagnitude *= factor;
        updateUIVisibility();
    }

    void MultiplyRadius(float factor)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;
        props.radius *= factor;
        updateUIVisibility();
    }

    void FocusMainCameraOnSelection()
    {
        if (selection == null || MainCamera == null) return;

        var renderer = selection.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = size * cameraPadding;

        MainCamera.transform.position = center + new Vector3(0f, 0f, -1f) * distance;
        MainCamera.transform.LookAt(center);

        AttachCameraToSelection();
    }

    // Création utilitaire d'un anchor
    GameObject CreateAnchor(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    void AttachCameraToSelection()
    {
        if (selection == null || MainCamera == null) return;

        // MAIN CAMERA anchor only — la preview reste indépendante
        if (mainCameraAnchor == null)
        {
            mainCameraAnchor = CreateAnchor("MainCameraAnchor");
        }

        Vector3 camWorldPos = MainCamera.transform.position;
        Quaternion camWorldRot = MainCamera.transform.rotation;

        mainCameraAnchor.transform.position = camWorldPos;
        mainCameraAnchor.transform.rotation = camWorldRot;
        mainCameraAnchor.transform.SetParent(selection.transform, true);

        MainCamera.transform.SetParent(mainCameraAnchor.transform, true);

        mainCameraOffset = MainCamera.transform.position - selection.transform.position;
        mainCameraRotationWhenLocked = MainCamera.transform.rotation;
        cameraLockedToSelection = true;

        // s'assurer que la preview n'est pas parentée (doit rester indépendante)
        if (selectionCameraAnchor != null)
        {
            if (SelectionCamera != null) SelectionCamera.transform.SetParent(null, true);
            Destroy(selectionCameraAnchor);
            selectionCameraAnchor = null;
            selectionCameraLockedToSelection = false;
        }
    }

    void DetachCameraFromSelection()
    {

        if (MainCamera != null) MainCamera.transform.SetParent(null, true);

        if (mainCameraAnchor != null)
        {
            Destroy(mainCameraAnchor);
            mainCameraAnchor = null;
        }

        if (SelectionCamera != null && SelectionCamera.transform.parent != null)
        {
            SelectionCamera.transform.SetParent(null, true);
        }
        if (selectionCameraAnchor != null)
        {
            Destroy(selectionCameraAnchor);
            selectionCameraAnchor = null;
        }
        selectionCameraLockedToSelection = false;

        cameraLockedToSelection = false;
    }
    void ReparentAnchorsToNewSelection()
    {
        if (selection == null) return;

        if (mainCameraAnchor != null)
        {
            Vector3 camWorldPos = MainCamera.transform.position;
            Quaternion camWorldRot = MainCamera.transform.rotation;

            mainCameraAnchor.transform.SetParent(selection.transform, true);

            mainCameraOffset = camWorldPos - selection.transform.position;
            mainCameraRotationWhenLocked = camWorldRot;

            mainCameraAnchor.transform.position = camWorldPos;
            mainCameraAnchor.transform.rotation = camWorldRot;
        }
    }

    void InitializeListUI()
    {
        if (ListObjet == null) return;

        tmpDropdown = ListObjet.GetComponent<TMP_Dropdown>() ?? ListObjet.GetComponentInChildren<TMP_Dropdown>();
        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.RemoveAllListeners();
            tmpDropdown.onValueChanged.AddListener(OnTMPDropdownValueChanged);
            tmpDropdown.ClearOptions();
            return;
        }

        legacyDropdown = ListObjet.GetComponent<Dropdown>() ?? ListObjet.GetComponentInChildren<Dropdown>();
        if (legacyDropdown != null)
        {
            legacyDropdown.onValueChanged.RemoveAllListeners();
            legacyDropdown.onValueChanged.AddListener(OnLegacyDropdownValueChanged);
            legacyDropdown.options.Clear();
            return;
        }

        Debug.LogWarning("[ObjectManager] `ListObjet` ne contient ni `TMP_Dropdown` ni `Dropdown`. Veuillez ajouter un dropdown dans l'inspecteur.");
    }

    void Update()
    {
        if (MainCamera == null) return;

        var click = MainCamera.GetComponent<ClickDetection>();
        if (click == null) return;

        var clicked = click.selectedObject;

        // --- BUFFER / DEBOUNCE: on stocke la sélection candidate et on applique seulement si stable ---
        if (clicked != pendingSelection)
        {
            pendingSelection = clicked;
            pendingSelectionElapsed = 0f;
            pendingSelectionActive = true;
        }
        else if (pendingSelectionActive)
        {
            pendingSelectionElapsed += Time.deltaTime;
            if (pendingSelectionElapsed >= selectionBufferDuration)
            {
                // si la sélection candidate est différente de l'actuelle, on l'applique
                if (selection != pendingSelection)
                {
                    // si verrou présent -> détache immédiatement (ne réapplique pas automatiquement)
                    if (cameraLockedToSelection || selectionCameraLockedToSelection)
                    {
                        DetachCameraFromSelection();
                    }

                    selection = pendingSelection;
                    lastSelection = selection;
                    BindFieldListeners();

                    RestoreSelectionLayers();
                    ApplySelectionLayer(selection);

                    // mettre à jour la preview APRÈS le détachement / changement de sélection
                    UpdateSelectionCamera();

                    // mettre à jour la dropdown pour refléter la sélection confirmée
                    UpdateObjectList(); // s'assurer que dropdownObjects est à jour
                    int idx = dropdownObjects.IndexOf(selection);
                    if (tmpDropdown != null)
                    {
                        tmpDropdown.onValueChanged.RemoveListener(OnTMPDropdownValueChanged);
                        if (idx >= 0) tmpDropdown.value = idx;
                        else tmpDropdown.value = (tmpDropdown.options.Count > 0 ? 0 : tmpDropdown.value);
                        tmpDropdown.onValueChanged.AddListener(OnTMPDropdownValueChanged);
                    }
                    else if (legacyDropdown != null)
                    {
                        legacyDropdown.onValueChanged.RemoveListener(OnLegacyDropdownValueChanged);
                        if (idx >= 0) legacyDropdown.value = idx;
                        else legacyDropdown.value = (legacyDropdown.options.Count > 0 ? 0 : legacyDropdown.value);
                        legacyDropdown.onValueChanged.AddListener(OnLegacyDropdownValueChanged);
                    }
                }

                pendingSelectionActive = false;
            }
        }

        // Toggle lock avec la touche L (si un objet est sélectionné)
        if (Input.GetKeyDown(KeyCode.L) && selection != null)
        {
            if (cameraLockedToSelection || selectionCameraLockedToSelection)
            {
                DetachCameraFromSelection();
            }
            else
            {
                AttachCameraToSelection();
            }
        }

        UpdateObjectList();

        // Déplacement WASD relatif à l'orientation de la caméra principale (en mode verrou)
        if (cameraLockedToSelection && selection != null && MainCamera != null && mainCameraAnchor != null)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            if (Mathf.Abs(h) > 0.0001f || Mathf.Abs(v) > 0.0001f)
            {
                // mouvement relatif à l'orientation de la caméra (strafe et forward dans le plan XZ)
                Vector3 right = MainCamera.transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(MainCamera.transform.forward, Vector3.up).normalized;
                Vector3 move = (right * h + forward * v) * lockedMoveSpeed * Time.deltaTime;

                mainCameraOffset += move;
            }

            // positionner l'anchor en position monde = selection.position + offset (conserve la position monde de la caméra)
            mainCameraAnchor.transform.position = selection.transform.position + mainCameraOffset;
            // maintenir la rotation fixe enregistrée (empêche la sélection d'affecter la rotation)
            mainCameraAnchor.transform.rotation = mainCameraRotationWhenLocked;
        }

        // IMPORTANT: ne plus appliquer WASD à la caméra de preview — la preview ne bouge pas via WASD
        if (selectionCameraLockedToSelection && selection != null && SelectionCamera != null && selectionCameraAnchor != null)
        {
            // on conserve l'anchor position/rotation (pas de modification par WASD)
            selectionCameraAnchor.transform.position = selection.transform.position + selectionCameraOffset;
            selectionCameraAnchor.transform.rotation = selectionCameraRotationWhenLocked;
        }

        // Si la caméra de preview n'est pas en mode locked, comportement standard
        UpdateSelectionCamera();

        if (IsAnyFieldEditing()) return;
        updateUIVisibility();
    }

    void UpdateObjectList()
    {
        ObjectProperties[] allObjectsInScene = FindObjectsOfType<ObjectProperties>();
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

        if (listChanged)
        {
            dropdownObjects.Clear();

            List<string> optionNames = new List<string>();
            foreach (ObjectProperties objProps in allObjectsInScene)
            {
                if (objProps == null) continue;
                GameObject objToAdd = objProps.gameObject;
                if (objProps.transform.parent != null) objToAdd = objProps.transform.parent.gameObject;
                dropdownObjects.Add(objToAdd);
                optionNames.Add(objProps.objectName);
            }

            if (tmpDropdown != null)
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(optionNames);
            }
            else if (legacyDropdown != null)
            {
                legacyDropdown.options.Clear();
                foreach (var name in optionNames) legacyDropdown.options.Add(new Dropdown.OptionData(name));
            }

            lastFrameObjects.Clear();
            foreach (ObjectProperties objProps in allObjectsInScene) lastFrameObjects.Add(objProps);

            if (selection != null)
            {
                int idx = dropdownObjects.IndexOf(selection);
                if (idx < 0)
                {
                    for (int i = 0; i < dropdownObjects.Count; i++)
                    {
                        if (dropdownObjects[i] == selection || (dropdownObjects[i].transform != null && dropdownObjects[i].transform == selection.transform.parent))
                        {
                            idx = i; break;
                        }
                    }
                }

                if (tmpDropdown != null && idx >= 0) tmpDropdown.value = idx;
                else if (legacyDropdown != null && idx >= 0) legacyDropdown.value = idx;
            }
            else
            {
                if (tmpDropdown != null && tmpDropdown.options.Count > 0) tmpDropdown.value = 0;
                if (legacyDropdown != null && legacyDropdown.options.Count > 0) legacyDropdown.value = 0;
            }
        }
    }

    void OnTMPDropdownValueChanged(int index)
    {
        if (index < 0 || index >= dropdownObjects.Count) return;
        SelectObject(dropdownObjects[index]);
    }

    void OnLegacyDropdownValueChanged(int index)
    {
        if (index < 0 || index >= dropdownObjects.Count) return;
        SelectObject(dropdownObjects[index]);
    }

    void SelectObject(GameObject obj)
    {
        var clickDetection = MainCamera?.GetComponent<ClickDetection>();
        if (clickDetection != null) clickDetection.selectedObject = obj;
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

        // Si verrouillée, la position est gérée dans Update() via l'anchor
        if (!selectionCameraLockedToSelection)
        {
            SelectionCamera.transform.position = center + new Vector3(0f, 0f, -1f) * distance;
            SelectionCamera.transform.LookAt(center);
        }
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
                float vraieMasse = props.Mass * props.unityToKgScale;
                float vraiRayon = props.radius * props.radiusToMetersScale;

                SetText(mass, FormaterScientifiqueTMP(vraieMasse) + " kg");
                SetText(radius, FormaterScientifiqueTMP(vraiRayon) + " m");
                SetText(speed, props.speedMagnitude.ToString("F2") + " km/s");
                SetText(obj_name, props.objectName);
                SetText(periode, props.periode > 0f ? props.periode.ToString("G") : "N/A");
                SetText(density, props.density.ToString("G"));

                float grav = props.gravityMagnitude;
                float gravEnG = grav / 9.81f;
                SetText(surface_gravity, $"{grav:0.##} m/s² ({gravEnG:0.##} g)");

                if (props.EtoileParent != null)
                {
                    float distUnity = Vector3.Distance(props.EtoileParent.transform.position, selection.transform.position);
                    float vraieDistance = distUnity * props.distanceToMetersScale;
                    float distanceAL = vraieDistance / 9.461e15f;
                    SetText(dist_etoile, $"{FormaterScientifiqueTMP(vraieDistance)} m ({FormaterScientifiqueTMP(distanceAL)} al)");
                }
                else SetText(dist_etoile, "N/A");

                if (temperature != null)
                {
                    if (props.temperatureMagnitude > 0f)
                    {
                        float tempK = props.temperatureMagnitude;
                        float tempC = tempK - 273.15f;
                        SetText(temperature, $"{tempK:F1} K ({tempC:F1} °C)");
                    }
                    else SetText(temperature, "N/A");
                }
            }
            else
            {
                SetText(mass, ""); SetText(speed, ""); SetText(radius, ""); SetText(dist_etoile, ""); SetText(periode, ""); SetText(density, "");
            }
        }
        else InfoUI.SetActive(false);
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
        return IsFieldEditing(mass) || IsFieldEditing(speed) || IsFieldEditing(radius) || IsFieldEditing(obj_name) || IsFieldEditing(dist_etoile);
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
        massTmp = GetTMPInput(mass); massUi = GetLegacyInput(mass);
        if (massTmp != null) { massListener = (s) => OnMassEndEdit(s); massTmp.onEndEdit.AddListener(massListener); }
        else if (massUi != null) { massListener = (s) => OnMassEndEdit(s); massUi.onEndEdit.AddListener(massListener); }

        speedTmp = GetTMPInput(speed); speedUi = GetLegacyInput(speed);
        if (speedTmp != null) { speedListener = (s) => OnSpeedEndEdit(s); speedTmp.onEndEdit.AddListener(speedListener); }
        else if (speedUi != null) { speedListener = (s) => OnSpeedEndEdit(s); speedUi.onEndEdit.AddListener(speedListener); }

        radiusTmp = GetTMPInput(radius); radiusUi = GetLegacyInput(radius);
        if (radiusTmp != null) { radiusListener = (s) => OnRadiusEndEdit(s); radiusTmp.onEndEdit.AddListener(radiusListener); }
        else if (radiusUi != null) { radiusListener = (s) => OnRadiusEndEdit(s); radiusUi.onEndEdit.AddListener(radiusListener); }

        nameTmp = GetTMPInput(obj_name); nameUi = GetLegacyInput(obj_name);
        if (nameTmp != null) { nameListener = (s) => OnNameEndEdit(s); nameTmp.onEndEdit.AddListener(nameListener); }
        else if (nameUi != null) { nameListener = (s) => OnNameEndEdit(s); nameUi.onEndEdit.AddListener(nameListener); }
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

    // --- CALLBACKS D'ÉDITION ---
    void OnMassEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (LireEntreeUtilisateur(input, out float vraieMasseTapee))
        {
            props.Mass = vraieMasseTapee / props.unityToKgScale;
        }
        else
        {
            float vraieMasse = props.Mass * props.unityToKgScale;
            SetText(mass, FormaterScientifiqueTMP(vraieMasse) + " kg");
        }
        updateUIVisibility();
    }

    void OnSpeedEndEdit(string input)
    {
        var props = selection?.GetComponent<ObjectProperties>();
        if (props == null) return;

        if (LireEntreeUtilisateur(input, out float v)) props.speedMagnitude = v;
        else SetText(speed, props.speedMagnitude.ToString("F2") + " km/s");

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
            SetText(radius, FormaterScientifiqueTMP(vraiRayon) + " m");
        }
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
        else SetText(obj_name, props.objectName);

        updateUIVisibility();
    }

    bool LireEntreeUtilisateur(string input, out float resultatFinal)
    {
        resultatFinal = 0f;
        if (string.IsNullOrWhiteSpace(input)) return false;

        float multiplicateur = 1f;

        if (input.Contains("M km")) multiplicateur = 1e9f;
        else if (input.Contains("km")) multiplicateur = 1e3f;

        string textPropre = input.Replace(" kg", "").Replace(" M km", "").Replace(" km", "").Replace(" m", "").Replace(" m/s", "").Replace(" km/s", "");
        textPropre = textPropre.Replace(" × 10<sup>", "E").Replace(" x 10<sup>", "E").Replace("</sup>", "");
        textPropre = textPropre.Replace(" ", "").Replace(",", ".");

        if (float.TryParse(textPropre, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float valeurBrute))
        {
            resultatFinal = valeurBrute * multiplicateur;
            return true;
        }
        return false;
    }

    string FormaterScientifiqueTMP(float valeur)
    {
        if (valeur == 0f) return "0";
        string formatStandard = valeur.ToString("E2");
        string[] parties = formatStandard.Split('E');

        if (parties.Length == 2)
        {
            string baseNum = parties[0];
            int exposant = int.Parse(parties[1]);
            return $"{baseNum.Replace('.', ',')} × 10<sup>{exposant}</sup>";
        }
        return formatStandard;
    }

    void OnDestroy()
    {
        UnbindAllFieldListeners();
        DetachCameraFromSelection();
        RestoreSelectionLayers();
        if (tmpDropdown != null) tmpDropdown.onValueChanged.RemoveAllListeners();
        if (legacyDropdown != null) legacyDropdown.onValueChanged.RemoveAllListeners();

        // remove multiplier listeners
        if (massMultiply10Button != null) massMultiply10Button.onClick.RemoveListener(OnMassMultiply10);
        if (massDivide10Button != null) massDivide10Button.onClick.RemoveListener(OnMassDivide10);
        if (speedMultiply10Button != null) speedMultiply10Button.onClick.RemoveListener(OnSpeedMultiply10);
        if (speedDivide10Button != null) speedDivide10Button.onClick.RemoveListener(OnSpeedDivide10);
        if (radiusMultiply10Button != null) radiusMultiply10Button.onClick.RemoveListener(OnRadiusMultiply10);
        if (radiusDivide10Button != null) radiusDivide10Button.onClick.RemoveListener(OnRadiusDivide10);
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