using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class MenuManager : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] Canvas _pauseMenu;
    [SerializeField] Canvas _audioMenu;

    [Space]
    [Space]

    [Header("Global"), ParamRef]
    [SerializeField] string globalParamMenuOpen;

    ISettingsMenu pauseMenu;
    ISettingsMenu audioMenu;

    void UnPause()
    {
        GlobalEvent.Invoke(GlobalEvent.type.UNPAUSE);
    }

    public void ShowPauseMenu()
    {
        audioMenu.Hide(fadeDuration);
        pauseMenu.Show(fadeDuration);
    }

    public void ShowAudioSettings()
    {
        pauseMenu.Hide(fadeDuration);
        audioMenu.Show(fadeDuration);
    }

    void HideAllMenus()
    {
        pauseMenu.Hide(fadeDuration);
        audioMenu.Hide(fadeDuration);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    void OnEnable()
    {
        GlobalEvent.Subscribe(OnGlobalEvent);
    }

    void OnDisable()
    {
        GlobalEvent.Unsubscribe(OnGlobalEvent);
    }

    void OnGlobalEvent(GlobalEvent.type eventType)
    {
        switch (eventType)
        {
            case GlobalEvent.type.PAUSE:
                OnPause();
                break;
            case GlobalEvent.type.UNPAUSE:
                OnUnpause();
                break;
            case GlobalEvent.type.MUFFLE_AUDIO:
                SetGlobalParamMenuOpen(true);
                break;
            case GlobalEvent.type.UNMUFFLE_AUDIO:
                SetGlobalParamMenuOpen(false);
                break;
        }
    }

    void OnPause()
    {
        ShowPauseMenu();
        Time.timeScale = 0f;
        GlobalEvent.Invoke(GlobalEvent.type.MUFFLE_AUDIO);
    }

    void OnUnpause()
    {
        HideAllMenus();
        Time.timeScale = 1f;
        GlobalEvent.Invoke(GlobalEvent.type.UNMUFFLE_AUDIO);
    }

    void Awake()
    {
        pauseMenu = _pauseMenu.GetComponent<ISettingsMenu>();
        audioMenu = _audioMenu.GetComponent<ISettingsMenu>();
    }

    void SetGlobalParamMenuOpen(bool isOpen)
    {
        if (string.IsNullOrEmpty(globalParamMenuOpen)) return;
        float value = isOpen ? 1f : 0f;
        FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName(globalParamMenuOpen, value);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError(string.Format(("[FMOD] StudioGlobalParameterTrigger failed to set parameter {0} : result = {1}"), globalParamMenuOpen, result));
        }
    }
}
