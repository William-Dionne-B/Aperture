using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement")] 
    public float moveSpeed = 50f;
    public float boostMultiplier = 500f;
    public float acceleration = 100f;

    [Header("Mouse Look")] 
    public float mouseSensitivity = 3f;
    public bool lockCursor = true;

    public KeyCode unlockCursorKey = KeyCode.LeftAlt;
    public KeyCode shiftKey = KeyCode.LeftShift;
    public KeyCode speedUp = KeyCode.UpArrow;
    public KeyCode speedDown = KeyCode.DownArrow;

    private Vector3 velocity;
    private float yaw;
    private float pitch;

    void Start()
    {
        if (lockCursor)
        {
            LockCursor();
        }

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(unlockCursorKey))
        {
            ToggleCursor();
        }

        if (Input.GetKeyDown(speedUp))
        {
            ChangerVitesse(Mathf.Clamp(moveSpeed + 10f, 10f, 500f));
        }

        if (Input.GetKeyDown(speedDown))
        {
            ChangerVitesse(Mathf.Clamp(moveSpeed - 10f, 10f, 500f)); 
        }

        if (PauseMenu.isPaused || Cursor.visible)
        {
            return;
        }

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A / D
        float z = Input.GetAxisRaw("Vertical"); // W / S

        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y += 1f; // Up
        if (Input.GetKey(KeyCode.Q)) y -= 1f; // Down

        Vector3 input = new Vector3(x, y, z).normalized;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= boostMultiplier;

        Vector3 targetVelocity = transform.TransformDirection(input) * speed;
        velocity = Vector3.Lerp(velocity, targetVelocity, acceleration * Time.deltaTime);

        transform.position += velocity * Time.deltaTime;
    }

    void ToggleCursor()
    {
        // Si le curseur est verrouillé, on le libère. Sinon, on le verrouille.
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ChangerVitesse(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
}