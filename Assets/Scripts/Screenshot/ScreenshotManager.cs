using UnityEngine;
using UnityEngine.Rendering;

public enum ScreenshotMode
{
    DISABLED,
    READY,
    REVIEW,
}

public class ScreenshotManager : MonoBehaviour
{
    ScreenshotMode mode;

    [SerializeField] float timeBetweenShots = 0.1f;
    [SerializeField] ScreenshotUI screenshotUI;

    float timeElapsedSinceLastShot = float.MaxValue;
    bool shouldTakeScreenshot;

    public bool isScreenshotModeEnabled => mode != ScreenshotMode.DISABLED;

    public void ToggleScreenshotMode()
    {
        if (mode == ScreenshotMode.DISABLED)
        {
            mode = ScreenshotMode.READY;
            OnModeReady();
        }
        else
        {
            mode = ScreenshotMode.DISABLED;
            OnModeDisabled();
        }
    }

    public void TryToTakeScreenshot()
    {
        if (!screenshotUI.IsInteractable) return;
        if (timeElapsedSinceLastShot < timeBetweenShots) return;
        timeElapsedSinceLastShot = 0f;
        shouldTakeScreenshot = true;
    }

    void OnModeReady()
    {
        screenshotUI.Show();
    }

    void OnModeDisabled()
    {
        screenshotUI.Hide();
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRender;
    }

    void Update()
    {
        timeElapsedSinceLastShot += Time.deltaTime;
    }

    void OnEndCameraRender(ScriptableRenderContext arg1, Camera arg2)
    {
        if (!shouldTakeScreenshot) return;
        shouldTakeScreenshot = false;
        Time.timeScale = 0f;
        try
        {
            ScreenshotUtils.TakeScreenshot();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
        Time.timeScale = 1f;
        screenshotUI.Flash();
    }
}
