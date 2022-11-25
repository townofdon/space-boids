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
        }
        else
        {
            mode = ScreenshotMode.DISABLED;
        }
    }

    public void TryToTakeScreenshot()
    {

    }
}
