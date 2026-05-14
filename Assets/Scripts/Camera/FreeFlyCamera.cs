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
    public KeyCode speed1 = KeyCode.Alpha1;
    public KeyCode speed2 = KeyCode.Alpha2;
    public KeyCode speed3 = KeyCode.Alpha3;
    public KeyCode speed4 = KeyCode.Alpha4;
    public KeyCode speed5 = KeyCode.Alpha5;

    [Header("References")]
    public Camera playerCamera;
    
    // --- Variables privées ---
    private Vector3 velocity;
    private float yaw;
    private float pitch;
    
    
    /// <summary>
    /// Initialise la camera et charge les parametres sauvegardes.
    /// </summary>
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

    /// <summary>
    /// Gere l'input utilisateur, la rotation et le deplacement.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(unlockCursorKey))
            ToggleCursor();

        if (Cursor.visible)
            return;
        
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     if (PauseMenu.isPaused)
        //     {
        //         TimeManager.Resume();
        //         PauseMenu.isPaused = false;
        //     }
        //     
        //         
        //     else
        //     {
        //         TimeManager.Pause();
        //         PauseMenu.isPaused = true;
        //     }
        //     
        // }

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
        
        if (Input.GetKey(speed1))
        {
            ChangerVitesse(Mathf.Clamp(5, 5f, 500f));
        }
        
        if (Input.GetKey(speed2))
        {
            ChangerVitesse(Mathf.Clamp(10, 5f, 500f));
        }
        
        if (Input.GetKey(speed3))
        {
            ChangerVitesse(Mathf.Clamp(55, 5f, 500f));
        }
        
        if (Input.GetKey(speed4))
        {
            ChangerVitesse(Mathf.Clamp(105, 5f, 500f));
        }
        
        if (Input.GetKey(speed5))
        {
            ChangerVitesse(Mathf.Clamp(250, 10f, 500f));
        }
    }
    
    /// <summary>
    /// Calcule et applique la rotation de la caméra basée sur les mouvements de la souris.
    /// </summary>
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 1f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 1f;

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
        // Lecture manuelle — exclut les flèches du mouvement
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;

        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y += 1f;
        if (Input.GetKey(KeyCode.Q)) y -= 1f;

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

    /// <summary>
    /// Bascule l'etat de verrouillage du curseur.
    /// </summary>
    void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
        else LockCursor();
    }

    /// <summary>
    /// Verrouille et masque le curseur.
    /// </summary>
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Deverrouille et affiche le curseur.
    /// </summary>
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    // ==========================================
    // MÉTHODES PUBLIQUES (Utilisées par l'UI)
    // ==========================================

    /// <summary>
    /// Met a jour le champ de vision de la camera.
    /// </summary>
    public void ChangerFieldOfView(float newFieldOfView) { playerCamera.fieldOfView = newFieldOfView; }

    /// <summary>
    /// Met a jour la sensibilite de la souris.
    /// </summary>
    public void ChangerMouseSensitivity(float newMouseSensitivity) { mouseSensitivity = newMouseSensitivity; }

    /// <summary>
    /// Met a jour la vitesse de deplacement.
    /// </summary>
    public void ChangerVitesse(float newSpeed) { moveSpeed = newSpeed; }
}