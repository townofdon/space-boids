using UnityEngine;

public class SpaceRock : MonoBehaviour
{
    public float rotationVariance = 180f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        rb.AddTorque(UnityEngine.Random.Range(-rotationVariance, rotationVariance));
    }
}