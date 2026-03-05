using UnityEngine;
using TMPro;

public class Floating3DText : MonoBehaviour
{
    [Header("Text Settings")]
    public GameObject textPrefab;       
    
    [Tooltip("Ex: 0.2 signifie que le texte flotte 20% plus haut que le rayon de la planète")]
    public float heightOffsetPercentage = 0.65f;

    [Header("Scaling Settings")]
    public float minScale = 0.5f;       
    public float maxScale = 50f;
    public float scaleMultiplier = 0.5f; 

    private Transform player;
    private Transform textTransform;

    void Start()
    {
        player = Camera.main.transform;

        GameObject textObj = Instantiate(textPrefab, transform.position, Quaternion.identity);

        textTransform = textObj.transform;
        textTransform.SetParent(transform);

        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = gameObject.name;
        }
    }

    void Update()
    {
        if (textTransform == null || player == null) return;
       
        float rayonActuel = transform.lossyScale.y / 2f;

        float ecartDeFlottaison = rayonActuel * heightOffsetPercentage;
        textTransform.position = transform.position + (Vector3.up * (rayonActuel + ecartDeFlottaison));

        textTransform.LookAt(player);
        textTransform.forward = player.forward;

        float tailleDesiree = rayonActuel * scaleMultiplier;
        
        tailleDesiree = Mathf.Clamp(tailleDesiree, minScale, maxScale);

        Vector3 parentScale = transform.lossyScale;
        textTransform.localScale = new Vector3(
            tailleDesiree / parentScale.x,
            tailleDesiree / parentScale.y,
            tailleDesiree / parentScale.z
        );
    }
}