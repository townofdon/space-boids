using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PauseMenu : MonoBehaviour, ISettingsMenu
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button resumeButton;

    Tween focusing;
    bool isShowing = false;

    Button[] buttons;

    public void Show(float fadeDuration)
    {
        if (isShowing) return;
        isShowing = true;
        if (focusing != null) focusing.Kill();
        focusing = canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        focusing.OnComplete(() =>
        {
            SetInteractable(true);
            resumeButton.Select();
        });
    }

    public void Hide(float fadeDuration)
    {
        if (!isShowing) return;
        if (focusing != null) focusing.Kill();
        isShowing = false;
        SetInteractable(false);
        focusing = canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
    }

    void Awake()
    {
        buttons = GetComponentsInChildren<Button>();
    }

    void Start()
    {
        SetInteractable(false);
        canvasGroup.alpha = 0f;
    }

    void SetInteractable(bool isInteractable)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = isInteractable;
        }
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }
}
