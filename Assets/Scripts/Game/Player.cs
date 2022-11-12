using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


// NOTE - I combined all of the player functionality into a single script for sheer convenience.
// In an actual game I would have definitely split out various functions into separate script,
// e.g. control, movement, FX, etc.

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 360f;
    [SerializeField] float cardinality = 45f;

    [Space]
    [Space]

    [SerializeField] ParticleSystem leftTrail;
    [SerializeField] ParticleSystem rightTrail;
    [SerializeField] float exhaustRate = 20f;
    [SerializeField] AnimationCurve velocityExhaustMod = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    ParticleSystem.MainModule leftFXModule;
    ParticleSystem.MainModule rightFXModule;
    ParticleSystem.EmissionModule leftEmission;
    ParticleSystem.EmissionModule rightEmission;

    Rigidbody2D rb;

    Vector2 move;
    Vector2 look;
    Quaternion desiredRotation;

    void Start()
    {
        desiredRotation = transform.rotation;
        StartCoroutine(UpdateScreenStats());
    }

    void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        leftEmission = leftTrail.emission;
        rightEmission = rightTrail.emission;
        leftEmission.rateOverTime = 0f;
        rightEmission.rateOverTime = 0f;
    }

    void Update()
    {
        Move();
        Rotate();
        ManageTrails();
    }

    void Move()
    {
        rb.velocity = move * moveSpeed * Simulation.speed;
    }

    void Rotate()
    {
        if (rb.velocity.magnitude <= Mathf.Epsilon) return;
        float angle = Vector2.SignedAngle(Vector2.up, rb.velocity);
        angle = angle - angle % cardinality;
        desiredRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = desiredRotation;
    }

    void ManageTrails()
    {
        float exhaust = exhaustRate * velocityExhaustMod.Evaluate(Mathf.Clamp01(rb.velocity.magnitude / moveSpeed));
        leftEmission.rateOverTime = exhaust;
        rightEmission.rateOverTime = exhaust;
    }

    IEnumerator UpdateScreenStats()
    {
        while (true)
        {
            Utils.InvalidateScreenStats();
            yield return new WaitForSeconds(1f);
        }
    }
}
