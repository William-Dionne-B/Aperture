using UnityEditor;
using UnityEngine;
using System.IO;

public class SaveCreatorWindow : EditorWindow
{
    private string fileName = "save_";
    private Vector2 scroll;

    [MenuItem("Tools/JSON Writer _F5")] // ctrl shift f5
    public static void Open()
    {
        GetWindow<SaveCreatorWindow>("Save Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Save File", EditorStyles.boldLabel);

        fileName = EditorGUILayout.TextField("Save Name", fileName);

        if (GUILayout.Button("Create Save"))
        {
            CreateSave();
        }

        GUILayout.Space(10);
        GUILayout.Label("Existing Saves", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        var files = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (var file in files)
        {
            GUILayout.Label(Path.GetFileName(file));
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateSave()
    {
        var loader = Object.FindFirstObjectByType<SystemeSauvegarde>();

        if (loader == null)
        {
            Debug.LogWarning("No SceneLoader found in scene!");
            return;
        }

        // Generate full path
        string fullPath = Path.Combine(Application.persistentDataPath, fileName);

        if (!fullPath.EndsWith(".json"))
            fullPath += ".json";

        // CALL YOUR SAVE METHOD HERE
        loader.SaveScene(fullPath);

        Debug.Log("Saved new file: " + fullPath);
    }
}