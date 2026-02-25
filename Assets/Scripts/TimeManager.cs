using UnityEngine;

/// <summary>
/// Sert d'interface entre les boutons UI de gestion du temps et 
/// le système physique de gravité globale (GravityManager).
/// </summary>
public class TimeManager : MonoBehaviour
{
    // ==========================================
    // CONTRÔLE DU TEMPS ACTUEL (Option 1)
    // ==========================================
    
    /// <summary>
    /// Remet la vitesse de la simulation à la normale (x1).
    /// </summary>
    public void Resume()
    {
        if (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(1f);
        }
    }

    /// <summary>
    /// Fige complètement la simulation physique (x0).
    /// </summary>
    public void Pause()
    {
        if  (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(0f);
        }
    }

    /// <summary>
    /// Accélère la simulation physique (x10).
    /// </summary>
    public void SetFastForward()
    {
        if (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(10f);
        }
    }

    // =========================================================
    // FONCTIONNALITÉS EN ATTENTE POUR L'OPTION 2
    // =========================================================
    
    /// <summary>
    /// [DÉSACTIVÉ] Fera reculer le temps de la simulation.
    /// Incompatible avec le système Rigidbody actuel.
    /// </summary>
    public void SetFastBackward()
    {
        Debug.LogWarning("Le retour en arrière est désactivé avec le moteur physique actuel. Prévu pour plus tard !");
    }

    public void SkipOneSecond() { Skip(1f); }
    public void SkipFiveSeconds() { Skip(5f); }
    public void SkipTenSeconds() { Skip(10f); }
    
    public void GoBackOneSecond() { Skip(-1f); }
    public void GoBackFiveSeconds() { Skip(-5f); }
    public void GoBackTenSeconds() { Skip(-10f); }

    /// <summary>
    /// [DÉSACTIVÉ] Fonction centrale pour sauter dans le temps.
    /// </summary>
    private void Skip(float seconds)
    {
        Debug.LogWarning("Les sauts dans le temps sont incompatibles avec les Rigidbodies pour l'instant. Fonction désactivée.");
        
    }
}