using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ConstrainUIToAspectRatio : MonoBehaviour
{
    private float targetAspect = 16f / 9f;
    private RectTransform rt;
    private Vector2 lastScreenSize;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        Vector2 currentSize = new Vector2(Screen.width, Screen.height);
        if (currentSize != lastScreenSize)
        {
            Apply();
            lastScreenSize = currentSize;
        }
    }

    void Apply()
    {
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // Letterbox : barres haut/bas
            float newHeight = Screen.height * scaleHeight;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        }
        else
        {
            // Pillarbox : barres gauche/droite
            float scaleWidth = 1.0f / scaleHeight;
            float newWidth = Screen.width * scaleWidth;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
        }

        rt.anchoredPosition = Vector2.zero;
    }
}