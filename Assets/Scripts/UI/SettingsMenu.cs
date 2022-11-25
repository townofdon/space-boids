using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using FMODUnity;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] float animationTime = 1f;
    [SerializeField] RectTransform rect;
    [SerializeField] Button openButton;

    [Space]
    [Space]

    [Header("Global"), ParamRef]
    [SerializeField] string globalParamMenuOpen;
    [SerializeField] StudioEventEmitter openSound;
    [SerializeField] StudioEventEmitter closeSound;

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

    void SetGlobalParam(bool isOpen)
    {
        if (string.IsNullOrEmpty(globalParamMenuOpen)) return;
        float value = isOpen ? 1f : 0f;
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(globalParamMenuOpen, value);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(string.Format(("[FMOD] StudioGlobalParameterTrigger failed to set parameter {0} : result = {1}"), globalParamMenuOpen, result));
        }
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;
        openSound.Play();
        SetGlobalParam(true);
        PrepareMenuMovement();
        minTween = rect.DOAnchorMin(new Vector2(0.5f, 0f), animationTime);
        maxTween = rect.DOAnchorMin(new Vector2(0.5f, 0f), animationTime);
        timeTween = DOTween.To(() => Simulation.speed, (x) => Simulation.SetSimulationSpeed(x), .1f, animationTime);
        timeTween.OnComplete(() =>
        {
            openButton.interactable = false;
            foreach (var slider in sliders) slider.interactable = true;
            sliders[0].Select();
        });
        GlobalEvent.Invoke(GlobalEvent.type.OPEN_MENU);
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;
        openButton.interactable = true;
        closeSound.Play();
        SetGlobalParam(false);
        PrepareMenuMovement();
        foreach (var slider in sliders) slider.interactable = false;
        minTween = rect.DOAnchorMin(origMinAnchors, animationTime);
        maxTween = rect.DOAnchorMax(origMaxAnchors, animationTime);
        timeTween = DOTween.To(() => Simulation.speed, (x) => Simulation.SetSimulationSpeed(x), 1f, animationTime);
        GlobalEvent.Invoke(GlobalEvent.type.CLOSE_MENU);
        EventSystem.current.SetSelectedGameObject(null);
    }

    void Awake()
    {
        sliders = GetComponentsInChildren<Slider>();
    }

    void Start()
    {
        origMinAnchors = rect.anchorMin;
        origMaxAnchors = rect.anchorMax;
        openButton.interactable = true;
    }

    void PrepareMenuMovement()
    {
        minTween.Kill();
        maxTween.Kill();
        timeTween.Kill();
    }
}
