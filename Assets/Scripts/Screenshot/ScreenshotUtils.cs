
using UnityEngine;
using System.Runtime.InteropServices;

// SCREENSHOT UTILS
// For more info - see Code Monkey's video "How to take a Screenshot in Unity URP or HDRP"): https://www.youtube.com/watch?v=d5nENoQN4Tw

public static class ScreenshotUtils
{
    // NOTE - full path will be /Assets/<DIR_NAME>
    // NOTE - make sure your screenshots directory is excluded from source control
    const string SCREENSHOTS_FOLDER = "Screenshots/";
    const string SCREENSHOTS_FILENAME = "SpaceBoidsScreenshot";
    const string TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";
    const string FILE_EXTENSION = "png";

    // see: /Assets/Plugins/download.jslib
    [DllImport("__Internal")] static extern void DownloadFile(byte[] array, int byteLength, string fileName);

    public static void TakeScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Rect rect = new Rect(0, 0, width, height);
        screenshotTexture.ReadPixels(rect, 0, 0);
        screenshotTexture.Apply();
        byte[] byteArray = GetEncodedData(screenshotTexture, FILE_EXTENSION);

#if (UNITY_EDITOR)
        SaveFileLocal(byteArray);
#elif (UNITY_WEBGL)
        SaveFileWebGL(byteArray);
#elif (UNITY_STANDALONE_WIN)
        SaveFileWindows(byteArray);
#elif (UNITY_STANDALONE_OSX)
        SaveFileOSX(byteArray);
#elif (UNITY_STANDALONE)
        SaveFileOther(byteArray);
#endif
    }

    static byte[] GetEncodedData(Texture2D screenshotTexture, string fileExtension)
    {
        if (fileExtension == "png")
        {
            return screenshotTexture.EncodeToPNG();
        }
        if (fileExtension == "jpg")
        {
            return screenshotTexture.EncodeToJPG();
        }
        throw new UnityException("Screenshot file must be either .png or .jpg");
    }

    static string GetFilename()
    {
        System.DateTime currentTime = System.DateTime.Now;
        string formattedTime = currentTime.ToString(TIMESTAMP_FORMAT);
        return $"{SCREENSHOTS_FILENAME}{formattedTime}.{FILE_EXTENSION}";
    }

    static void SaveFileWebGL(byte[] byteArray)
    {
        DownloadFile(byteArray, byteArray.Length, GetFilename());
    }

    static void SaveFileWindows(byte[] byteArray)
    {
        // note - if this does not work, will try this solution: https://stackoverflow.com/a/61722837
        string homePath = System.Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        System.IO.File.WriteAllBytes($"{homePath}/Downloads/{GetFilename()}", byteArray);
    }

    static void SaveFileOSX(byte[] byteArray)
    {
        // see: https://stackoverflow.com/questions/1143706/getting-the-path-of-the-home-directory-in-c
        string homePath = System.Environment.GetEnvironmentVariable("HOME");
        System.IO.File.WriteAllBytes($"{homePath}/Downloads/{GetFilename()}", byteArray);
    }

    static void SaveFileOther(byte[] byteArray)
    {
        System.IO.File.WriteAllBytes($"{Application.persistentDataPath}/{GetFilename()}", byteArray);
    }

    static void SaveFileLocal(byte[] byteArray)
    {
        System.IO.Directory.CreateDirectory($"{Application.dataPath}/{SCREENSHOTS_FOLDER}");
        System.IO.File.WriteAllBytes($"{Application.dataPath}/{SCREENSHOTS_FOLDER}{GetFilename()}", byteArray);
    }
}
