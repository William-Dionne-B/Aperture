using UnityEngine;

/// <summary>
/// Sert d'interface entre les boutons UI de gestion du temps et 
/// le système physique de gravité globale.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static float currentSpeedMultiplier = 1f;

    // ==========================================
    // CONTRÔLE DYNAMIQUE
    // ==========================================

    /// <summary>
    /// À connecter sur un Slider UI ou des boutons (ex: 0.5f, 1f, 2f, 10f)
    /// </summary>
    public void SetSpeedMultiplier(float newSpeed)
    {
        currentSpeedMultiplier = newSpeed;

        if (!PauseMenu.isSimulationPaused && !PauseMenu.isMenuOpen)
        {
            ApplySpeed(currentSpeedMultiplier);
        }
    }

    public void SetSpeedNormal() { SetSpeedMultiplier(1f); }
    public void SetSpeedFast() { SetSpeedMultiplier(5f); }
    public void SetSpeedUltra() { SetSpeedMultiplier(10f); }

    // ==========================================
    // GESTION DES PAUSES
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

    /// <summary>
    /// Etablit une vitesse pour la simulation
    /// </summary>
    private static void ApplySpeed(float speed)
    {
        if (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(speed);
        }
    }
}