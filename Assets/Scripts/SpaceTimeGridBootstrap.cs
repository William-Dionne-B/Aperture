using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpaceTimeGridBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureGridExists(scene);
    }

    private static void EnsureGridExists(Scene activeScene)
    {
        if (!activeScene.IsValid() || activeScene.name != "SystemeSolaire")
        {
            return;
        }

        if (Object.FindAnyObjectByType<SpaceTimeGrid>() != null)
        {
            return;
        }

        GameObject gridObject = new GameObject("SpaceTimeGrid");
        gridObject.transform.position = Vector3.zero;
        gridObject.transform.rotation = Quaternion.identity;
        gridObject.transform.localScale = Vector3.one;

        gridObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();

        Shader warpShader = Shader.Find("Unlit/SpaceTimeWarp");
        if (warpShader != null)
        {
            Material runtimeMaterial = new Material(warpShader)
            {
                name = "SpaceTimeWarp (Runtime)"
            };
            meshRenderer.sharedMaterial = runtimeMaterial;
        }

        SpaceTimeGrid grid = gridObject.AddComponent<SpaceTimeGrid>();
        grid.resolution = 250;
        grid.size = 2000f;
        grid.maxWarpDepth = 2000f;
        grid.gridCellWorldSize = 10f;
        grid.gridVerticalOffset = 0f;
        grid.followCamera = true;

        SpaceTimeController controller = gridObject.AddComponent<SpaceTimeController>();
        controller.useAllGravityBodies = true;

        Debug.Log("SpaceTimeGridBootstrap: created runtime SpaceTimeGrid for SystemeSolaire.", gridObject);
    }
}
