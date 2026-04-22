using UnityEngine;
using TMPro;

public class Floating3DText : MonoBehaviour
{
    [Header("Text Settings")]
    public GameObject textPrefab;
    public float heightOffsetPercentage = 0.65f;
    public float minTopOffset = 0.5f;
    public float cameraOffsetPercentage = 0.35f;
    public float minSurfacePadding = 0.2f;
    public float closeRangeBoostDistance = 8f;
    public float closeRangeBoostFactor = 0.1f;

    [Header("Scaling Settings")]
    public float minScale = 0.1f;       
    public float maxScale = 500f;
    public float scaleMultiplier = 0.5f; 

    private Transform player;
    private Transform textTransform;
    private TextMeshPro textMesh;
    private Renderer objectRenderer;
    private Renderer textRenderer;

    void Start()
    {
        if (Camera.main != null)
        {
            player = Camera.main.transform;
        }

        if (textPrefab == null)
        {
            return;
        }

        GameObject textObj = Instantiate(textPrefab, transform.position, Quaternion.identity);

        textTransform = textObj.transform;
        textRenderer = textObj.GetComponent<Renderer>();
        objectRenderer = GetComponentInChildren<Renderer>();

        textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = gameObject.name;
        }
    }

    void Update()
    {
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
        }

        if (textTransform == null || player == null || textMesh == null) return;

        ObjectProperties properties = gameObject.GetComponent<ObjectProperties>();
        if (properties != null)
        {
            textMesh.text = properties.objectName;
        }
        else
        {
            textMesh.text = gameObject.name;
        }
           
        float rayonActuel = ResolveObjectRadius();

        float tailleDesiree = rayonActuel * scaleMultiplier;
        tailleDesiree = Mathf.Clamp(tailleDesiree, minScale, maxScale);
        textTransform.localScale = new Vector3(tailleDesiree, tailleDesiree, tailleDesiree);

        float demiHauteurTexte = 0f;
        if (textRenderer != null)
        {
            demiHauteurTexte = textRenderer.bounds.extents.y;
        }

        float ecartDeFlottaison = Mathf.Max(rayonActuel * heightOffsetPercentage, minTopOffset);
        Vector3 toCamera = player.position - transform.position;
        float cameraDistance = toCamera.magnitude;
        Vector3 toCameraDir = cameraDistance > 0.0001f ? toCamera / cameraDistance : Vector3.forward;

        float offsetVersCamera = (rayonActuel * cameraOffsetPercentage) + minSurfacePadding;
        if (cameraDistance < closeRangeBoostDistance)
        {
            float boost = (closeRangeBoostDistance - cameraDistance) * closeRangeBoostFactor;
            offsetVersCamera += boost;
        }

        textTransform.position = transform.position
            + (Vector3.up * (rayonActuel + ecartDeFlottaison + demiHauteurTexte + minSurfacePadding))
            + (toCameraDir * offsetVersCamera);

        textTransform.LookAt(player);
        textTransform.forward = player.forward;

    }

    float ResolveObjectRadius()
    {
        if (objectRenderer != null)
        {
            return Mathf.Max(objectRenderer.bounds.extents.y, 0.0001f);
        }

        return Mathf.Max(transform.lossyScale.y * 0.5f, 0.0001f);
    }
    
    void OnDestroy()
    {
        if (textTransform != null)
        {
            Destroy(textTransform.gameObject);
        }
    }
}