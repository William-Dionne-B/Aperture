using System.Security.Cryptography;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartButtonPressed()
    {
        // loadScene("SystemeSolaire");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SystemeSolaire");
    }

    public void OnQuitButtonPressed()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public static void loadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

}
