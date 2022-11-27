using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Transform player;

    Vector3 position;

    void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void LateUpdate()
    {
        position = player.position;
        position.z = transform.position.z;
        transform.position = position;
    }
}
