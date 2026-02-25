using UnityEngine;
using UnityEngine.Events;

public class DragDropManager : MonoBehaviour
{
    // Create a signal for button presses
    public UnityEvent<int> OnButtonPressed;

    // Call this when a button is pressed
    public void ButtonPressed(int buttonID)
    {
        Debug.Log("Button " + buttonID + " pressed");

        // Invoke the signal for listeners
        if (OnButtonPressed != null)
            OnButtonPressed.Invoke(buttonID);
    }
}