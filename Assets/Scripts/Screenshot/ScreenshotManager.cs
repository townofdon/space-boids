using UnityEngine;

public enum ScreenshotMode
{
    DISABLED,
    READY,
    REVIEW,
}

public class ScreenshotManager : MonoBehaviour
{
    ScreenshotMode mode;

    [SerializeField] ScreenshotUI screenshotUI;

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
            screenshotUI.Hide();
        }
    }

    public void TryToTakeScreenshot()
    {
        screenshotUI.Flash();
    }

    void OnModeReady()
    {
        screenshotUI.Show();
    }

    void OnModeDisabled()
    {
        screenshotUI.Hide();
    }
}
