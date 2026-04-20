using UnityEngine;

/// <summary>
/// Sert d'interface entre les boutons UI de gestion du temps et 
/// le système physique de gravité globale (GravityManager).
/// </summary>
public class TimeManager : MonoBehaviour
{
    // On mémorise la vitesse choisie par le joueur (par défaut 1x)
    public static float currentSpeedMultiplier = 1f;

    // ==========================================
    // CONTRÔLE DYNAMIQUE (Pour tes Sliders / Boutons UI)
    // ==========================================

    /// <summary>
    /// À connecter sur un Slider UI ou des boutons (ex: 0.5f, 1f, 2f, 10f)
    /// </summary>
    public void SetSpeedMultiplier(float newSpeed)
    {
        currentSpeedMultiplier = newSpeed;

        // On applique la nouvelle vitesse UNIQUEMENT si le jeu n'est pas en pause
        if (!PauseMenu.isSimulationPaused && !PauseMenu.isMenuOpen)
        {
            ApplySpeed(currentSpeedMultiplier);
        }
    }

    // Boutons UI rapides pour te faciliter la vie
    public void SetSpeedNormal() { SetSpeedMultiplier(1f); }
    public void SetSpeedFast() { SetSpeedMultiplier(5f); }
    public void SetSpeedUltra() { SetSpeedMultiplier(10f); }

    // ==========================================
    // GESTION DES PAUSES (Appelé par PauseMenu)
    // ==========================================
    
    /// <summary>
    /// Relance la simulation à la vitesse mémorisée.
    /// </summary>
    public static void Resume()
    {
        ApplySpeed(currentSpeedMultiplier);
    }

    /// <summary>
    /// Fige complètement la simulation physique (x0).
    /// </summary>
    public static void Pause()
    {
        ApplySpeed(0f);
    }

    private static void ApplySpeed(float speed)
    {
        if (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(speed);
        }
    }
}