using UnityEngine;
using DG.Tweening;

public class ScreenshotUI : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float flashDuration = 0.5f;

    [Space]
    [Space]

    [SerializeField] float slowmoTargetSpeed = 0.5f;
    [SerializeField] float slowmoDuration = 1f;

    [Space]
    [Space]

    [SerializeField] CanvasGroup flash;

    CanvasGroup canvasGroup;

    Tween fading;
    Tween slowmoing;
    Tween flashing;
    bool isInteractable;

    public bool IsInteractable => isInteractable;


    public void Show()
    {
        ResetTweens();
        isInteractable = false;
        slowmoing = DOTween.To(() => Simulation.speed, x => Simulation.SetSimulationSpeed(x), slowmoTargetSpeed, slowmoDuration);
        fading = canvasGroup.DOFade(1f, fadeDuration);
        fading.OnComplete(() =>
        {
            isInteractable = true;
        });
    }

    public void Hide()
    {
        ResetTweens();
        isInteractable = false;
        slowmoing = DOTween.To(() => Simulation.speed, x => Simulation.SetSimulationSpeed(x), 1f, slowmoDuration);
        fading = canvasGroup.DOFade(0f, fadeDuration);
    }

    public void Flash()
    {
        if (!isInteractable) return;
        if (flashing != null) flashing.Kill();
        flash.alpha = 1f;
        flashing = flash.DOFade(0f, flashDuration);
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        flash.alpha = 0f;
    }

    void ResetTweens()
    {
        if (fading != null) fading.Kill();
        if (slowmoing != null) slowmoing.Kill();
    }
}
