using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] float animationTime = 1f;
    [SerializeField] RectTransform rect;

    Tween minTween;
    Tween maxTween;
    Tween timeTween;
    Vector2 origMinAnchors;
    Vector2 origMaxAnchors;

    Slider[] sliders;

    bool isMenuOpen;

    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        // TODO: ADD SOUND - "retract"
        isMenuOpen = true;
        PrepareMenuMovement();
        minTween = rect.DOAnchorMin(new Vector2(0.5f, 0f), animationTime);
        maxTween = rect.DOAnchorMin(new Vector2(0.5f, 0f), animationTime);
        timeTween = DOTween.To(() => Simulation.speed, (x) => Simulation.SetSimulationSpeed(x), .1f, animationTime);
        timeTween.OnComplete(() =>
        {
            foreach (var slider in sliders) slider.interactable = true;
            sliders[0].Select();
        });
    }

    public void CloseMenu()
    {
        // TODO: ADD SOUND
        isMenuOpen = false;
        PrepareMenuMovement();
        foreach (var slider in sliders) slider.interactable = false;
        minTween = rect.DOAnchorMin(origMinAnchors, animationTime);
        maxTween = rect.DOAnchorMax(origMaxAnchors, animationTime);
        timeTween = DOTween.To(() => Simulation.speed, (x) => Simulation.SetSimulationSpeed(x), 1f, animationTime);
    }

    void Awake()
    {
        sliders = GetComponentsInChildren<Slider>();
    }

    void Start()
    {
        origMinAnchors = rect.anchorMin;
        origMaxAnchors = rect.anchorMax;
    }

    void PrepareMenuMovement()
    {
        minTween.Kill();
        maxTween.Kill();
        timeTween.Kill();
    }
}
