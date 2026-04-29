using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject saveItemPrefab;
    [SerializeField] private KeyCode toggleKey = KeyCode.F5;
    [SerializeField] private GameObject menuRoot;

    private string saveExtension = ".json";

    private SystemeSauvegarde loader;
    private bool isOpen;

    private void Awake()
    {
        loader = Object.FindFirstObjectByType<SystemeSauvegarde>();
    }

    private void Start()
    {
        RefreshSaveList();
        menuRoot.SetActive(false);
        isOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;

        menuRoot.SetActive(isOpen);

        Time.timeScale = isOpen ? 0f : 1f;

        Debug.Log("Menu state: " + isOpen);
    }


    // CREATE SAVE
    public void CreateSave()
    {

        if (loader == null)
        {
            Debug.LogWarning("No SystemeSauvegarde found in scene!");
            return;
        }

        string fileName = saveNameInput.text;

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "save_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string fullPath = Path.Combine(Application.persistentDataPath, fileName + saveExtension);

        loader.SaveScene(fullPath);

        Debug.Log("Saved: " + fullPath);

        RefreshSaveList();
    }


    // LOAD SAVE
    public void LoadSave(string filePath)
    {

        if (loader == null)
            return;

        loader.LoadScene(filePath);

        Debug.Log("Loaded: " + filePath);
    }


    // DELETE SAVE
    public void DeleteSave(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            Debug.Log("Deleted: " + filePath);
        }

        RefreshSaveList();
    }


    // REFRESH UI LIST
    public void RefreshSaveList()
    {
        foreach (Transform child in contentParent)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*" + saveExtension);

        foreach (string file in files)
        {
            CreateSaveItem(file);
        }
    }


    // CREATE ONE UI ENTRY
    private void CreateSaveItem(string filePath)
    {
        GameObject item = Instantiate(saveItemPrefab, contentParent);

        string fileName = Path.GetFileNameWithoutExtension(filePath);

        TMP_Text label = item.transform.Find("FileName").GetComponent<TMP_Text>();
        Button loadBtn = item.transform.Find("LoadButton").GetComponent<Button>();
        Button deleteBtn = item.transform.Find("DeleteButton").GetComponent<Button>();

        label.text = fileName;

        loadBtn.onClick.AddListener(() => LoadSave(filePath));
        deleteBtn.onClick.AddListener(() => DeleteSave(filePath));
    }
}