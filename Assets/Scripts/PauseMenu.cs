using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    public GameObject saveMenuUI;

    [Header("Options UI Elements")] 
    public Slider fieldOfViewSlider;
    public Slider mouseSensitivitySlider;
    public Slider movementSpeedSlider;
    public Slider simulationSpeedSlider;
    public TextMeshProUGUI speedValueText;
    
    [Header("Icones du Bouton Pause")]
    public Image boutonSimulationImage;
    public Sprite spritePause;
    public Sprite spritePlay;
    
    [Header("External Scripts")]
    public FreeFlyCamera cameraScript;
    public TimeManager timeManager;
    

    public static bool isMenuOpen = false; 
    public static bool isSimulationPaused = false; 

    // ==========================================
    // MÉTHODES UNITY
    // ==========================================
    
    void Start()
    {
        Resume();
        
        if (mouseSensitivitySlider != null && cameraScript != null)
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 3.5f);
        
        if (fieldOfViewSlider != null && cameraScript != null)
            fieldOfViewSlider.value = PlayerPrefs.GetFloat("FieldOfView", 60f);

        if (movementSpeedSlider != null && cameraScript != null)
            movementSpeedSlider.value = PlayerPrefs.GetFloat("MoveSpeed", 100f);
        
        if (simulationSpeedSlider != null)
        {
            simulationSpeedSlider.value = TimeManager.currentSpeedMultiplier;
            simulationSpeedSlider.onValueChanged.AddListener(delegate { OnSpeedSliderChanged(); });
        }
        
        DesactivateAllMenus();
    }

    void Update()
    {
        UpdateSpeedText();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI != null && keysMenuUI.activeSelf)
                OpenOptions();
            else if (optionMenuUI != null && optionMenuUI.activeSelf || guideMenuUI != null && guideMenuUI.activeSelf || saveMenuUI != null && saveMenuUI.activeSelf)
                OpenPauseMenu();
            else if (isMenuOpen)
                Resume();
            else
                Pause();
        }
        else if (!isMenuOpen)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleSimulation();
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                Pause();
                OpenSave();
            }

            HandleKeyboardSpeedControl();
        }
    }

    // --- LOGIQUE DE VITESSE ---

    /// <summary>
    /// Permet de modifier la valeur du slider avec les flèches directionnelles.
    /// </summary>
    private void HandleKeyboardSpeedControl()
    {
        if (simulationSpeedSlider == null) return;

        float step = 1.0f;
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            simulationSpeedSlider.value += step;
            changed = true;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            simulationSpeedSlider.value -= step;
            changed = true;
        }

        if (changed)
        {
            ClearUIFocus();
        }
    }
    
    /// <summary>
    /// Actions fait quand on change une valeur sur le slider de la vitesse de la simulation.
    /// </summary>
    public void OnSpeedSliderChanged()
    {
        if (timeManager != null && simulationSpeedSlider != null)
        {
            timeManager.SetSpeedMultiplier(simulationSpeedSlider.value);
            ClearUIFocus();
        }
    }

    /// <summary>
    /// Change le texte qui represente la vitesse de la simulation.
    /// </summary>
    private void UpdateSpeedText()
    {
        if (speedValueText != null)
        {
            // Affiche la vitesse mémorisée dans le TimeManager
            float displaySpeed = TimeManager.currentSpeedMultiplier;
            speedValueText.text = "Vitesse: x" + displaySpeed.ToString("F1");
        }
    }
    
    /// <summary>
    /// Focntion de deselection
    /// </summary>
    private void ClearUIFocus()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    
    // ==========================================
    // CONTRÔLE DU TEMPS DE SIMULATION
    // ==========================================
    
    /// <summary>
    /// Change l'etat du temps (pause/resume)
    /// </summary>
    public void ToggleSimulation()
    {
        isSimulationPaused = !isSimulationPaused;

        if (isSimulationPaused)
        {
            TimeManager.Pause();
            if (boutonSimulationImage != null) boutonSimulationImage.sprite = spritePlay;
        }
        else
        {
            TimeManager.Resume();
            if (boutonSimulationImage != null) boutonSimulationImage.sprite = spritePause;
        }
    }


    // ==========================================
    // CONTRÔLE DE L'ÉTAT DU MENU
    // ==========================================

    /// <summary>
    /// Ferme le menu pause et relance le moteur Unity, en respectant le multiplicateur de vitesse.
    /// </summary>
    public void Resume()
    {
        DesactivateAllMenus();
        
        if (isSimulationPaused) TimeManager.Pause();
        else TimeManager.Resume();
        
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        ClearUIFocus();
    }

    /// <summary>
    /// Ouvre le menu pause et fige tout le moteur Unity de force.
    /// </summary>
    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        
        TimeManager.Pause(); 
        isMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Ferme complètement l'application.
    /// </summary>
    public void QuitGame()
    {
        SystemeSauvegarde.Instance.SaveScene("Autosave");
        SceneManager.LoadScene("MenuAccueil");
    }

    // ==========================================
    // NAVIGATION DES MENUS
    // ==========================================
    
    /// <summary>
    /// Fonction qui ouvre le menu pause
    /// </summary>
    public void OpenPauseMenu()
    {
        DesactivateAllMenus();
        pauseMenuUI.SetActive(true);
    }
    
    /// <summary>
    /// Fonction qui ouvre le menu guide
    /// </summary>
    public void OpenGuide()
    {
        DesactivateAllMenus();
        guideMenuUI.SetActive(true);
    }
    
    /// <summary>
    /// Fonction qui ouvre le menu cles
    /// </summary>
    public void OpenKeys()
    {
        DesactivateAllMenus();
        keysMenuUI.SetActive(true);
    }
    
    /// <summary>
    /// Fonction qui ouvre le menu options
    /// </summary>
    public void OpenOptions()
    {
        DesactivateAllMenus();
        optionMenuUI.SetActive(true);

        if (cameraScript != null)
        {
            if (fieldOfViewSlider != null) fieldOfViewSlider.value = cameraScript.playerCamera.fieldOfView;
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = cameraScript.mouseSensitivity;
            if (movementSpeedSlider != null) movementSpeedSlider.value = cameraScript.moveSpeed;
        }
    }

    /// <summary>
    /// Fonction qui ouvre le menu save
    /// </summary>
    public void OpenSave()
    {
        DesactivateAllMenus();
        saveMenuUI.SetActive(true);
    }

    /// <summary>
    /// Fonction qui ferme tout les menus
    /// </summary>
    private void DesactivateAllMenus()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionMenuUI != null) optionMenuUI.SetActive(false);
        if (guideMenuUI != null) guideMenuUI.SetActive(false);
        if (keysMenuUI != null) keysMenuUI.SetActive(false);
        if (saveMenuUI != null) saveMenuUI.SetActive(false);
    }

    // ==========================================
    // GESTION DES PARAMÈTRES
    // ==========================================
    
    /// <summary>
    /// Fonction qui change les options pour les dernieres valeurs enregistree
    /// </summary>
    public void Reset()
    {
        if (fieldOfViewSlider != null) fieldOfViewSlider.value = PlayerPrefs.GetFloat("FieldOfView");
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity");
        if (movementSpeedSlider != null) movementSpeedSlider.value = PlayerPrefs.GetFloat("MoveSpeed");
    }

    /// <summary>
    /// Fonction qui sauvegarde les options
    /// </summary>
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

            if (movementSpeedSlider != null)
            {
                PlayerPrefs.SetFloat("MoveSpeed", movementSpeedSlider.value);
                cameraScript.moveSpeed = movementSpeedSlider.value;
            }

            PlayerPrefs.Save();
        }
    }
    
    
    /// <summary>
    /// Fonction qui permet de changer les changements des options sans les sauvegardes et de quitter le menu options
    /// </summary>
    public void QuitOptions()
    {
        if (cameraScript != null)
        {
            if (mouseSensitivitySlider != null)
            {
                cameraScript.mouseSensitivity = mouseSensitivitySlider.value;
            }

            if (fieldOfViewSlider != null)
            {
                cameraScript.playerCamera.fieldOfView = fieldOfViewSlider.value;
            }

            if (movementSpeedSlider != null)
            {
                cameraScript.moveSpeed = movementSpeedSlider.value;
            }
        }
        
        OpenPauseMenu();
    }
}