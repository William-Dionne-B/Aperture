using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float simulationSpeed = 1f;

    public static float universTime = 0f;

    // void Update()
    // {
    //     // Time.timeScale = simulationSpeed;
    //     
    //     universTime += Time.deltaTime * simulationSpeed;
    //     
    //     // Debug.Log(universTime);
    // }

    public void Resume()
    {
        if (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(1f);
        }
    }

    public void Pause()
    {
        if  (GravityManager.Instance != null)
        {
            GravityManager.Instance.SetSimulationSpeed(0f);
        }
    }

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
    
    public void SetFastBackward()
    {
        Debug.LogWarning("Le retour en arrière est désactivé avec le moteur physique actuel. Prévu pour plus tard !");
        // TODO consulter si nous voulons aller dans le passer du debut de la simulation puisque faut changer ca si on veut pas
        // if (GravityManager.Instance != null)
        // {
        //     GravityManager.Instance.SetSimulationSpeed(-10f);
        // }
    }

    public void SkipOneSecond() { Skip(1f); }
    
    public void SkipFiveSeconds() { Skip(5f); }

    public void SkipTenSeconds() { Skip(10f); }
    
    public void GoBackOneSecond() { Skip(-1f); }

    public void GoBackFiveSeconds() { Skip(-5f); }

    public void GoBackTenSeconds() { Skip(-10f); }

    private void Skip(float seconds)
    {
        Debug.LogWarning("Les sauts dans le temps sont incompatibles avec les Rigidbodies pour l'instant. Fonction désactivée.");
        // if (seconds == 0)
        // {
        //     Debug.Log("TIME SKIPPED IS 0 seconds, skipping 0 seconds does nothing");
        //     return;
        // }
        //
        // universTime += seconds;
        //
        // // if (universTime < 0f) // TODO consulter si nous voulons aller dans le passer du debut de la simulation 
        // // {
        // //     universTime = 0f;
        // // }
        //
        // Debug.Log("TIME SKIPPED IS " + seconds + " seconds");
        
    }
}