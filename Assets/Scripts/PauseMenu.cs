using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôle le menu de pause principal, la navigation entre les sous-menus
/// et la séparation entre la pause de l'interface et la pause de la simulation.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pauseMenuUI;
    public GameObject optionMenuUI;
    public GameObject guideMenuUI;
    public GameObject keysMenuUI;
    public GameObject audioMenuUI;
    public GameObject timeMenuUI;

    [Header("Options UI Elements")] 
    public Slider fieldOfViewSlider;
    public Slider mouseSensitivitySlider;
    public Slider speedSlider;
    
    [Header("Icones du Bouton Pause")]
    public Image boutonSimulationImage;
    public Sprite spritePause;
    public Sprite spritePlay;
    
    [Header("External Scripts")]
    public FreeFlyCamera cameraScript;

    public static bool isMenuOpen = false; 
    public static bool isSimulationPaused = false; 

    // ==========================================
    // MÉTHODES UNITY
    // ==========================================
    void Start()
    {
        if (mouseSensitivitySlider != null && cameraScript != null)
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 3.5f);
        
        if (fieldOfViewSlider != null && cameraScript != null)
            fieldOfViewSlider.value = PlayerPrefs.GetFloat("FieldOfView", 60f);

        if (speedSlider != null && cameraScript != null)
            speedSlider.value = PlayerPrefs.GetFloat("MoveSpeed", 100f);
        
        DesactivateAllMenus();
        Debug.Log("PauseMenu initialized. Press ESC to open the menu, SPACE to toggle simulation pause.");
    }

    void Update()
    {
        // 1. Touche ÉCHAP (Menu)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen) Resume(); else Pause();
        }
        
        // 2. Touche ESPACE (Simulation) - Uniquement si menu fermé
        else if (Input.GetKeyDown(KeyCode.Space) && !isMenuOpen)
        {
            ToggleSimulation();
        }
    }
    // {
    //     if (Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         if (keysMenuUI.activeSelf || audioMenuUI.activeSelf) OpenOptions();
    //         else if (optionMenuUI.activeSelf || guideMenuUI.activeSelf) OpenPauseMenu();
    //         else if (isMenuOpen) Resume(); 
    //         else Pause(); 
    //     }
    //     
    //     else if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         if (isMenuOpen)
    //         {
    //             Resume();
    //         }
    //         else
    //         {
    //             ToggleSimulationTime();
    //         }
    //     }
    // }

    // ==========================================
    // CONTRÔLE DU TEMPS DE SIMULATION (ESPACE)
    // ==========================================
    
    public void ToggleSimulation()
    {
        isSimulationPaused = !isSimulationPaused; // Inverse l'état

        if (isSimulationPaused)
        {
            TimeManager.Pause();
            boutonSimulationImage.sprite = spritePlay; // On affiche Play car c'est en pause
        }
        else
        {
            TimeManager.Resume();
            boutonSimulationImage.sprite = spritePause; // On affiche Pause car ça tourne
        }
    }


    // ==========================================
    // CONTRÔLE DE L'ÉTAT DU MENU (ÉCHAP)
    // ==========================================

    /// <summary>
    /// Ferme le menu Échap et relance le moteur Unity, SANS toucher à l'état de la simulation.
    /// </summary>
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        timeMenuUI.SetActive(true);
        
        Time.timeScale = isSimulationPaused ? 0f : 1f;
        
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Ouvre le menu Échap et fige tout le moteur Unity.
    /// </summary>
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        timeMenuUI.SetActive(false);
        
        Time.timeScale = 0f;
        isMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Ferme complètement l'application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saving Simulation !");
        SystemeSauvegarde.Instance.SaveScene("save");
        Debug.Log("Ending Simulator !");
        MainMenuManager.loadScene("MenuAccueil");
        //Resume();
    }

    // ==========================================
    // NAVIGATION DES MENUS
    // ==========================================
    
    public void OpenPauseMenu()
    {
        DesactivateAllMenus();
        pauseMenuUI.SetActive(true);
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

    public void OpenAudio()
    {
        DesactivateAllMenus();
        audioMenuUI.SetActive(true);
    }
    
    public void OpenOptions()
    {
        DesactivateAllMenus();
        optionMenuUI.SetActive(true);

        if (cameraScript != null)
        {
            if (fieldOfViewSlider != null) fieldOfViewSlider.value = cameraScript.playerCamera.fieldOfView;
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = cameraScript.mouseSensitivity;
            if (speedSlider != null) speedSlider.value = cameraScript.moveSpeed;
        }
    }

    private void DesactivateAllMenus()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionMenuUI != null) optionMenuUI.SetActive(false);
        if (guideMenuUI != null) guideMenuUI.SetActive(false);
        if (keysMenuUI != null) keysMenuUI.SetActive(false);
        if (audioMenuUI != null) audioMenuUI.SetActive(false);
        Resume();
    }

    // ==========================================
    // GESTION DES PARAMÈTRES (OPTIONS)
    // ==========================================

    public void Reset()
    {
        if (fieldOfViewSlider != null) fieldOfViewSlider.value = 60f;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = 3.5f;
        if (speedSlider != null) speedSlider.value = 100f;
        
        Debug.Log("Paramètres réinitialisés aux valeurs par défaut !");
    }

    public void SaveOptions()
    {
        if (cameraScript != null)
        {
            if (mouseSensitivitySlider != null)
            {
                PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);
                cameraScript.mouseSensitivity = mouseSensitivitySlider.value;
            }

            if (fieldOfViewSlider != null)
            {
                PlayerPrefs.SetFloat("FieldOfView", fieldOfViewSlider.value);
                cameraScript.playerCamera.fieldOfView = fieldOfViewSlider.value;
            }

            if (speedSlider != null)
            {
                PlayerPrefs.SetFloat("MoveSpeed", speedSlider.value);
                cameraScript.moveSpeed = speedSlider.value;
            }

            PlayerPrefs.Save();
            Debug.Log("Paramètres sauvegardés avec succès !");
        }

        OpenPauseMenu();
    }
}