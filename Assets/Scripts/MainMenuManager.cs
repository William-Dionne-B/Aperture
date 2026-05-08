using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Contrôle la gestion de transition entre les menus de l'application.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    
    /// <summary>
    /// Contrôle la transition entre le menu d'acceuil et la scene du systeme solaire
    /// </summary>
    public void OnStartButtonPressed()
    {
        LoadScene("SystemeSolaire");
    }

    
    /// <summary>
    /// Contrôle la gestion de ce qui se passe lorsqu'on quitte l'application.
    /// </summary>
    public void OnQuitButtonPressed()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Fonction generale permettant de load une scene
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot load a scene because the scene name is empty.");
            return;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(sceneName);
    }

}
