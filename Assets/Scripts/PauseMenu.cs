using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")] // Comme un titre
    public GameObject pauseMenuUI;

    public GameObject optionMenuUI;
    public GameObject guideMenuUI;
    public GameObject keysMenuUI;
    public GameObject timeMenuUI;

    [Header("Options Settings")] public Slider fieldOfViewSlider;
    public Slider mouseSensitivitySlider;
    public Slider speedSlider;
    public FreeFlyCamera cameraScript;

    public static bool isPaused = false;


    void Start()
    {
        if (mouseSensitivitySlider != null && cameraScript != null)
        {
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 3.5f);
        }

        if (fieldOfViewSlider != null && cameraScript != null)
        {
            fieldOfViewSlider.value = PlayerPrefs.GetFloat("FieldOfView", 60f);
        }

        if (speedSlider != null && cameraScript != null)
        {
            speedSlider.value = PlayerPrefs.GetFloat("MoveSpeed", 100f);
        }
        
        DesactivateAllMenus();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI.activeSelf)
            {
                OpenOptions();
            }

            else if (optionMenuUI.activeSelf || guideMenuUI.activeSelf)
            {
                OpenPauseMenu();
            }

            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // --- FONCTIONS PRINCIPALES ---

    public void Resume()
    {
        DesactivateAllMenus();
        timeMenuUI.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        OpenPauseMenu();
        timeMenuUI.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- NAVIGATION ENTRE LES MENUS ---

    public void QuitGame()
    {
        //SAUVEGARDE
        Debug.Log("Ending Simulator !");
        Application.Quit();
    }

    public void OpenOptions()
    {
        DesactivateAllMenus();
        optionMenuUI.SetActive(true);

        if (fieldOfViewSlider != null && cameraScript != null)
        {
            fieldOfViewSlider.value = cameraScript.playerCamera.fieldOfView;
        }

        if (mouseSensitivitySlider != null && cameraScript != null)
        {
            mouseSensitivitySlider.value = cameraScript.mouseSensitivity;
        }

        if (speedSlider != null && cameraScript != null)
        {
            speedSlider.value = cameraScript.moveSpeed;
        }
    }

    public void OpenGuide()
    {
        DesactivateAllMenus();
        guideMenuUI.SetActive(true);
    }

    public void OpenKeys()
    {
        DesactivateAllMenus();
        keysMenuUI.SetActive(true);
    }

    public void OpenPauseMenu()
    {
        DesactivateAllMenus();
        pauseMenuUI.SetActive(true);
    }

    private void DesactivateAllMenus()
    {
        pauseMenuUI.SetActive(false);
        optionMenuUI.SetActive(false);
        guideMenuUI.SetActive(false);
        keysMenuUI.SetActive(false);
    }

    public void Reset()
    {
        fieldOfViewSlider.value = 60f;
        mouseSensitivitySlider.value = 3.5f;
        speedSlider.value = 100f;
        Debug.Log("Reset !");
    }

    public void SaveOptions()
    {
        if (mouseSensitivitySlider != null && cameraScript != null)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);
            cameraScript.mouseSensitivity = mouseSensitivitySlider.value;
        }

        if (fieldOfViewSlider != null && cameraScript != null)
        {
            PlayerPrefs.SetFloat("FieldOfView", fieldOfViewSlider.value);
            cameraScript.playerCamera.fieldOfView = fieldOfViewSlider.value;
        }

        if (speedSlider != null && cameraScript != null)
        {
            PlayerPrefs.SetFloat("MoveSpeed", speedSlider.value);
            cameraScript.moveSpeed = speedSlider.value;
        }

        PlayerPrefs.Save();

        Debug.Log("Settings Saved !");

        OpenPauseMenu();
    }
}