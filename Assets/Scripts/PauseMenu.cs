using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")] // Comme un titre
    public GameObject pauseMenuUI;
    public GameObject optionMenuUI;
    public GameObject guideMenuUI;
    public GameObject keysMenuUI;

    public static bool isPaused = false;

    // Update is called once per frame
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
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        OpenPauseMenu();
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
        Debug.Log("Reset !");
    }

    public void SaveOptions()
    {
        Debug.Log("Settings Saved !");
        OpenPauseMenu();
    }
}