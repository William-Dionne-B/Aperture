using UnityEngine;
using UnityEngine.UI;

public class SetAspectRatio : MonoBehaviour
{
    private float targetAspect = 16f / 9f;

    private Canvas overlayCanvas;
    private RectTransform barTop;
    private RectTransform barBottom;
    private RectTransform barLeft;
    private RectTransform barRight;

    void Start()
    {
        CreateOverlayBars();
    }

    void CreateOverlayBars()
    {
        GameObject canvasGO = new GameObject("AspectRatioOverlay");
        DontDestroyOnLoad(canvasGO);
        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        barTop = CreateBar(canvasGO.transform, "BarTop");
        barBottom = CreateBar(canvasGO.transform, "BarBottom");
        barLeft = CreateBar(canvasGO.transform, "BarLeft");
        barRight = CreateBar(canvasGO.transform, "BarRight");
    }

    RectTransform CreateBar(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false; 
        return go.GetComponent<RectTransform>();
    }

    void Update()
    {
        ApplyAspectRatio();
    }

    void ApplyAspectRatio()
    {
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera cam = GetComponent<Camera>();
        float sw = Screen.width;
        float sh = Screen.height;

        if (scaleHeight < 1.0f)
        {
            float barH = (1.0f - scaleHeight) / 2.0f * sh;

            cam.rect = new Rect(0, (1.0f - scaleHeight) / 2.0f, 1, scaleHeight);

            SetBarRect(barTop, 0, sh - barH, sw, barH);
            SetBarRect(barBottom, 0, 0, sw, barH);
            SetBarRect(barLeft, Vector4.zero);
            SetBarRect(barRight, Vector4.zero);
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            float barW = (1.0f - scaleWidth) / 2.0f * sw;

            cam.rect = new Rect((1.0f - scaleWidth) / 2.0f, 0, scaleWidth, 1);

            SetBarRect(barLeft, 0, 0, barW, sh);
            SetBarRect(barRight, sw - barW, 0, barW, sh);
            SetBarRect(barTop, Vector4.zero);
            SetBarRect(barBottom, Vector4.zero);
        }
    }


    void SetBarRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }


    void SetBarRect(RectTransform rt, Vector4 _)
    {
        rt.sizeDelta = Vector2.zero;
    }

    void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
    }
}