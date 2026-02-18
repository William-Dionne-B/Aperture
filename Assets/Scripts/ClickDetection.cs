using System.Collections.Generic;
using UnityEngine;

public class ClickDetection : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogWarning("Aucune Camera avec le tag 'MainCamera' trouvée.");
    }

    void Update()
    {
        if (mainCam == null) return;

        // Centre de l'écran
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = mainCam.ScreenPointToRay(screenCenter);

        // Clic gauche (la logique existante est conservée)
        if (Input.GetMouseButtonDown(0))
        {
            // Réutilise le même raycast depuis le centre d'écran
            if (Physics.Raycast(ray, out RaycastHit clickHit, Mathf.Infinity))
            {
                Debug.Log($"Objet touché: {clickHit.collider.gameObject.name}");
            }
            else
            {
                Debug.Log("Aucun objet touché par le raycast.");
            }
        }
    }
}