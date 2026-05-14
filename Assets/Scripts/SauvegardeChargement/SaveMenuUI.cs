using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public Transform contentParent; // ScrollView Content
    public GameObject saveButtonPrefab;

    private string selectedSave = null;
    private List<string> saves = new List<string>();

    /// <summary>
    /// Initialise la liste des sauvegardes.
    /// </summary>
    void Start()
    {
        RefreshList();
    }

    /// <summary>
    /// Rafraichit l'UI de la liste des sauvegardes.
    /// </summary>
    public void RefreshList()
    {
        // Clear UI
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        saves.Clear();

        string path = Application.persistentDataPath;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            saves.Add(fileName);

            GameObject btnObj = Instantiate(saveButtonPrefab, contentParent);

            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            txt.text = fileName;

            Button btn = btnObj.GetComponent<Button>();

            btn.onClick.AddListener(() =>
            {
                SelectSave(fileName, btnObj);
            });
        }
    }

    /// <summary>
    /// Selectionne une sauvegarde et met en surbrillance le bouton.
    /// </summary>
    void SelectSave(string saveName, GameObject buttonObj)
    {
        selectedSave = saveName;

        Debug.Log("Selected save: " + saveName);

        // Optional: highlight selected button
        foreach (Transform child in contentParent)
        {
            child.GetComponent<Image>().color = Color.white;
        }

        buttonObj.GetComponent<Image>().color = Color.green;
    }

    /// <summary>
    /// Cree une nouvelle sauvegarde avec le nom saisi.
    /// </summary>
    public void CreateSave()
    {
        string saveName = inputField.text;

        if (string.IsNullOrEmpty(saveName))
        {
            Debug.LogWarning("Save name is empty!");
            return;
        }

        SystemeSauvegarde.Instance.SaveScene(saveName);

        RefreshList();
    }

    /// <summary>
    /// Charge la sauvegarde selectionnee.
    /// </summary>
    public void LoadSelected()
    {
        if (string.IsNullOrEmpty(selectedSave))
        {
            Debug.LogWarning("No save selected!");
            return;
        }

        SystemeSauvegarde.Instance.LoadScene(selectedSave);
    }

    /// <summary>
    /// Supprime la sauvegarde selectionnee.
    /// </summary>
    public void DeleteSelected()
    {
        if (string.IsNullOrEmpty(selectedSave))
        {
            Debug.LogWarning("No save selected!");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, selectedSave);

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Deleted: " + selectedSave);
        }

        selectedSave = null;

        RefreshList();
    }
}