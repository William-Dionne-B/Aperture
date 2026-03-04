using UnityEngine;

/// <summary>
/// Gère le mouvement libre et la rotation de la caméra dans l'espace.
/// Permet également de modifier la vitesse et le champ de vision (FOV).
/// </summary>
public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement Settings")] 
    public float moveSpeed = 100f;
    public float boostMultiplier = 25f;
    public float acceleration = 5f;

    [Header("Mouse Look Settings")] 
    public float mouseSensitivity = 3.5f;
    public bool lockCursor = true;

    [Header("Key Bindings")]
    public KeyCode unlockCursorKey = KeyCode.Tab;
    public KeyCode speedUp = KeyCode.UpArrow;
    public KeyCode speedDown = KeyCode.DownArrow;

    [Header("References")]
    public Camera playerCamera;
    
    // --- Variables privées ---
    private Vector3 velocity;
    private float yaw;
    private float pitch;

    // ==========================================
    // MÉTHODES UNITY
    // ==========================================
    
    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        
        // Chargement des paramètres sauvegardés
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 3.5f);
        playerCamera.fieldOfView = PlayerPrefs.GetFloat("FieldOfView", 60f);
        moveSpeed = PlayerPrefs.GetFloat("MoveSpeed", 100f);
        
        if (lockCursor)
            LockCursor();

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(unlockCursorKey))
            ToggleCursor();

        if (PauseMenu.isPaused || Cursor.visible)
            return;

        HandleInputs();
        
        HandleMouseLook();
        HandleMovement();
    }

    // ==========================================
    // LOGIQUE PRINCIPALE DE LA CAMÉRA
    // ==========================================

    /// <summary>
    /// Gère les entrées clavier et souris pour modifier la vitesse et le FOV.
    /// </summary>
    private void HandleInputs()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0f)
        {
            ChangerFieldOfView(Mathf.Clamp(playerCamera.fieldOfView + (scroll * -50f), 30f, 110f));
        }

        if (Input.GetKey(speedUp))
        {
            float newSpeed = moveSpeed + (100f * Time.unscaledDeltaTime);
            ChangerVitesse(Mathf.Clamp(newSpeed, 10f, 500f));
        }

        if (Input.GetKey(speedDown))
        {
            float newSpeed = moveSpeed - (100f * Time.unscaledDeltaTime);
            ChangerVitesse(Mathf.Clamp(newSpeed, 10f, 500f));
        }
    }
    
    /// <summary>
    /// Calcule et applique la rotation de la caméra basée sur les mouvements de la souris.
    /// </summary>
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.unscaledDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.unscaledDeltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Calcule et applique le déplacement de la caméra dans l'espace 3D.
    /// </summary>
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
        velocity = Vector3.Lerp(velocity, targetVelocity, acceleration * Time.unscaledDeltaTime);

        transform.position += velocity * Time.unscaledDeltaTime;
    }

    // ==========================================
    // GESTION DU CURSEUR
    // ==========================================
    
    void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
        else LockCursor();
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
    
    // ==========================================
    // MÉTHODES PUBLIQUES (Utilisées par l'UI)
    // ==========================================

    public void ChangerFieldOfView(float newFieldOfView) { playerCamera.fieldOfView = newFieldOfView; }
    public void ChangerMouseSensitivity(float newMouseSensitivity) { mouseSensitivity = newMouseSensitivity; }
    public void ChangerVitesse(float newSpeed) { moveSpeed = newSpeed; }
}