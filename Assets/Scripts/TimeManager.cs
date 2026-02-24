using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float simulationSpeed = 1f;

    public static float universTime = 0f;

    void Update()
    {
        universTime += Time.deltaTime * simulationSpeed;
        Debug.Log(universTime);
    }

    public void Resume()
    {
        simulationSpeed = 1f;
        Debug.Log("Normal Time Speed: x1");
    }

    public void Pause()
    {
        simulationSpeed = 0f;
        Debug.Log("Pausing: x0");
    }

    public void SetFastForward()
    {
        simulationSpeed = 10f;
        Debug.Log("Fast Forward Speed: x10");
    }

    public void SetFastBackward()
    {
        // TODO consulter si nous voulons aller dans le passer du debut de la simulation puisque faut changer ca si on veut pas
        simulationSpeed = -10f;
        Debug.Log("Fast Backward Speed: x-10");
    }

    public void SkipOneSecond()
    {
        Skip(1f);
    }
    
    public void SkipFiveSeconds()
    {
        Skip(5f);
    }

    public void SkipTenSeconds()
    {
        Skip(10f);
    }
    
    public void GoBackOneSecond()
    {
        Skip(-1f);
    }

    public void GoBackFiveSeconds()
    {
        Skip(-5f);
    }

    public void GoBackTenSeconds()
    {
        Skip(-10f);
    }

    private void Skip(float seconds)
    {
        if (seconds == 0)
        {
            Debug.Log("TIME SKIPPED IS 0 seconds, skipping 0 seconds does nothing");
            return;
        }

        universTime += seconds;

        // if (universTime < 0f) // TODO consulter si nous voulons aller dans le passer du debut de la simulation 
        // {
        //     universTime = 0f;
        // }
        
        Debug.Log("TIME SKIPPED IS " + seconds + " seconds");
        
    }
}