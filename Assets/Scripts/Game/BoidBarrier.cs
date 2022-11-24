using System.Collections;
using DG.Tweening;
using UnityEngine;

public class BoidBarrier : MonoBehaviour
{
    [SerializeField] float pulseTime = 1f;
    [SerializeField] float minAlpha = 0.1f;

    Tween pulsing;
    SpriteRenderer spriteRenderer;

    Color initialColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialColor = spriteRenderer.color;
        StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        while (true)
        {
            pulsing = DOTween.To(() => spriteRenderer.color, (x) => spriteRenderer.color = x, initialColor.toAlpha(minAlpha), pulseTime);
            yield return pulsing.WaitForCompletion();
            pulsing = DOTween.To(() => spriteRenderer.color, (x) => spriteRenderer.color = x, initialColor, pulseTime);
            yield return pulsing.WaitForCompletion();
        }
    }
}
