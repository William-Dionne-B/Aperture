using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject optionMenuUI;
    public GameObject guideMenuUI;
    public GameObject keysMenuUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (keysMenuUI.activeSelf)
            {
                pauseMenuUI.SetActive(false);
                optionMenuUI.SetActive(true);
                guideMenuUI.SetActive(false);
                keysMenuUI.SetActive(false);
            }

            else if (optionMenuUI.activeSelf || guideMenuUI.activeSelf)
            {
                pauseMenuUI.SetActive(true);
                optionMenuUI.SetActive(false);
                guideMenuUI.SetActive(false);
            }

            else if (pauseMenuUI.activeSelf)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
        Time.timeScale = 1f;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
        Time.timeScale = 0f;

        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
    }

    public void QuitGame()
    {
        //SAUVEGARDE
        Debug.Log("Ending Simulator !");
        Application.Quit();
    }

    public void Options()
    {
        pauseMenuUI.SetActive(false);
        optionMenuUI.SetActive(true);
    }

    public void Guide()
    {
        pauseMenuUI.SetActive(false);
        optionMenuUI.SetActive(false);
        guideMenuUI.SetActive(true);
    }

    public void Keys()
    {
        pauseMenuUI.SetActive(false);
        optionMenuUI.SetActive(false);
        guideMenuUI.SetActive(false);
        keysMenuUI.SetActive(true);
    }

    public void Reset()
    {
        Debug.Log("Reset !");
    }

    public void BackOption()
    {
        optionMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        Debug.Log("Settings Saved !");
    }

    public void BackGuide()
    {
        guideMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void BackKeys()
    {
        keysMenuUI.SetActive(false);
        optionMenuUI.SetActive(true);
    }
}