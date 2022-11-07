using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 360f;

    Rigidbody2D rb;

    Vector2 move;
    Vector2 look;

    void Start()
    {
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
        rb.velocity = move * moveSpeed;
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
