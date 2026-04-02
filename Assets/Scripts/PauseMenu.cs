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
    
    [Header("External Scripts")]
    public FreeFlyCamera cameraScript;
    
    [Header("Icones du Bouton Pause")]
    public Image imageBoutonFastBackward;
    public Image imageBoutonPause;
    public Image imageBoutonResume;
    public Image imageBoutonFastForward;
    public Sprite iconFastBackward;
    public Sprite iconFastBackwardIsSelected;
    public Sprite iconPause;
    public Sprite iconPauseIsSelected;
    public Sprite iconResume;
    public Sprite iconResumeIsSelected;
    public Sprite iconFastForward;
    public Sprite iconFastForwardIsSelected;

    // --- VARIABLES SÉPARÉES POUR RÉSOUDRE LE CONFLIT ---
    
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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI.activeSelf || audioMenuUI.activeSelf) OpenOptions();
            else if (optionMenuUI.activeSelf || guideMenuUI.activeSelf) OpenPauseMenu();
            else if (isMenuOpen) Resume(); 
            else Pause(); 
        }
        
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isMenuOpen)
            {
                Resume();
            }
            else
            {
                ToggleSimulationTime();
            }
        }
    }

    // ==========================================
    // CONTRÔLE DU TEMPS DE SIMULATION (ESPACE)
    // ==========================================
    
    public void ToggleSimulationTime()
    {
        if (isSimulationPaused)
        {
            TimeManager.Resume();
            isSimulationPaused = false;
            
            if (imageBoutonPause != null) imageBoutonPause.overrideSprite = iconPause;
            if (imageBoutonResume != null) imageBoutonResume.overrideSprite = iconResumeIsSelected;
        }
        else
        {
            TimeManager.Pause();
            isSimulationPaused = true;
            
            if (imageBoutonPause != null) imageBoutonPause.overrideSprite = iconPauseIsSelected;
            if (imageBoutonResume != null) imageBoutonResume.overrideSprite = iconResume;
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
        DesactivateAllMenus();
        timeMenuUI.SetActive(true);
        
        if (isSimulationPaused)
        {
            Time.timeScale = 0f; 
        }
        else
        {
            Time.timeScale = 1f; 
        }
        
        isMenuOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Ouvre le menu Échap et fige tout le moteur Unity.
    /// </summary>
    public void Pause()
    {
        OpenPauseMenu();
        timeMenuUI.SetActive(false);
        
        Time.timeScale = 0f; // Fige le moteur Unity
        isMenuOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Ferme complètement l'application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Ending Simulator !");
        MainMenuManager.loadScene("MenuAccueil");
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