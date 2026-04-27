using UnityEditor;
using UnityEngine;
using System.IO;

public class JsonSelectorWindow : EditorWindow
{
    private string[] jsonFiles;
    private Vector2 scroll;

    [MenuItem("Tools/JSON Selector _F9")] // ctrl shift f9
    public static void ShowWindow()
    {
        GetWindow<JsonSelectorWindow>("JSON Selector");
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        string path = Application.persistentDataPath;

        if (!Directory.Exists(path))
        {
            jsonFiles = new string[0];
            return;
        }
        
        
        jsonFiles = Directory.GetFiles(path, "*.json");

    }

    private void OnGUI()
    {
        if (GUILayout.Button("Refresh"))
            Refresh();

        GUILayout.Space(5);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (jsonFiles == null || jsonFiles.Length == 0)
        {
            GUILayout.Label("No saves found.");
        }
        else
        {
            foreach (var file in jsonFiles)
            {
                DrawSaveEntry(file);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSaveEntry(string file)
    {
        string fileName = Path.GetFileName(file);

        EditorGUILayout.BeginHorizontal("box");

        // LOAD BUTTON (optional if you still want it)
        if (GUILayout.Button("Load", GUILayout.Width(60)))
        {
            LoadSave(file);
        }

        GUILayout.Label(fileName);

        // DELETE BUTTON
        if (GUILayout.Button("Delete", GUILayout.Width(70)))
        {
            DeleteSave(file);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void LoadSave(string fullPath)
    {
        Debug.Log("Loading: " + fullPath);

        // IMPORTANT:
        // This only works in Play Mode unless you bridge into runtime
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to load scene.");
            return;
        }

        var loader = Object.FindFirstObjectByType<SystemeSauvegarde>();
        if (loader != null)
        {
            loader.LoadScene(fullPath);
        }
        else
        {
            Debug.LogWarning("No Scene Loader found in scene.");
        }
    }

    private void DeleteSave(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log("Deleted: " + fullPath);
            Refresh(); // update UI immediately
        }
    }
}