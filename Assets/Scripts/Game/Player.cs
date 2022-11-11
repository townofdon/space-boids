using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 360f;
    [SerializeField] float cardinality = 45f;

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
    }

    void Update()
    {
        rb.velocity = move * moveSpeed * Simulation.speed;
        if (rb.velocity.magnitude > Mathf.Epsilon)
        {
            float angle = Vector2.SignedAngle(Vector2.up, rb.velocity);
            angle = angle - angle % cardinality;
            desiredRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = desiredRotation;
        }
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
