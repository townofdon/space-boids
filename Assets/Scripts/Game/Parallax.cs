using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField][Range(-1f, 1f)] float depth = 0f;

    // Transform cam; // Camera reference (of its transform)
    // Vector3 previousCamPos;

    // public float distanceX; // Distance of the item (z-index based) 
    // public float distanceY;

    // public float smoothingX = 1f; // Smoothing factor of parrallax effect
    // public float smoothingY = 1f;

    // float t = 0f;

    // void Awake()
    // {
    //     cam = Camera.main.transform;
    // }

    // void Update()
    // {
    //     float parallaxX = (previousCamPos.x - cam.position.x) * transform.position.z * -0.1f;
    //     Vector3 backgroundTargetPosX = new Vector3(transform.position.x + parallaxX, transform.position.y, transform.position.z);
    //     transform.position = Vector3.Lerp(transform.position, backgroundTargetPosX, smoothingX * t);

    //     float parallaxY = (previousCamPos.y - cam.position.y) * transform.position.z * -0.1f;
    //     Vector3 backgroundTargetPosY = new Vector3(transform.position.x, transform.position.y + parallaxY, transform.position.z);
    //     transform.position = Vector3.Lerp(transform.position, backgroundTargetPosY, smoothingY * t);

    //     if (previousCamPos == cam.position)
    //     {
    //         t = 0;
    //     }
    //     else
    //     {
    //         t += Time.deltaTime;
    //     }

    //     previousCamPos = cam.position;
    // }

    Vector2 initialPosition;
    Vector3 position;

    void Start()
    {
        initialPosition = transform.position;
    }

    void LateUpdate()
    {
        // disabling depth by z-index since pixel-perfect camera limits the far clipping plane to 5f
        // float depth = transform.position.z * 0.1f;
        position = initialPosition + (Vector2)CameraUtils.GetMainCamera().transform.position * depth;
        position.z = transform.position.z;
        transform.position = position;
    }
}
