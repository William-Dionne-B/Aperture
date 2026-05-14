using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(LineRenderer))]
public class GravityBody : MonoBehaviour
{
    [Header("Initial Motion")]
    public Vector3 initialVelocity;
    public bool applyInitialVelocity = true;

    [HideInInspector] public float Mass = 1f;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public LineRenderer line;

    private bool initialized = false;

    /// <summary>
    /// Initialise le rigidbody et le line renderer avec les valeurs de depart.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        line = GetComponent<LineRenderer>();
        rb.mass = Mass;
        rb.linearVelocity = initialVelocity;
        rb.useGravity = false;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
    }

    /// <summary>
    /// Enregistre le corps dans le gestionnaire et applique la vitesse initiale.
    /// </summary>
    void OnEnable()
    {
        GravityManager.Register(this);

        if (!initialized && applyInitialVelocity)
        {
            rb.linearVelocity = initialVelocity;
            initialized = true;
        }
    }


    /// <summary>
    /// Retire le corps du gestionnaire de gravite.
    /// </summary>
    void OnDisable()
    {
        GravityManager.Unregister(this);
    }
}