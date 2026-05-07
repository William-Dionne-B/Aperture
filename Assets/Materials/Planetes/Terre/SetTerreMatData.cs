using UnityEngine;

public class SetTerreMatData : MonoBehaviour
{
    public GameObject SoleilParent;

    private Renderer rend;
    private MaterialPropertyBlock mpb;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (SoleilParent == null || rend == null) return;

        Vector3 lightDir = (transform.position - SoleilParent.transform.position).normalized;

        rend.GetPropertyBlock(mpb);
        mpb.SetVector("_LightDir", lightDir);
        rend.SetPropertyBlock(mpb);
    }
}