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
        rb.AddTorque(UnityEngine.Random.Range(-rotationVariance, rotationVariance));
    }
}