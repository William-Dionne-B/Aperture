using UnityEngine;

public class DragDropManager : MonoBehaviour
{
    [Tooltip("Prefab to spawn when this button is pressed.")]
    public GameObject prefab;

    public void ButtonPressed()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[DragDropManager] No prefab assigned on '{gameObject.name}'.");
            return;
        }

        PlanetSpawner spawner = Camera.main?.GetComponent<PlanetSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[DragDropManager] PlanetSpawner not found on Main Camera.");
            return;
        }

        spawner.SetPrefab(prefab);
        Debug.Log($"[DragDropManager] Sent prefab '{prefab.name}' to PlanetSpawner.");
    }
}