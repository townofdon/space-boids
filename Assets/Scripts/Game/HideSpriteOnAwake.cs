using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HideSpriteOnAwake : MonoBehaviour
{
    private void Start()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }
}
