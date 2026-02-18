using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(LineRenderer))]
public class GravityBody : MonoBehaviour
{
    [Header("Initial Motion")]
    public Vector3 initialVelocity;
    public bool applyInitialVelocity = true;
    public float Mass = 1f;

    [HideInInspector] public Rigidbody rb;

    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = Mass;
        rb.linearVelocity = initialVelocity;
        rb.useGravity = false;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
    }

    void OnEnable()
    {
        GravityManager.Register(this);

        if (!initialized && applyInitialVelocity)
        {
            rb.linearVelocity = initialVelocity;
            initialized = true;
        }
    }

    void OnDisable()
    {
        GravityManager.Unregister(this);
    }
}
