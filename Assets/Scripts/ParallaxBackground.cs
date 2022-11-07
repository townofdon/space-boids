using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField][Range(-1f, 1f)] float depth = 0f;

    Material mat;
    Vector2 initialOffset;

    const float SCROLL_FACTOR = 0.01f;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
        initialOffset = mat.mainTextureOffset;
    }

    void LateUpdate()
    {
        Vector2 displacement = CameraUtils.GetMainCamera().transform.position;
        mat.mainTextureOffset = initialOffset
            + displacement * SCROLL_FACTOR
            - displacement * SCROLL_FACTOR * depth;
    }
}
