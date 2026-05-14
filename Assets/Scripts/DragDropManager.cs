using UnityEngine;

public class DragDropManager : MonoBehaviour
{
    public GameObject prefab;

    /// <summary>
    /// Selectionne le prefab a instancier via le PlanetSpawner.
    /// </summary>
    public void ButtonPressed()
    {
        if (prefab == null)
        {
            return;
        }

        PlanetSpawner spawner = Camera.main?.GetComponent<PlanetSpawner>();
        if (spawner == null)
        {
            return;
        }

        spawner.SetPrefab(prefab);
    }
}