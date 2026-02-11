using UnityEngine;
using TMPro;

public class Floating3DText : MonoBehaviour
{
    [Header("Text Settings")]
    public GameObject textPrefab;       // Your 3D TextMeshPro prefab
    public float heightOffset = -2f;     // How high above the object

    [Header("Scaling Settings")]
    public float minScale = 0.5f;       // Minimum text scale
    public float maxScale = 2f;         // Maximum text scale
    public float scaleMultiplier = 0.1f;// How strongly distance affects scale

    private Transform player;
    private Transform textTransform;

    void Start()
    {
        player = Camera.main.transform;

        GameObject textObj = Instantiate(textPrefab, transform.position + Vector3.up * heightOffset, Quaternion.identity);

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

        textTransform.LookAt(player);
        textTransform.forward = player.forward;

    }
}
