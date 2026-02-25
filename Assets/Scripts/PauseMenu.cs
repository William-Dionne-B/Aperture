using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôle le menu de pause principal, la navigation entre les sous-menus
/// (Options, Guide, Touches) et la gestion des paramètres utilisateur.
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

    // --- Variables Globales ---
    public static bool isPaused = false;

    // ==========================================
    // MÉTHODES UNITY
    // ==========================================

    void Start()
    {
        // Initialisation visuelle des sliders au démarrage
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
        // Gestion de la touche Échap pour la navigation en arrière
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI.activeSelf) OpenOptions();
            else if (optionMenuUI.activeSelf || guideMenuUI.activeSelf) OpenPauseMenu();
            else if (isPaused) Resume();
            else Pause();
        }
    }

    // ==========================================
    // CONTRÔLE DE L'ÉTAT DU JEU
    // ==========================================

    /// <summary>
    /// Reprend le jeu, réactive le contrôle du temps et cache les menus.
    /// </summary>
    public void Resume()
    {
        DesactivateAllMenus();
        timeMenuUI.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Met le jeu en pause, fige le temps et affiche le menu principal.
    /// </summary>
    public void Pause()
    {
        OpenPauseMenu();
        timeMenuUI.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Ferme complètement l'application.
    /// </summary>
    public void QuitGame()
    {
        // TODO: Ajouter la logique de sauvegarde de l'univers ici
        Debug.Log("Ending Simulator !");
        Application.Quit();
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
    
    /// <summary>
    /// Ouvre le menu des options et synchronise la valeur des sliders avec la caméra.
    /// </summary>
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

    /// <summary>
    /// Fonction utilitaire pour cacher tous les panneaux d'interface.
    /// </summary>
    private void DesactivateAllMenus()
    {
        pauseMenuUI.SetActive(false);
        optionMenuUI.SetActive(false);
        guideMenuUI.SetActive(false);
        keysMenuUI.SetActive(false);
        audioMenuUI.SetActive(false);
    }

    // ==========================================
    // GESTION DES PARAMÈTRES (OPTIONS)
    // ==========================================

    /// <summary>
    /// Réinitialise les sliders à leurs valeurs par défaut.
    /// </summary>
    public void Reset()
    {
        if (fieldOfViewSlider != null) fieldOfViewSlider.value = 60f;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = 3.5f;
        if (speedSlider != null) speedSlider.value = 100f;
        
        Debug.Log("Paramètres réinitialisés aux valeurs par défaut !");
    }

    
    /// <summary>
    /// Sauvegarde les valeurs actuelles des sliders dans les PlayerPrefs et met à jour la caméra.
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