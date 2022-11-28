using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;
using DG.Tweening;

public class AudioSettings : MonoBehaviour, ISettingsMenu
{

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button cancelButton;
    [SerializeField] StudioEventEmitter testVolumeSound;

    [Space]
    [Space]

    [SerializeField] Slider sliderSFX;
    [SerializeField] Slider sliderMusic;

    const string MASTER_BUS = "bus:/Master";
    const string MUSIC_BUS = "bus:/Master/Music";
    const string SFX_BUS = "bus:/Master/SFX";

    Bus masterBus;
    Bus musicBus;
    Bus sfxBus;

    Tween focusing;
    bool isShowing;

    Slider[] sliders;
    Button[] buttons;

    public void SetMasterVolume(float volume)
    {
        SetChannelVolume(masterBus, volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetChannelVolume(musicBus, volume);
    }

    public void SetSfxVolume(float volume)
    {
        SetChannelVolume(sfxBus, volume);
        TestSfxVolume();
    }

    public void TestSfxVolume()
    {
        if (testVolumeSound != null) testVolumeSound.Play();
    }

    public void Show(float fadeDuration)
    {
        if (isShowing) return;
        isShowing = true;
        if (focusing != null) focusing.Kill();
        focusing = canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        focusing.OnComplete(() =>
        {
            ResetSliders();
            SetInteractable(true);
            cancelButton.Select();
        });
    }

    public void Hide(float fadeDuration)
    {
        if (!isShowing) return;
        isShowing = false;
        if (focusing != null) focusing.Kill();
        SetInteractable(false);
        focusing = canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
    }

    private void Awake()
    {
        buttons = GetComponentsInChildren<Button>();
        sliders = GetComponentsInChildren<Slider>();
    }

    void Start()
    {
        masterBus = RuntimeManager.GetBus(MASTER_BUS);
        musicBus = RuntimeManager.GetBus(MUSIC_BUS);
        sfxBus = RuntimeManager.GetBus(SFX_BUS);
        SetInteractable(false);
        canvasGroup.alpha = 0f;
        ResetSliders();
    }

    void ResetSliders()
    {
        ResetSlider(sliderSFX, sfxBus);
        ResetSlider(sliderMusic, musicBus);
    }

    void ResetSlider(Slider slider, Bus bus)
    {
        FMOD.RESULT result = bus.getVolume(out float value);
        if (result == FMOD.RESULT.OK)
        {
            slider.value = value;
        }
        else
        {
            Debug.LogError("Could not get volume from bus");
        }
    }

    void SetChannelVolume(Bus bus, float volume)
    {
        // 1.0 = fader initial position, 0.0 = -Infinity
        bus.setVolume(volume);
    }

    void SetInteractable(bool isInteractable)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = isInteractable;
        }
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].interactable = isInteractable;
        }
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }
}
