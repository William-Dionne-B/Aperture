using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ObjectManager : MonoBehaviour
{
    private GameObject selection;

    public GameObject MainCamera;
    public GameObject InfoUI;
    public GameObject SelectionViewFrame;

    public GameObject speed;
    public GameObject mass;
    public GameObject radius;
    public GameObject obj_name;
    public GameObject dist_etoile;

    public Camera SelectionCamera;
    public RenderTexture SelectionRenderTexture;

    private RawImage selectionRawImage;

    public float cameraPadding = 1.5f;

    TMP_InputField massTmp; InputField massUi; UnityAction<string> massListener;
    TMP_InputField speedTmp; InputField speedUi; UnityAction<string> speedListener;
    TMP_InputField radiusTmp; InputField radiusUi; UnityAction<string> radiusListener;
    TMP_InputField nameTmp; InputField nameUi; UnityAction<string> nameListener;

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
        SelectionCamera.clearFlags = CameraClearFlags.SolidColor;
        SelectionCamera.backgroundColor = Color.black;
        SelectionCamera.fieldOfView = 60f;

        if (SelectionViewFrame != null)
        {
            selectionRawImage = SelectionViewFrame.GetComponent<RawImage>();
            if (selectionRawImage != null)
            {
                selectionRawImage.texture = SelectionRenderTexture;
            }
        }

        updateUIVisibility();
    }

    void Update()
    {
        if (MainCamera == null) return;

        var click = MainCamera.GetComponent<ClickDetection>();
        if (click == null) return;

        var selected = click.selectedObject;

        if (selection != selected)
        {
            selection = selected;
            BindFieldListeners();
        }

        UpdateSelectionCamera();

        if (IsAnyFieldEditing())
            return;

        updateUIVisibility();
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
    }
}