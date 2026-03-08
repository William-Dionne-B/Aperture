using UnityEngine;
using TMPro;

public class Floating3DText : MonoBehaviour
{
    [Header("Text Settings")]
    public GameObject textPrefab;
    public float heightOffsetPercentage = 0.65f;

    [Header("Scaling Settings")]
    public float minScale = 0.1f;       
    public float maxScale = 500f;
    public float scaleMultiplier = 0.5f; 

    private Transform player;
    private Transform textTransform;
    private TextMeshPro textMesh;

    void Start()
    {
        player = Camera.main.transform;

        GameObject textObj = Instantiate(textPrefab, transform.position, Quaternion.identity);

        textTransform = textObj.transform;

        textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = gameObject.name;
        }
    }

    void Update()
    {
        if (textTransform == null || player == null) return;

        if (textMesh != null && textMesh.text != gameObject.name)
        {
            textMesh.text = gameObject.name;
        }    
        
        float rayonActuel = transform.lossyScale.y / 2f;

        float ecartDeFlottaison = rayonActuel * heightOffsetPercentage;
        textTransform.position = transform.position + (Vector3.up * (rayonActuel + ecartDeFlottaison));

        textTransform.LookAt(player);
        textTransform.forward = player.forward;

        float tailleDesiree = rayonActuel * scaleMultiplier;
        tailleDesiree = Mathf.Clamp(tailleDesiree, minScale, maxScale);

        textTransform.localScale = new Vector3(tailleDesiree, tailleDesiree, tailleDesiree);
    }
    
    void OnDestroy()
    {
        if (textTransform != null)
        {
            Destroy(textTransform.gameObject);
        }
    }
}