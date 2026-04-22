using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
    public GameObject timeScrollMenuUI;

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
        Debug.Log("PauseMenu initialized. Press ESC to open the menu, SPACE to toggle simulation pause.");
    }

    void Update()
    {
        UpdateSpeedText();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI != null && keysMenuUI.activeSelf || audioMenuUI != null && audioMenuUI.activeSelf) 
                OpenOptions();
            else if (optionMenuUI != null && optionMenuUI.activeSelf || guideMenuUI != null && guideMenuUI.activeSelf) 
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

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            simulationSpeedSlider.value += step;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            simulationSpeedSlider.value -= step;
        }
    }
    
    public void OnSpeedSliderChanged()
    {
        if (timeManager != null && simulationSpeedSlider != null)
        {
            timeManager.SetSpeedMultiplier(simulationSpeedSlider.value);
        }
    }

    private void UpdateSpeedText()
    {
        if (speedValueText != null)
        {
            // Affiche la vitesse mémorisée dans le TimeManager
            float displaySpeed = TimeManager.currentSpeedMultiplier;
            speedValueText.text = "Vitesse: x" + displaySpeed.ToString("F1");
        }
    }
    
    // ==========================================
    // CONTRÔLE DU TEMPS DE SIMULATION (ESPACE)
    // ==========================================
    
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
    // CONTRÔLE DE L'ÉTAT DU MENU (ÉCHAP)
    // ==========================================

    /// <summary>
    /// Ferme le menu Échap et relance le moteur Unity, en respectant le multiplicateur de vitesse.
    /// </summary>
    public void Resume()
    {
        DesactivateAllMenus();
        if (timeMenuUI != null) timeMenuUI.SetActive(true);
        if (timeScrollMenuUI != null) timeScrollMenuUI.SetActive(true);
        
        if (isSimulationPaused) TimeManager.Pause();
        else TimeManager.Resume();
        
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Ouvre le menu Échap et fige tout le moteur Unity de force.
    /// </summary>
    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (timeMenuUI != null) timeMenuUI.SetActive(false);
        if (timeScrollMenuUI != null) timeScrollMenuUI.SetActive(false);
        
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
            if (movementSpeedSlider != null) movementSpeedSlider.value = cameraScript.moveSpeed;
        }
    }

    private void DesactivateAllMenus()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionMenuUI != null) optionMenuUI.SetActive(false);
        if (guideMenuUI != null) guideMenuUI.SetActive(false);
        if (keysMenuUI != null) keysMenuUI.SetActive(false);
        if (audioMenuUI != null) audioMenuUI.SetActive(false);
        //Resume();
    }

    // ==========================================
    // GESTION DES PARAMÈTRES (OPTIONS)
    // ==========================================

    public void Reset()
    {
        if (fieldOfViewSlider != null) fieldOfViewSlider.value = 60f;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = 3.5f;
        if (movementSpeedSlider != null) movementSpeedSlider.value = 100f;
        
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

            if (movementSpeedSlider != null)
            {
                PlayerPrefs.SetFloat("MoveSpeed", movementSpeedSlider.value);
                cameraScript.moveSpeed = movementSpeedSlider.value;
            }

            PlayerPrefs.Save();
            Debug.Log("Paramètres sauvegardés avec succès !");
        }

        OpenPauseMenu();
    }
}