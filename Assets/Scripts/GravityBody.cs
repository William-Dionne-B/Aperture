using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    [Header("Initial Motion")]
    public Vector3 initialVelocity;
    public bool applyInitialVelocity = true;

    [HideInInspector] public Rigidbody rb;

    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
