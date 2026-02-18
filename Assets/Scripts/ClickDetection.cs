using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClickDetection : MonoBehaviour
{
    private Camera mainCam;

    [SerializeField]    
    private Material outlinerMat; // Assignez `Outliner_MAT` dans l'inspecteur.

    private GameObject currentLookedAt;

    // Nouvelle table pour restaurer proprement les matériaux originaux par Renderer
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogWarning("Aucune Camera avec le tag 'MainCamera' trouvée.");

        if (outlinerMat == null)
        {
            // 1) Essaye Resources (si vous placez le mat dans un dossier Resources)
            outlinerMat = Resources.Load<Material>("Outliner_MAT");
        }

        if (outlinerMat == null)
        {
            // 2) Fallback : recherche parmi les matériaux chargés (fonctionne en Editor et si l'asset est inclus dans la build)
            outlinerMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "Outliner_MAT");
        }

        if (outlinerMat == null)
            Debug.LogWarning("Le matériau `Outliner_MAT` n'a pas été trouvé. Assignez-le dans l'inspecteur ou placez-le dans un dossier Resources.");
    }

    void Update()
    {
        if (mainCam == null) return;

        // Centre de l'écran
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = mainCam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit clickHit, Mathf.Infinity))
        {
            GameObject hitObj = clickHit.collider.gameObject;
            // Si on regarde un nouvel objet
            if (hitObj != currentLookedAt)
            {
                // Retire l'outline de l'ancien
                if (currentLookedAt != null)
                    RemoveOutlineFromObject(currentLookedAt);

                // Ajoute l'outline au nouvel objet
                AddOutlineToObject(hitObj);
                currentLookedAt = hitObj;
            }
        }
        else
        {
            // Si on ne regarde rien, retire l'outline de l'ancien
            if (currentLookedAt != null)
            {
                RemoveOutlineFromObject(currentLookedAt);
                currentLookedAt = null;
            }
        }
    }

    private void AddOutlineToObject(GameObject obj)
    {
        if (obj == null || outlinerMat == null) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Sauvegarde l'état original si ce Renderer n'a pas encore été modifié
            if (!originalMaterials.ContainsKey(rend))
            {
                var original = rend.materials; // cela renvoie une copie des matériaux actuels
                var copy = new Material[original.Length];
                original.CopyTo(copy, 0);
                originalMaterials[rend] = copy;
            }

            var mats = rend.materials;
            // Vérifie présence du matériau d'outline (tolère les instances en regardant si le nom contient)
            bool already = mats.Any(m => m != null && (m == outlinerMat || m.name.Contains(outlinerMat.name)));
            if (!already)
            {
                var newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[newMats.Length - 1] = outlinerMat;
                rend.materials = newMats;
            }
        }
    }

    private void RemoveOutlineFromObject(GameObject obj)
    {
        if (obj == null || outlinerMat == null) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Si on a sauvegardé les matériaux originaux, restaure-les proprement
            if (originalMaterials.TryGetValue(rend, out var original))
            {
                rend.materials = original;
                originalMaterials.Remove(rend);
            }
            else
            {
                // Fallback : suppression par comparaison tolérante aux instances
                var mats = rend.materials;
                if (mats.Any(m => m == outlinerMat || (m != null && m.name.Contains(outlinerMat.name))))
                {
                    var list = new List<Material>(mats);
                    list.RemoveAll(m => m == outlinerMat || (m != null && m.name.Contains(outlinerMat.name)));

                    if (list.Count == 0)
                    {
                        rend.materials = new Material[0];
                    }
                    else
                    {
                        rend.materials = list.ToArray();
                    }
                }
            }
        }
    }
}