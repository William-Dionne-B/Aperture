using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickDetection : MonoBehaviour
{
    private Camera mainCam;

    private GameObject currentLookedAt;
    public GameObject selectedObject;

    private GameObject selectionSpriteGO;
    private GameObject spriteTarget;

    private Coroutine pendingDeselectionCoroutine;

    [SerializeField] private Sprite selectionSprite;
    [SerializeField] private Vector3 selectionSpriteOffset = Vector3.zero;

    [SerializeField] private float selectionSpriteScale = 0.5f;
    [SerializeField] private float selectionSpriteMinScale = 0.4f;

    [SerializeField] private float hoverRotationSpeed = 45f;

    void Start()
    {
        mainCam = Camera.main;
        CreateSelectionSpheres();
    }

    void Update()
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        GameObject hitObj = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hitObj = GetRootObject(hit.collider.gameObject);

            if (hitObj != null)
            {
                currentLookedAt = hitObj;

                if (Input.GetMouseButtonDown(0))
                {
                    // Ne pas sélectionner si le pointeur est sur un élément UI (même transparent si il bloque les raycasts)
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    {
                        // Ignorer la sélection quand on clique sur l'UI
                    }
                    else
                    {
                        if (pendingDeselectionCoroutine != null)
                            StopCoroutine(pendingDeselectionCoroutine);

                        if (selectedObject != hitObj)
                            selectedObject = hitObj;
                    }
                }
            }
        }
        else
        {
            currentLookedAt = null;

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    if (selectedObject != null)
                        pendingDeselectionCoroutine = StartCoroutine(DelayedDeselect(selectedObject));
                }
            }
        }

        GameObject target = currentLookedAt != null ? currentLookedAt : selectedObject;

        if (target != null)
        {
            if (selectionSpriteGO == null || spriteTarget != target)
            {
                CreateSelectionSprite(target);
                spriteTarget = target;
            }

            UpdateSprite(target);
        }
        else
        {
            RemoveSprite();
        }
    }

    void LateUpdate()
    {
        GameObject target = currentLookedAt != null ? currentLookedAt : selectedObject;

        if (target != null && selectionSpriteGO != null)
        {
            Vector3 center = target.transform.position;
            selectionSpriteGO.transform.position = center + selectionSpriteOffset;
        }
    }

    void CreateSelectionSpheres()
    {
        ObjectProperties[] objects = FindObjectsOfType<ObjectProperties>();

        foreach (var obj in objects)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.name = "SelectionSphere";
            sphere.transform.SetParent(obj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;

            float r = obj.gameObject.GetComponent<ObjectProperties>().radius;

            Destroy(sphere.GetComponent<MeshRenderer>());

            SphereCollider col = sphere.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = r + 6.9f;

            sphere.layer = LayerMask.NameToLayer("Default");
        }
    }

    GameObject GetRootObject(GameObject obj)
    {
        ObjectProperties prop = obj.GetComponentInParent<ObjectProperties>();
        return prop != null ? prop.gameObject : null;
    }

    float GetObjectRadius(GameObject obj)
    {
        float radius = obj.GetComponent<ObjectProperties>().radius;
        return radius;
    }

    private IEnumerator DelayedDeselect(GameObject obj)
    {
        yield return null;
        yield return null;

        if (selectedObject == obj)
            selectedObject = null;
    }

    private void CreateSelectionSprite(GameObject obj)
    {
        RemoveSprite();

        if (selectionSprite == null || obj == null) return;

        Vector3 center = obj.transform.position;

        selectionSpriteGO = new GameObject("SelectionSprite");

        var sr = selectionSpriteGO.AddComponent<SpriteRenderer>();
        sr.sprite = selectionSprite;
        sr.sortingOrder = 1000;

        selectionSpriteGO.transform.position = center + selectionSpriteOffset;
    }

    private void UpdateSprite(GameObject target)
    {
        if (selectionSpriteGO == null) return;

        float distance = Vector3.Distance(mainCam.transform.position, target.transform.position);

        float scale = Mathf.Max(distance * selectionSpriteScale, selectionSpriteMinScale);
        selectionSpriteGO.transform.localScale = Vector3.one * scale;

        Vector3 dir = mainCam.transform.position - selectionSpriteGO.transform.position;
        Quaternion lookRot = Quaternion.LookRotation(dir);

        if (currentLookedAt == target)
        {
            selectionSpriteGO.transform.rotation =
                lookRot * Quaternion.Euler(0, 0, Time.time * hoverRotationSpeed);
        }
        else
        {
            selectionSpriteGO.transform.rotation = lookRot;
        }
    }

    private void RemoveSprite()
    {
        if (selectionSpriteGO != null)
        {
            Destroy(selectionSpriteGO);
            selectionSpriteGO = null;
            spriteTarget = null;
        }
    }
}