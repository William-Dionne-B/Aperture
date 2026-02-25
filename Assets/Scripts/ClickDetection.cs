using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClickDetection : MonoBehaviour
{
    private Camera mainCam;

    [SerializeField]
    private Material outlinerMat; // Assignez `Outliner_MAT` dans l'inspecteur.
    [SerializeField]
    private Material selectionOutlineMat; // Assignez le matériau d'outline de sélection.

    private GameObject currentLookedAt;

    // Objet sélectionné (conservé après le clic gauche)
    private GameObject selectedObject;

    // Nouvelle table pour restaurer proprement les matériaux originaux par Renderer
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogWarning("Aucune Camera avec le tag 'MainCamera' trouvée.");

        if (outlinerMat == null)
        {
            outlinerMat = Resources.Load<Material>("Outliner_MAT");
        }

        if (outlinerMat == null)
        {
            outlinerMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.Contains("Outliner_MAT") || m.name.Contains("Outliner"));
        }

        if (outlinerMat == null)
            Debug.LogWarning("Outliner mat est nul-part");

        // Charge le matériau de sélection (fallback similaire)
        if (selectionOutlineMat == null)
        {
            selectionOutlineMat = Resources.Load<Material>("SelectionOutliner_MAT");
        }

        if (selectionOutlineMat == null)
        {
            selectionOutlineMat = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name.Contains("SelectionOutliner") || m.name.Contains("Selection_Outline") || m.name.Contains("SelectionOutline") || m.name.Contains("Selection"));
        }

        if (selectionOutlineMat == null)
            Debug.LogWarning("Selection outline mat est nul-part");
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
                // Retire l'outline de l'ancien uniquement si ce n'était pas l'objet sélectionné
                if (currentLookedAt != null)
                {
                    string oldName = currentLookedAt.name;
                    if (currentLookedAt != selectedObject)
                    {
                        RemoveOutlineFromObject(currentLookedAt);
                    }
                    Debug.Log("Ne regarde plus : " + oldName); // Debug quand on ne regarde plus, nom objet
                }

                // Ajoute l'outline au nouvel objet seulement si ce n'est pas déjà l'objet sélectionné
                if (hitObj != selectedObject)
                    AddOutlineToObject(hitObj);

                currentLookedAt = hitObj;
                Debug.Log("Regarde : " + hitObj.name); // debug quand on regarde un objet, nom objet
            }

            // Au clic gauche, gère la sélection (outline de sélection)
            if (Input.GetMouseButtonDown(0))
            {
                if (hitObj != null)
                {
                    // Si on avait une sélection différente, la retirer
                    if (selectedObject != null && selectedObject != hitObj)
                    {
                        RemoveSelectionOutlineFromObject(selectedObject);
                    }

                    // Si on sélectionne un nouvel objet (ou le même), appliquer l'outline de sélection
                    if (selectedObject != hitObj)
                    {
                        selectedObject = hitObj;
                        AddSelectionOutlineToObject(selectedObject);
                        Debug.Log("Objet sélectionné : " + selectedObject.name);
                    }
                }
            }
        }
        else
        {
            // Si on ne regarde rien, retire l'outline de l'ancien uniquement s'il n'est pas sélectionné
            if (currentLookedAt != null)
            {
                string oldName = currentLookedAt.name;
                if (currentLookedAt != selectedObject)
                {
                    RemoveOutlineFromObject(currentLookedAt);
                }
                currentLookedAt = null;
                Debug.Log("Ne regarde plus : " + oldName);
            }
        }
    }

    private void AddOutlineToObject(GameObject obj)
    {
        if (obj == null || outlinerMat == null) return;

        // Si l'objet est sélectionné, on ne doit pas ajouter l'outline temporaire
        if (obj == selectedObject) return;

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

    private void AddSelectionOutlineToObject(GameObject obj)
    {
        if (obj == null || selectionOutlineMat == null) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Sauvegarde l'état original si ce Renderer n'a pas encore été modifié
            if (!originalMaterials.ContainsKey(rend))
            {
                var original = rend.materials;
                var copy = new Material[original.Length];
                original.CopyTo(copy, 0);
                originalMaterials[rend] = copy;
            }

            var mats = rend.materials;

            // Si le renderer contient déjà le matériau de sélection, rien à faire
            bool alreadySelection = mats.Any(m => m != null && (m == selectionOutlineMat || m.name.Contains(selectionOutlineMat.name)));
            if (alreadySelection) continue;

            // Si le renderer a l'outline temporaire, on le remplace par l'outline de sélection
            int outIndex = -1;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && (m == outlinerMat || m.name.Contains(outlinerMat.name)))
                {
                    outIndex = i;
                    break;
                }
            }

            if (outIndex >= 0)
            {
                mats[outIndex] = selectionOutlineMat;
                rend.materials = mats;
            }
            else
            {
                // Sinon on ajoute l'outline de sélection à la fin
                var newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[newMats.Length - 1] = selectionOutlineMat;
                rend.materials = newMats;
            }
        }
    }

    private void RemoveSelectionOutlineFromObject(GameObject obj)
    {
        if (obj == null || selectionOutlineMat == null) return;

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
                if (mats.Any(m => m == selectionOutlineMat || (m != null && m.name.Contains(selectionOutlineMat.name))))
                {
                    var list = new List<Material>(mats);
                    list.RemoveAll(m => m == selectionOutlineMat || (m != null && m.name.Contains(selectionOutlineMat.name)));

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

        // Si on regarde encore cet objet, réappliquer l'outline de regard (temporaire)
        if (currentLookedAt == obj)
        {
            AddOutlineToObject(obj);
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